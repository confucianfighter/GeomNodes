using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLN
{
    public static class MeshAccess
    {
        public static List<Vector3> GetVerts(GameObject go)
        {
            if (go == null)
                throw new ArgumentNullException(nameof(go));

            var pb = go.GetComponent<ProBuilderMesh>();
            if (pb != null)
            {
                if (pb.positions == null)
                    throw new InvalidOperationException(
                        $"ProBuilderMesh on '{go.name}' has no positions.");

                return new List<Vector3>(pb.positions);
            }

            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                throw new InvalidOperationException(
                    $"GameObject '{go.name}' has no valid MeshFilter/sharedMesh.");

            return new List<Vector3>(mf.sharedMesh.vertices);
        }

        public static void WriteVerts(
            GameObject go,
            IList<Vector3> vertices,
            string undoLabel = "Modify Mesh",
            bool recalculateNormals = true,
            bool recalculateTangents = true,
            bool recalculateBounds = true,
            bool makeUniqueForMeshFilter = true)
        {
            if (go == null)
                throw new ArgumentNullException(nameof(go));
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            var pb = go.GetComponent<ProBuilderMesh>();
            if (pb != null)
            {
                WriteProBuilderVerts(go, pb, vertices, undoLabel);
                return;
            }

            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                throw new InvalidOperationException(
                    $"GameObject '{go.name}' has no valid MeshFilter/sharedMesh.");

#if UNITY_EDITOR
            if (makeUniqueForMeshFilter && !Application.isPlaying)
                EnsureUniqueMeshInstanceWithUndo(mf, "Create Unique Mesh Instance");

            Undo.RecordObject(mf, undoLabel);
#endif

            WriteVerts(
                mf.sharedMesh,
                vertices,
                undoLabel,
                recalculateNormals,
                recalculateTangents,
                recalculateBounds);
        }

        public static void WriteVerts(
            Mesh mesh,
            IList<Vector3> vertices,
            string undoLabel = "Modify Mesh",
            bool recalculateNormals = true,
            bool recalculateTangents = true,
            bool recalculateBounds = true)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));

            if (vertices.Count != mesh.vertexCount)
            {
                throw new InvalidOperationException(
                    $"Vertex count mismatch. Mesh has {mesh.vertexCount}, new data has {vertices.Count}.");
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RegisterCompleteObjectUndo(mesh, undoLabel);
#endif

            mesh.SetVertices(new List<Vector3>(vertices));

            if (recalculateBounds)
                mesh.RecalculateBounds();
            if (recalculateNormals)
                mesh.RecalculateNormals();
            if (recalculateTangents)
                mesh.RecalculateTangents();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(mesh);
#endif
        }

        private static void WriteProBuilderVerts(
            GameObject go,
            ProBuilderMesh pb,
            IList<Vector3> vertices,
            string undoLabel)
        {
            if (pb.positions == null)
                throw new InvalidOperationException(
                    $"ProBuilderMesh on '{go.name}' has no positions.");

            if (vertices.Count != pb.positions.Count)
            {
                throw new InvalidOperationException(
                    $"Vertex count mismatch on '{go.name}'. ProBuilder has {pb.positions.Count}, new data has {vertices.Count}.");
            }

#if UNITY_EDITOR
            pb.MakeUnique();

            Undo.RecordObject(pb, undoLabel);

            var mf = pb.GetComponent<MeshFilter>();
            if (mf != null)
                Undo.RecordObject(mf, undoLabel);

            if (mf != null && mf.sharedMesh != null)
                Undo.RegisterCompleteObjectUndo(mf.sharedMesh, undoLabel);
#endif

            pb.positions = new List<Vector3>(vertices);
            pb.ToMesh();
            pb.Refresh();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(pb);

                var pbMf = pb.GetComponent<MeshFilter>();
                if (pbMf != null)
                    EditorUtility.SetDirty(pbMf);
                if (pbMf != null && pbMf.sharedMesh != null)
                    EditorUtility.SetDirty(pbMf.sharedMesh);
            }
#endif
        }

#if UNITY_EDITOR
        private static void EnsureUniqueMeshInstanceWithUndo(MeshFilter mf, string undoLabel)
        {
            if (mf == null || mf.sharedMesh == null || Application.isPlaying)
                return;

            var current = mf.sharedMesh;
            if (LooksLikeInstance(current))
                return;

            Undo.RecordObject(mf, undoLabel);

            Mesh clone = UnityEngine.Object.Instantiate(current);
            clone.name = current.name + " (Instance)";
            mf.sharedMesh = clone;

            Undo.RegisterCreatedObjectUndo(clone, undoLabel);

            EditorUtility.SetDirty(mf);
            EditorUtility.SetDirty(clone);
        }

        private static bool LooksLikeInstance(Mesh mesh)
        {
            if (mesh == null || string.IsNullOrEmpty(mesh.name))
                return false;

            return mesh.name.Contains("(Instance)") || mesh.name.Contains("(Clone)");
        }
#endif
    }
}