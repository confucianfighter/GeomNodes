using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using CodeSmile.GraphMesh;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ShapeStamperBakeService
    {
        public static void BakeShapeFaceToScene(ShapeCanvasDocument shapeDocument)
        {
            if (shapeDocument == null || shapeDocument.Points == null || shapeDocument.Points.Count < 3)
            {
                Debug.LogWarning("Shape Stamper: Need at least 3 shape points to bake.");
                return;
            }

            Mesh mesh = BuildMeshFromShape(shapeDocument);
            if (mesh == null)
            {
                Debug.LogWarning("Shape Stamper: Failed to build mesh.");
                return;
            }

            GameObject go = new GameObject("ShapeStamp_Baked");
            Undo.RegisterCreatedObjectUndo(go, "Bake Shape Stamp");

            MeshFilter meshFilter = go.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = go.AddComponent<MeshRenderer>();

            meshFilter.sharedMesh = mesh;

            if (Selection.activeGameObject != null)
            {
                MeshRenderer selectedRenderer = Selection.activeGameObject.GetComponent<MeshRenderer>();
                if (selectedRenderer != null && selectedRenderer.sharedMaterial != null)
                    meshRenderer.sharedMaterial = selectedRenderer.sharedMaterial;
            }

            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
        }

        public static Mesh BuildMeshFromShape(ShapeCanvasDocument shapeDocument)
        {
            List<Vector2> orderedLoop = BuildOrderedLoop(shapeDocument);
            if (orderedLoop == null || orderedLoop.Count < 3)
            {
                Debug.LogWarning("Shape Stamper: Could not build ordered loop from edges.");
                return null;
            }

            NativeArray<float3> nativePoints = default;

            try
            {
                nativePoints = BuildNativePointsXYCounterClockwise(orderedLoop, Allocator.Temp);

                using GMesh gmesh = new GMesh(nativePoints);

                Mesh mesh = new Mesh
                {
                    name = "ShapeStampMesh"
                };

                gmesh.ToMesh(mesh);
                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                return mesh;
            }
            finally
            {
                if (nativePoints.IsCreated)
                    nativePoints.Dispose();
            }
        }

        private static List<Vector2> BuildOrderedLoop(ShapeCanvasDocument document)
        {
            if (document.Edges == null || document.Edges.Count < 3)
                return null;

            Dictionary<int, CanvasPoint> pointsById = new Dictionary<int, CanvasPoint>();
            for (int i = 0; i < document.Points.Count; i++)
                pointsById[document.Points[i].Id] = document.Points[i];

            Dictionary<int, CanvasEdge> edgeByStart = new Dictionary<int, CanvasEdge>();
            for (int i = 0; i < document.Edges.Count; i++)
            {
                CanvasEdge edge = document.Edges[i];
                edgeByStart[edge.A] = edge;
            }

            CanvasEdge firstEdge = document.Edges[0];
            int startPointId = firstEdge.A;
            int currentPointId = startPointId;

            List<Vector2> ordered = new List<Vector2>(document.Edges.Count);
            HashSet<int> visitedStarts = new HashSet<int>();

            for (int i = 0; i < document.Edges.Count; i++)
            {
                if (!pointsById.TryGetValue(currentPointId, out CanvasPoint point))
                    return null;

                ordered.Add(point.Position);

                if (!edgeByStart.TryGetValue(currentPointId, out CanvasEdge edge))
                    return null;

                if (!visitedStarts.Add(currentPointId))
                    return null;

                currentPointId = edge.B;
            }

            if (currentPointId != startPointId)
                return null;

            return ordered;
        }

        private static NativeArray<float3> BuildNativePointsXYCounterClockwise(IList<Vector2> points, Allocator allocator)
        {
            int count = points.Count;
            NativeArray<float3> nativePoints = new NativeArray<float3>(count, allocator);

            Bounds2D bounds = CalculateBounds(points);
            Vector2 center = bounds.Center;
            bool isClockwise = IsClockwise(points);

            for (int i = 0; i < count; i++)
            {
                int sourceIndex = isClockwise ? (count - 1 - i) : i;
                Vector2 p = points[sourceIndex] - center;
                nativePoints[i] = new float3(p.x, p.y, 0f);
            }

            return nativePoints;
        }

        private static bool IsClockwise(IList<Vector2> points)
        {
            float signedAreaTwice = 0f;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                signedAreaTwice += (a.x * b.y) - (b.x * a.y);
            }

            return signedAreaTwice < 0f;
        }

        private static Bounds2D CalculateBounds(IList<Vector2> points)
        {
            Vector2 min = points[0];
            Vector2 max = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                Vector2 p = points[i];
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return new Bounds2D(min, max);
        }

        private readonly struct Bounds2D
        {
            public readonly Vector2 Min;
            public readonly Vector2 Max;

            public Vector2 Center => (Min + Max) * 0.5f;

            public Bounds2D(Vector2 min, Vector2 max)
            {
                Min = min;
                Max = max;
            }
        }
    }
}