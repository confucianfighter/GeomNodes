using System;
using UnityEngine;
using UnityEngine.Rendering;

#if PROBUILDER_2_9_OR_NEWER || PROBUILDER_4_OR_NEWER || PROBUILDER_5_OR_NEWER
using UnityEngine.ProBuilder;
#endif

namespace DLN
{
    public static class MeshScaleBaker
    {
        /// <summary>
        /// Bakes localScale into mesh vertex data so transform.localScale becomes (1,1,1).
        /// Works for MeshFilter meshes and ProBuilderMesh meshes.
        /// Safe for runtime and editor.
        /// </summary>
        /// <param name="root">Root object to bake.</param>
        /// <param name="includeChildren">If true, bake on root and all children.</param>
        /// <param name="updateMeshCollider">If true, updates MeshCollider.sharedMesh after bake.</param>
        /// <param name="forceUniqueMeshInstance">
        /// If true, ensures we do not mutate a shared asset mesh by instancing first.
        /// Recommended in editor workflows.
        /// </param>
        public static void BakeLocalScaleIntoMesh(
            GameObject root,
            bool includeChildren = true,
            bool updateMeshCollider = true,
            bool forceUniqueMeshInstance = true)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));

            if (includeChildren)
            {
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    BakeOnSingleTransform(t, updateMeshCollider, forceUniqueMeshInstance);
            }
            else
            {
                BakeOnSingleTransform(root.transform, updateMeshCollider, forceUniqueMeshInstance);
            }
        }

        private static void BakeOnSingleTransform(
            Transform t,
            bool updateMeshCollider,
            bool forceUniqueMeshInstance)
        {
            if (t == null) return;

            var s = t.localScale;
            if (ApproximatelyOne(s)) return;

            // Avoid weirdness / divide by zero in normal correction.
            if (Mathf.Approximately(s.x, 0f) || Mathf.Approximately(s.y, 0f) || Mathf.Approximately(s.z, 0f))
            {
                Debug.LogWarning($"[MeshScaleBaker] Skipping '{t.name}' because localScale has a zero component: {s}", t);
                return;
            }

            // 1) ProBuilder path (preferred if present) - keeps PB internal representation consistent.
#if PROBUILDER_2_9_OR_NEWER || PROBUILDER_4_OR_NEWER || PROBUILDER_5_OR_NEWER
        var pb = t.GetComponent<ProBuilderMesh>();
        if (pb != null)
        {
            BakeScaleIntoProBuilderMesh(pb, s);
            t.localScale = Vector3.one;

            if (updateMeshCollider)
                TryUpdateMeshCollider(t);

            return;
        }
#endif

            // 2) MeshFilter path
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null)
            {
                var mesh = GetWritableMesh(mf, forceUniqueMeshInstance);
                if (mesh == null) return;

                BakeScaleIntoUnityMesh(mesh, s);
                t.localScale = Vector3.one;

                if (updateMeshCollider)
                    TryUpdateMeshCollider(t);
            }
        }

#if PROBUILDER_2_9_OR_NEWER || PROBUILDER_4_OR_NEWER || PROBUILDER_5_OR_NEWER
    private static void BakeScaleIntoProBuilderMesh(ProBuilderMesh pb, Vector3 scale)
    {
        // ProBuilder stores positions separately; modify those then rebuild.
        var positions = pb.positions;
        for (int i = 0; i < positions.Count; i++)
            positions[i] = Vector3.Scale(positions[i], scale);

        pb.positions = positions;

        // Rebuild the Unity mesh representation.
        pb.ToMesh();
        pb.Refresh(); // refresh normals/tangents/uvs as needed

        // Ensure bounds are correct
        var mf = pb.GetComponent<MeshFilter>();
        if (mf != null && mf.sharedMesh != null)
            mf.sharedMesh.RecalculateBounds();
    }
#endif

        private static Mesh GetWritableMesh(MeshFilter mf, bool forceUniqueMeshInstance)
        {
            if (mf == null) return null;

            // mf.mesh returns an instance at runtime; in editor it may instantiate lazily too.
            // We additionally force an instance to be safe (prevents modifying imported/shared assets).
            var mesh = mf.mesh;
            if (mesh == null) return null;

            if (forceUniqueMeshInstance)
            {
                // If multiple objects might share this mesh instance, clone it.
                // (Unity doesn't expose ref counts, so we just always clone when forced.)
                var cloned = UnityEngine.Object.Instantiate(mesh);
                cloned.name = mesh.name + " (Baked Scale Instance)";
                mf.sharedMesh = cloned;
                mesh = cloned;
            }

            return mesh;
        }

        private static void BakeScaleIntoUnityMesh(Mesh mesh, Vector3 scale)
        {
            if (mesh == null) return;

            // Vertices
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
                verts[i] = Vector3.Scale(verts[i], scale);
            mesh.vertices = verts;

            // Normals: for non-uniform scale, transform by inverse-transpose.
            // Since scale matrix is diagonal, inverse-transpose is just component-wise divide.
            if (mesh.normals != null && mesh.normals.Length == verts.Length)
            {
                var n = mesh.normals;
                var inv = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
                for (int i = 0; i < n.Length; i++)
                {
                    var nn = new Vector3(n[i].x * inv.x, n[i].y * inv.y, n[i].z * inv.z);
                    n[i] = nn.normalized;
                }
                mesh.normals = n;
            }
            else
            {
                // If normals missing, regenerate.
                mesh.RecalculateNormals();
            }

            // Tangents (w is handedness)
            if (mesh.tangents != null && mesh.tangents.Length == verts.Length)
            {
                var t = mesh.tangents;
                var inv = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
                for (int i = 0; i < t.Length; i++)
                {
                    var v = new Vector3(t[i].x * inv.x, t[i].y * inv.y, t[i].z * inv.z).normalized;
                    t[i] = new Vector4(v.x, v.y, v.z, t[i].w);
                }
                mesh.tangents = t;
            }
            else
            {
                // Optional: you can RecalculateTangents on newer Unity versions
#if UNITY_2020_2_OR_NEWER
            try { mesh.RecalculateTangents(); } catch { /* ignore */ }
#endif
            }

            mesh.RecalculateBounds();
            mesh.UploadMeshData(false); // keep it writable
        }

        private static void TryUpdateMeshCollider(Transform t)
        {
            var mc = t.GetComponent<MeshCollider>();
            var mf = t.GetComponent<MeshFilter>();
            if (mc != null && mf != null && mf.sharedMesh != null)
            {
                // Force refresh
                mc.sharedMesh = null;
                mc.sharedMesh = mf.sharedMesh;
            }
        }

        private static bool ApproximatelyOne(Vector3 v)
        {
            return Mathf.Abs(v.x - 1f) < 1e-6f &&
                   Mathf.Abs(v.y - 1f) < 1e-6f &&
                   Mathf.Abs(v.z - 1f) < 1e-6f;
        }
    }
}