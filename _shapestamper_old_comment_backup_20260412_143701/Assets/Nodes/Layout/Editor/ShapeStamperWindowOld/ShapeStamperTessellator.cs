using System.Collections.Generic;
using UnityEngine;
using g4;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ShapeStamperTessellator
    {
        public static Mesh BuildFlatShapeMesh(ShapeCanvasDocument document)
        {
            if (document == null || document.OuterPoints == null || document.OuterEdges == null)
            {
                Debug.LogWarning("ShapeStamperTessellator: Document is null or incomplete.");
                return null;
            }

            List<Vector2> outerLoop = BuildOrderedLoop(document.OuterPoints, document.OuterEdges);
            if (outerLoop == null || outerLoop.Count < 3)
            {
                Debug.LogWarning("ShapeStamperTessellator: Failed to build ordered outer loop.");
                return null;
            }

            RemoveDuplicateClosingPoint(outerLoop);
            if (outerLoop.Count < 3)
            {
                Debug.LogWarning("ShapeStamperTessellator: Outer loop has fewer than 3 unique points.");
                return null;
            }

            List<Vector2> innerLoop = null;
            if (document.HasInnerShape && document.InnerPoints != null && document.InnerEdges != null && document.InnerPoints.Count >= 3)
            {
                innerLoop = BuildOrderedLoop(document.InnerPoints, document.InnerEdges);
                if (innerLoop != null)
                {
                    RemoveDuplicateClosingPoint(innerLoop);
                    if (innerLoop.Count < 3)
                        innerLoop = null;
                }
            }

            Vector2 sharedCenter = CalculateCenter(outerLoop);
            OffsetLoop(outerLoop, sharedCenter);
            if (innerLoop != null)
                OffsetLoop(innerLoop, sharedCenter);

            EnsureCounterClockwise(outerLoop);
            if (innerLoop != null)
                EnsureClockwise(innerLoop);

            return TriangulatePolygon(outerLoop, innerLoop);
        }

        private static Mesh TriangulatePolygon(List<Vector2> outerLoop, List<Vector2> innerLoop)
        {
            GeneralPolygon2d polygon = ToGeneralPolygon2d(outerLoop, innerLoop);

            TriangulatedPolygonGenerator generator = new TriangulatedPolygonGenerator
            {
                Polygon = polygon
            };

            generator.Generate();
            DMesh3 gmesh = generator.MakeDMesh();

            if (gmesh == null || gmesh.TriangleCount == 0)
            {
                Debug.LogWarning("ShapeStamperTessellator: Triangulator returned no triangles.");
                return null;
            }

            return ToUnityMesh(gmesh);
        }

        private static GeneralPolygon2d ToGeneralPolygon2d(List<Vector2> outerLoop, List<Vector2> innerLoop)
        {
            Polygon2d outer = new Polygon2d();
            for (int i = 0; i < outerLoop.Count; i++)
            {
                Vector2 p = outerLoop[i];
                outer.AppendVertex(new Vector2d(p.x, -p.y));
            }

            GeneralPolygon2d polygon = new GeneralPolygon2d(outer);

            if (innerLoop != null && innerLoop.Count >= 3)
            {
                Polygon2d hole = new Polygon2d();
                for (int i = 0; i < innerLoop.Count; i++)
                {
                    Vector2 p = innerLoop[i];
                    hole.AppendVertex(new Vector2d(p.x, -p.y));
                }

                polygon.AddHole(hole);
            }

            return polygon;
        }

        private static Mesh ToUnityMesh(DMesh3 gmesh)
        {
            Mesh mesh = new Mesh
            {
                name = "ShapeStampMesh"
            };

            List<Vector3> vertices = new List<Vector3>(gmesh.VertexCount);
            List<int> triangles = new List<int>(gmesh.TriangleCount * 3);
            Dictionary<int, int> vidToUnityIndex = new Dictionary<int, int>(gmesh.VertexCount);

            for (int vid = 0; vid < gmesh.MaxVertexID; vid++)
            {
                if (!gmesh.IsVertex(vid))
                    continue;

                Vector3d v = gmesh.GetVertex(vid);
                int unityIndex = vertices.Count;
                vidToUnityIndex.Add(vid, unityIndex);
                vertices.Add(new Vector3((float)v.x, (float)v.y, (float)v.z));
            }

            for (int tid = 0; tid < gmesh.MaxTriangleID; tid++)
            {
                if (!gmesh.IsTriangle(tid))
                    continue;

                Index3i tri = gmesh.GetTriangle(tid);
                triangles.Add(vidToUnityIndex[tri.a]);
                triangles.Add(vidToUnityIndex[tri.b]);
                triangles.Add(vidToUnityIndex[tri.c]);
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static List<Vector2> BuildOrderedLoop(IList<CanvasPoint> points, IList<CanvasEdge> edges)
        {
            if (points == null || edges == null || edges.Count < 3)
                return null;

            Dictionary<int, CanvasPoint> pointById = new Dictionary<int, CanvasPoint>(points.Count);
            for (int i = 0; i < points.Count; i++)
                pointById[points[i].Id] = points[i];

            Dictionary<int, CanvasEdge> edgeByStart = new Dictionary<int, CanvasEdge>(edges.Count);
            for (int i = 0; i < edges.Count; i++)
            {
                CanvasEdge edge = edges[i];
                if (edgeByStart.ContainsKey(edge.A))
                {
                    Debug.LogWarning($"ShapeStamperTessellator: Multiple outgoing edges from point {edge.A}.");
                    return null;
                }
                edgeByStart.Add(edge.A, edge);
            }

            CanvasEdge firstEdge = edges[0];
            int startPointId = firstEdge.A;
            int currentPointId = startPointId;

            HashSet<int> visitedStartPoints = new HashSet<int>();
            List<Vector2> ordered = new List<Vector2>(edges.Count);

            for (int i = 0; i < edges.Count; i++)
            {
                if (!pointById.TryGetValue(currentPointId, out CanvasPoint point))
                {
                    Debug.LogWarning($"ShapeStamperTessellator: Missing point id {currentPointId}.");
                    return null;
                }

                ordered.Add(point.Position);

                if (!edgeByStart.TryGetValue(currentPointId, out CanvasEdge edge))
                {
                    Debug.LogWarning($"ShapeStamperTessellator: No outgoing edge from point id {currentPointId}.");
                    return null;
                }

                if (!visitedStartPoints.Add(currentPointId))
                {
                    Debug.LogWarning("ShapeStamperTessellator: Loop revisited a point before clean closure.");
                    return null;
                }

                currentPointId = edge.B;
            }

            if (currentPointId != startPointId)
            {
                Debug.LogWarning("ShapeStamperTessellator: Edge walk did not close.");
                return null;
            }

            return ordered;
        }

        private static void RemoveDuplicateClosingPoint(List<Vector2> loop, float epsilon = 0.00001f)
        {
            if (loop == null || loop.Count < 2)
                return;

            if ((loop[0] - loop[loop.Count - 1]).sqrMagnitude <= epsilon * epsilon)
                loop.RemoveAt(loop.Count - 1);
        }

        private static Vector2 CalculateCenter(IList<Vector2> points)
        {
            Bounds2D bounds = CalculateBounds(points);
            return bounds.Center;
        }

        private static void OffsetLoop(List<Vector2> loop, Vector2 offset)
        {
            for (int i = 0; i < loop.Count; i++)
                loop[i] -= offset;
        }

        private static void EnsureCounterClockwise(List<Vector2> loop)
        {
            if (IsClockwise(loop))
                loop.Reverse();
        }

        private static void EnsureClockwise(List<Vector2> loop)
        {
            if (!IsClockwise(loop))
                loop.Reverse();
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
