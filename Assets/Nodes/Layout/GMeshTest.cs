using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using CodeSmile.GraphMesh;

namespace DLN
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]

    public class GMeshPolylineShape : MonoBehaviour
    {
        [System.Serializable]
        public struct Point2
        {
            public float x;
            public float z;

            public Point2(float x, float z)
            {
                this.x = x;
                this.z = z;
            }

            public float3 ToFloat3() => new float3(x, 0f, z);
        }

        [Header("Closed Polyline (local XZ plane)")]
        public Point2[] points = new Point2[]
        {
        new Point2(-0.5f, -0.5f),
        new Point2(-0.5f,  0.5f),
        new Point2( 0.5f,  0.5f),
        new Point2( 0.5f, -0.5f),
        };

        [Header("Build")]
        public bool rebuildInEditMode = true;
        public bool buildOnValidate = true;

        private MeshFilter _meshFilter;
        private Mesh _generatedMesh;

        private void OnEnable()
        {
            _meshFilter = GetComponent<MeshFilter>();
            Rebuild();
        }

        private void OnValidate()
        {
            if (!buildOnValidate)
                return;

            if (!rebuildInEditMode && !Application.isPlaying)
                return;

            Rebuild();
        }

        [ContextMenu("Rebuild")]
        public void Rebuild()
        {
            if (points == null || points.Length < 3)
                return;

            if (!IsClosedEnoughToUse())
                Debug.LogWarning($"{name}: Polyline is treated as closed by point order; first point is not duplicated. That is fine.");

            var nativePoints = new NativeArray<float3>(points.Length, Allocator.Temp);
            for (int i = 0; i < points.Length; i++)
                nativePoints[i] = points[i].ToFloat3();

            GMesh gmesh = null;

            try
            {
                // This constructor creates a single face from the supplied ordered vertices.
                gmesh = new GMesh(nativePoints);

                if (_generatedMesh == null)
                {
                    _generatedMesh = new Mesh();
                    _generatedMesh.name = $"{name}_GMeshShape";
                }

                gmesh.ToMesh(_generatedMesh);
                _meshFilter.sharedMesh = _generatedMesh;
            }
            finally
            {
                if (nativePoints.IsCreated)
                    nativePoints.Dispose();

                gmesh?.Dispose();
            }
        }

        private bool IsClosedEnoughToUse()
        {
            // We do not require the first point repeated at the end.
            // Ordered polygon vertices are enough.
            return points != null && points.Length >= 3;
        }

        private void OnDestroy()
        {
            if (_generatedMesh != null)
            {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(_generatedMesh);
            else
                Destroy(_generatedMesh);
#else
                Destroy(_generatedMesh);
#endif
            }
        }
    }
}