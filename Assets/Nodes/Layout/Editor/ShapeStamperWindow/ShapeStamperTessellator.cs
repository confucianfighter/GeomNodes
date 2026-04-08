using System.Collections.Generic;
using UnityEngine;
using g4;

namespace DLN.EditorTools.ShapeStamper
{
    /// <summary>
    /// Builds a flat XY mesh from the current Shape Stamper closed loop.
    /// Intended first for solid concave 2D faces.
    /// 
    /// Notes:
    /// - Keeps geometry in local XY so face normal should point along local +Z.
    /// - Reconstructs loop order from edges instead of trusting document.Points order.
    /// - Structured so we can later swap Polygon2d -> GeneralPolygon2d for holes.
    /// </summary>
    public static class ShapeStamperTessellator
    {
        public static Mesh BuildFlatShapeMesh(ShapeCanvasDocument document)
        {
            if (document == null || document.Points == null || document.Edges == null)
            {
                Debug.LogWarning("ShapeStamperTessellator: Document is null or incomplete.");
                return null;
            }

            List<Vector2> orderedLoop = BuildOrderedLoop(document);
            if (orderedLoop == null || orderedLoop.Count < 3)
            {
                Debug.LogWarning("ShapeStamperTessellator: Failed to build ordered loop.");
                return null;
            }

            RemoveDuplicateClosingPoint(orderedLoop);

            if (orderedLoop.Count < 3)
            {
                Debug.LogWarning("ShapeStamperTessellator: Ordered loop has fewer than 3 unique points.");
                return null;
            }

            CenterLoop(orderedLoop);
            EnsureCounterClockwise(orderedLoop);

            return TriangulateSolidPolygon(orderedLoop);
        }

        private static Mesh TriangulateSolidPolygon(List<Vector2> loop)
        {
            GeneralPolygon2d outer = ToPolygon2d(loop);

            TriangulatedPolygonGenerator generator = new TriangulatedPolygonGenerator
            {
                Polygon = outer
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
        private static GeneralPolygon2d ToPolygon2d(List<Vector2> loop)
        {
            // Most likely API shape; if your local fork prefers a constructor,
            // adjust this one function only.
            Polygon2d poly = new();

            for (int i = 0; i < loop.Count; i++)
            {
                Vector2 p = loop[i];
                poly.AppendVertex(new Vector2d(p.x, -p.y));
            }

            return new GeneralPolygon2d(poly);
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

                // Keep final mesh in XY, with Z reserved for later bevel depth.
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

        /// <summary>
        /// Reconstruct loop order from the edge chain.
        /// This avoids relying on document.Points list order, which can drift after edge splits.
        /// </summary>
        private static List<Vector2> BuildOrderedLoop(ShapeCanvasDocument document)
        {
            if (document.Edges.Count < 3)
                return null;

            Dictionary<int, CanvasPoint> pointById = new Dictionary<int, CanvasPoint>(document.Points.Count);
            for (int i = 0; i < document.Points.Count; i++)
                pointById[document.Points[i].Id] = document.Points[i];

            Dictionary<int, CanvasEdge> edgeByStart = new Dictionary<int, CanvasEdge>(document.Edges.Count);
            for (int i = 0; i < document.Edges.Count; i++)
            {
                CanvasEdge edge = document.Edges[i];

                if (edgeByStart.ContainsKey(edge.A))
                {
                    Debug.LogWarning($"ShapeStamperTessellator: Multiple outgoing edges from point {edge.A}.");
                    return null;
                }

                edgeByStart.Add(edge.A, edge);
            }

            CanvasEdge firstEdge = document.Edges[0];
            int startPointId = firstEdge.A;
            int currentPointId = startPointId;

            HashSet<int> visitedStartPoints = new HashSet<int>();
            List<Vector2> ordered = new List<Vector2>(document.Edges.Count);

            for (int i = 0; i < document.Edges.Count; i++)
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

        private static void CenterLoop(List<Vector2> loop)
        {
            Bounds2D bounds = CalculateBounds(loop);
            Vector2 center = bounds.Center;

            for (int i = 0; i < loop.Count; i++)
                loop[i] -= center;
        }

        private static void EnsureCounterClockwise(List<Vector2> loop)
        {
            if (IsClockwise(loop))
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