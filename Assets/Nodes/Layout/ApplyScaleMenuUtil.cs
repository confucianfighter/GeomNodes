using UnityEditor;
using UnityEngine;

namespace DLN
{
    public static class C3DLSMenu
    {
        private const string MenuRoot = "Tools/C3DLS/";

        [MenuItem(MenuRoot + "Apply Local Scale To Mesh")]
        public static void ApplyLocalScaleToSelectedMesh()
        {
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("No GameObject selected.");
                return;
            }

            if (!TryApplyLocalScaleToMesh(go))
            {
                Debug.LogWarning($"Selected object '{go.name}' does not have a MeshFilter/sharedMesh.");
            }
        }

        [MenuItem(MenuRoot + "Apply Local Scale To Mesh", true)]
        public static bool ValidateApplyLocalScaleToSelectedMesh()
        {
            var go = Selection.activeGameObject;
            if (go == null) return false;

            var mf = go.GetComponent<MeshFilter>();
            return mf != null && mf.sharedMesh != null;
        }

        public static bool TryApplyLocalScaleToMesh(GameObject go)
        {
            if (go == null) return false;

            var tf = go.transform;
            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                return false;

            var col = go.GetComponent<Collider>();
            var originalScale = tf.localScale;

            if (ApproximatelyOne(originalScale))
            {
                Debug.Log($"'{go.name}' already has localScale = 1. Nothing to apply.");
                return true;
            }

            var mesh = GetOrCreateUniqueMeshInstance(mf);
            if (mesh == null)
                return false;

            Transform[] childTransforms = GetDirectChildren(tf);
            Vector3[] childLocalPositions = new Vector3[childTransforms.Length];
            Vector3[] childLocalScales = new Vector3[childTransforms.Length];

            for (int i = 0; i < childTransforms.Length; i++)
            {
                childLocalPositions[i] = childTransforms[i].localPosition;
                childLocalScales[i] = childTransforms[i].localScale;
            }

            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Apply Local Scale To Mesh");

            Undo.RecordObject(tf, "Apply Local Scale To Mesh");
            Undo.RecordObject(mf, "Apply Local Scale To Mesh");
            Undo.RecordObject(mesh, "Apply Local Scale To Mesh");

            if (col != null)
                Undo.RecordObject(col, "Apply Local Scale To Mesh");

            for (int i = 0; i < childTransforms.Length; i++)
                Undo.RecordObject(childTransforms[i], "Apply Local Scale To Mesh");

            ApplyScaleToMesh(mesh, originalScale);

            tf.localScale = Vector3.one;

            for (int i = 0; i < childTransforms.Length; i++)
            {
                var child = childTransforms[i];
                child.localPosition = Vector3.Scale(childLocalPositions[i], originalScale);
                child.localScale = Vector3.Scale(childLocalScales[i], InverseSafe(originalScale));
            }

            if (col != null)
            {
                ColliderUpdater.RefreshCollider(
                    col: col,
                    bounds: mesh.bounds,
                    runtimeMesh: mesh
                );
            }

            EditorUtility.SetDirty(mesh);
            EditorUtility.SetDirty(mf);
            EditorUtility.SetDirty(tf);

            if (col != null)
                EditorUtility.SetDirty(col);

            for (int i = 0; i < childTransforms.Length; i++)
                EditorUtility.SetDirty(childTransforms[i]);

            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log($"Applied local scale {originalScale} to mesh on '{go.name}' and reset localScale to 1.");
            return true;
        }

        private static Mesh GetOrCreateUniqueMeshInstance(MeshFilter mf)
        {
            if (mf == null || mf.sharedMesh == null)
                return null;

            Mesh current = mf.sharedMesh;

            if (AssetDatabase.Contains(current))
            {
                Mesh clone = Object.Instantiate(current);
                clone.name = current.name + " (Scaled Instance)";

                Undo.RecordObject(mf, "Assign Unique Mesh Instance");
                mf.sharedMesh = clone;

                current = clone;
            }

            return current;
        }

        private static void ApplyScaleToMesh(Mesh mesh, Vector3 scale)
        {
            if (mesh == null) return;

            var vertices = mesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = Vector3.Scale(vertices[i], scale);

            mesh.vertices = vertices;

            bool validNormals = mesh.normals != null && mesh.normals.Length == vertices.Length;
            bool validTangents = mesh.tangents != null && mesh.tangents.Length == vertices.Length;

            Matrix4x4 normalMatrix = Matrix4x4.Scale(InverseSafe(scale)).transpose;

            if (validNormals)
            {
                var normals = mesh.normals;
                for (int i = 0; i < normals.Length; i++)
                    normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;

                mesh.normals = normals;
            }
            else
            {
                mesh.RecalculateNormals();
            }

            if (validTangents)
            {
                var tangents = mesh.tangents;
                for (int i = 0; i < tangents.Length; i++)
                {
                    Vector3 t = new Vector3(tangents[i].x, tangents[i].y, tangents[i].z);
                    t = normalMatrix.MultiplyVector(t).normalized;
                    tangents[i] = new Vector4(t.x, t.y, t.z, tangents[i].w);
                }

                mesh.tangents = tangents;
            }
            else
            {
                mesh.RecalculateTangents();
            }

            mesh.RecalculateBounds();
        }

        private static Transform[] GetDirectChildren(Transform parent)
        {
            int count = parent.childCount;
            Transform[] result = new Transform[count];
            for (int i = 0; i < count; i++)
                result[i] = parent.GetChild(i);

            return result;
        }

        private static Vector3 InverseSafe(Vector3 v)
        {
            return new Vector3(
                Mathf.Abs(v.x) > Mathf.Epsilon ? 1f / v.x : 1f,
                Mathf.Abs(v.y) > Mathf.Epsilon ? 1f / v.y : 1f,
                Mathf.Abs(v.z) > Mathf.Epsilon ? 1f / v.z : 1f
            );
        }

        private static bool ApproximatelyOne(Vector3 v)
        {
            return Mathf.Approximately(v.x, 1f)
                && Mathf.Approximately(v.y, 1f)
                && Mathf.Approximately(v.z, 1f);
        }
    }
}