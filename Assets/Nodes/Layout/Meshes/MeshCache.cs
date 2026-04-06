using System;
using UnityEngine;
using UEEditorUtility = UnityEditor.EditorUtility;

namespace DLN
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    public class MeshCache : MonoBehaviour
    {
        public Mesh originalMesh;

        [SerializeField] private MeshFilter _meshFilter = null;
        private MeshFilter MF => GetMeshFilter();
        private Mesh _workingMesh;


        void Awake()
        {
            CacheOriginalIfNeeded();
        }

        private MeshFilter GetMeshFilter()
        {
            if (_meshFilter == null) _meshFilter = GetComponent<MeshFilter>();
            return _meshFilter;
        }
        public void CacheOriginalIfNeeded()
        {
            if (originalMesh == null && MF.sharedMesh != null)
            {
                originalMesh = Instantiate(MF.sharedMesh);
                _workingMesh = MF.mesh;
                _workingMesh.MarkDynamic();
            }
        }

        public Mesh GetFreshCopy()
        {
            CacheOriginalIfNeeded();
            return originalMesh != null ? Instantiate(MF.sharedMesh) : null;
        }
        public Vector3[] GetOriginalVerts()
        {
            CacheOriginalIfNeeded();
            return originalMesh.vertices;
        }
        [ContextMenu("Restore()")]
        public void Restore()
        {
            if (originalMesh != null)
            {
                MF.mesh = GetFreshCopy();
            }
            else CacheOriginalIfNeeded();
        }

        public void WriteVerts(Vector3[] verts)
        {
            CacheOriginalIfNeeded();
            var mesh = GetWorkingMesh();
            mesh.SetVertices(verts);
            UpdateMesh(mesh);
        }
        public Mesh GetWorkingMesh()
        {
            CacheOriginalIfNeeded();
            if (_workingMesh == null)
            {
                _workingMesh = GetMeshFilter().mesh;
            }
            return _workingMesh;
        }
        private void UpdateMesh(Mesh mesh)
        {
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            UpdateCollider(mesh);

#if UNITY_EDITOR
            UEEditorUtility.SetDirty(MF);
#endif
        }
        private void UpdateCollider(Mesh mesh)
        {
            if (TryGetComponent<Collider>(out var col))
            {
                ColliderUpdater.RefreshCollider(col: col, bounds: mesh.bounds, runtimeMesh: mesh);
            }
        }

    }
}