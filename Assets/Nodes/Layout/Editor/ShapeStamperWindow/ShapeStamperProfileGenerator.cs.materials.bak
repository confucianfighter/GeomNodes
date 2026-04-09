using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using g4;

namespace DLN.EditorTools.ShapeStamper
{
    /// <summary>
    /// Debug-first profile generator.
    ///
    /// Current behavior:
    /// - Builds ordered outer/inner loops
    /// - Converts from centered 2D loop space into mesh-space
    /// - Applies a first-pass parallel closed-loop offset using profile X
    /// - Places each ring at profile Y as Z in 3D
    /// - Draws each ring as a LineRenderer child under a preview root
    /// - Builds a segmented bridge preview mesh:
    ///     * one submesh per profile segment
    ///     * one submesh for start cap
    ///     * one submesh for end cap
    ///
    /// Notes:
    /// - This is still a preview/debug seam.
    /// - Ring placement remains authoritative. The line renderers stay visible.
    /// - If later ring counts diverge, that is where MeshStitchLoops becomes more attractive.
    /// </summary>
    public static class ShapeStamperProfileGenerator
    {
        private const string PreviewRootName = "ShapeStamp_ProfileRings";
        private const string PreviewMeshObjectName = "ShapeStamp_ProfileSegments";
        private const float ParallelEpsilon = 0.000001f;
        private const float DefaultLineWidth = 0.02f;

        public static void Generate(ShapeCanvasDocument shapeDocument, ProfileCanvasDocument profileDocument)
        {
            ShapeStampRingBuildResult result = BuildRings(shapeDocument, profileDocument);

            if (result == null)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Failed to build profile rings.");
                return;
            }

            CreateOrUpdateRingPreview(result);

            Mesh segmentedMesh = BuildSegmentedBridgeMesh(result);
            if (segmentedMesh != null)
                CreateOrUpdateSegmentMeshPreview(segmentedMesh, result);

            Debug.Log(
                $"ShapeStamperProfileGenerator: Built {result.OuterRings.Count} outer ring(s), " +
                $"{result.InnerRings.Count} inner ring set(s), " +
                $"{result.ProfileSamples.Count} profile sample(s).");
        }

        public static ShapeStampRingBuildResult BuildRings(
            ShapeCanvasDocument shapeDocument,
            ProfileCanvasDocument profileDocument)
        {
            if (shapeDocument == null || profileDocument == null)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Missing shape or profile document.");
                return null;
            }

            if (shapeDocument.OuterPoints == null || shapeDocument.OuterEdges == null || shapeDocument.OuterPoints.Count < 3)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Outer shape is incomplete.");
                return null;
            }

            if (profileDocument.Points == null || profileDocument.Points.Count < 2)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Profile needs at least 2 points.");
                return null;
            }

            List<Vector2> baseOuterLoop2D = BuildOrderedLoop(shapeDocument.OuterPoints, shapeDocument.OuterEdges);
            if (baseOuterLoop2D == null || baseOuterLoop2D.Count < 3)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Failed to build ordered outer loop.");
                return null;
            }

            List<Vector2> baseInnerLoop2D = null;
            if (shapeDocument.HasInnerShape && shapeDocument.InnerPoints != null && shapeDocument.InnerEdges != null && shapeDocument.InnerPoints.Count >= 3)
            {
                baseInnerLoop2D = BuildOrderedLoop(shapeDocument.InnerPoints, shapeDocument.InnerEdges);
                if (baseInnerLoop2D != null && baseInnerLoop2D.Count < 3)
                    baseInnerLoop2D = null;
            }

            Vector2 sharedCenter = CalculateCenter(baseOuterLoop2D);
            OffsetLoop(baseOuterLoop2D, sharedCenter);
            if (baseInnerLoop2D != null)
                OffsetLoop(baseInnerLoop2D, sharedCenter);

            // Convert once from canvas-like space into mesh-space.
            FlipLoopY(baseOuterLoop2D);
            if (baseInnerLoop2D != null)
                FlipLoopY(baseInnerLoop2D);

            EnsureCounterClockwise(baseOuterLoop2D);
            if (baseInnerLoop2D != null)
                EnsureClockwise(baseInnerLoop2D);

            ShapeStampRingBuildResult result = new ShapeStampRingBuildResult
            {
                BaseOuterLoop2D = new List<Vector2>(baseOuterLoop2D),
                BaseInnerLoop2D = baseInnerLoop2D != null ? new List<Vector2>(baseInnerLoop2D) : null
            };

            for (int i = 0; i < profileDocument.Points.Count; i++)
            {
                CanvasPoint p = profileDocument.Points[i];
                ProfileSample sample = new ProfileSample
                {
                    Index = i,
                    Offset = p.Position.x,
                    Z = p.Position.y
                };
                result.ProfileSamples.Add(sample);

                List<Vector2> outerLoopForSample = OffsetClosedLoop(result.BaseOuterLoop2D, sample.Offset);
                if (outerLoopForSample == null || outerLoopForSample.Count < 3)
                {
                    Debug.LogWarning($"ShapeStamperProfileGenerator: Failed to offset outer loop for profile sample {i}.");
                    return null;
                }

                result.OuterLoops2D.Add(outerLoopForSample);
                result.OuterRings.Add(LiftLoopTo3D(outerLoopForSample, sample.Z));

                if (result.BaseInnerLoop2D != null && result.BaseInnerLoop2D.Count >= 3)
                {
                    List<Vector2> innerLoopForSample = OffsetClosedLoop(result.BaseInnerLoop2D, sample.Offset);
                    if (innerLoopForSample == null || innerLoopForSample.Count < 3)
                    {
                        Debug.LogWarning($"ShapeStamperProfileGenerator: Failed to offset inner loop for profile sample {i}.");
                        return null;
                    }

                    result.InnerLoops2D.Add(innerLoopForSample);
                    result.InnerRings.Add(LiftLoopTo3D(innerLoopForSample, sample.Z));
                }
            }

            return result;
        }

        private static void CreateOrUpdateRingPreview(ShapeStampRingBuildResult result)
        {
            GameObject root = GameObject.Find(PreviewRootName);
            if (root == null)
            {
                root = new GameObject(PreviewRootName);
                Undo.RegisterCreatedObjectUndo(root, "Create ShapeStamp Profile Ring Preview");
            }

            ClearChildrenImmediate(root.transform);

            Material sharedMaterial = CreateLinePreviewMaterial();
            float width = ComputePreviewWidth(result);

            for (int i = 0; i < result.OuterRings.Count; i++)
            {
                CreateRingObject(
                    parent: root.transform,
                    name: $"OuterRing_{i}",
                    ring: result.OuterRings[i],
                    color: Color.Lerp(new Color(0.2f, 1f, 0.3f), new Color(0.1f, 0.5f, 1f), Safe01(i, result.OuterRings.Count)),
                    width: width,
                    sharedMaterial: sharedMaterial);
            }

            for (int i = 0; i < result.InnerRings.Count; i++)
            {
                CreateRingObject(
                    parent: root.transform,
                    name: $"InnerRing_{i}",
                    ring: result.InnerRings[i],
                    color: Color.Lerp(new Color(1f, 0.6f, 0.2f), new Color(1f, 0.2f, 0.7f), Safe01(i, result.InnerRings.Count)),
                    width: width,
                    sharedMaterial: sharedMaterial);
            }
        }

        private static void CreateRingObject(
            Transform parent,
            string name,
            List<Vector3> ring,
            Color color,
            float width,
            Material sharedMaterial)
        {
            if (ring == null || ring.Count < 2)
                return;

            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.positionCount = ring.Count;
            lr.widthMultiplier = width;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.sharedMaterial = sharedMaterial;
            lr.startColor = color;
            lr.endColor = color;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.alignment = LineAlignment.View;

            lr.SetPositions(ring.ToArray());
        }

        private static Mesh BuildSegmentedBridgeMesh(ShapeStampRingBuildResult result)
        {
            if (result == null || result.OuterRings == null || result.OuterRings.Count < 2)
                return null;

            SegmentedMeshBuilder builder = new SegmentedMeshBuilder();

            int segmentCount = result.OuterRings.Count - 1;

            // One submesh per profile segment.
            for (int i = 0; i < segmentCount; i++)
                builder.AddSubmesh();

            int startCapSubmesh = builder.AddSubmesh();
            int endCapSubmesh = builder.AddSubmesh();

            // Segment walls
            for (int i = 0; i < segmentCount; i++)
            {
                AddRingBridge(builder, result.OuterRings[i], result.OuterRings[i + 1], i);

                if (result.InnerRings != null && result.InnerRings.Count == result.OuterRings.Count)
                {
                    // Reverse bridge direction so inner wall normals face into the hole properly.
                    AddRingBridge(builder, result.InnerRings[i + 1], result.InnerRings[i], i);
                }
            }

            // Caps
            AddCap(
                builder,
                result.OuterLoops2D[0],
                result.InnerLoops2D.Count > 0 ? result.InnerLoops2D[0] : null,
                result.ProfileSamples[0].Z,
                startCapSubmesh,
                reverseWinding: true);

            int last = result.ProfileSamples.Count - 1;
            AddCap(
                builder,
                result.OuterLoops2D[last],
                result.InnerLoops2D.Count > 0 ? result.InnerLoops2D[last] : null,
                result.ProfileSamples[last].Z,
                endCapSubmesh,
                reverseWinding: false);

            return builder.ToMesh("ShapeStamp_ProfileSegmentsMesh");
        }

        private static void AddCap(
            SegmentedMeshBuilder builder,
            List<Vector2> outerLoop,
            List<Vector2> innerLoop,
            float z,
            int submeshIndex,
            bool reverseWinding)
        {
            if (outerLoop == null || outerLoop.Count < 3)
                return;

            GeneralPolygon2d polygon = ToGeneralPolygon2d(outerLoop, innerLoop);

            TriangulatedPolygonGenerator generator = new TriangulatedPolygonGenerator
            {
                Polygon = polygon
            };

            generator.Generate();
            DMesh3 gmesh = generator.MakeDMesh();
            if (gmesh == null || gmesh.TriangleCount == 0)
                return;

            Dictionary<int, int> vidToBuilder = new Dictionary<int, int>(gmesh.VertexCount);

            for (int vid = 0; vid < gmesh.MaxVertexID; vid++)
            {
                if (!gmesh.IsVertex(vid))
                    continue;

                Vector3d v = gmesh.GetVertex(vid);
                int builderIndex = builder.AddVertex(new Vector3((float)v.x, (float)v.y, z));
                vidToBuilder.Add(vid, builderIndex);
            }

            for (int tid = 0; tid < gmesh.MaxTriangleID; tid++)
            {
                if (!gmesh.IsTriangle(tid))
                    continue;

                Index3i tri = gmesh.GetTriangle(tid);
                int a = vidToBuilder[tri.a];
                int b = vidToBuilder[tri.b];
                int c = vidToBuilder[tri.c];

                if (reverseWinding)
                    builder.AddTriangle(submeshIndex, a, c, b);
                else
                    builder.AddTriangle(submeshIndex, a, b, c);
            }
        }

        private static void AddRingBridge(SegmentedMeshBuilder builder, List<Vector3> ringA, List<Vector3> ringB, int submeshIndex)
        {
            if (ringA == null || ringB == null)
                return;

            if (ringA.Count < 3 || ringB.Count < 3)
                return;

            if (ringA.Count != ringB.Count)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Ring bridge currently requires equal vertex counts.");
                return;
            }

            int count = ringA.Count;
            List<int> aIndices = new List<int>(count);
            List<int> bIndices = new List<int>(count);

            for (int i = 0; i < count; i++)
            {
                aIndices.Add(builder.AddVertex(ringA[i]));
                bIndices.Add(builder.AddVertex(ringB[i]));
            }

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;

                int a0 = aIndices[i];
                int a1 = aIndices[next];
                int b0 = bIndices[i];
                int b1 = bIndices[next];

                builder.AddTriangle(submeshIndex, a0, b1, b0);
                builder.AddTriangle(submeshIndex, a0, a1, b1);
            }
        }

        private static void CreateOrUpdateSegmentMeshPreview(Mesh mesh, ShapeStampRingBuildResult result)
        {
            GameObject go = GameObject.Find(PreviewMeshObjectName);
            if (go == null)
            {
                go = new GameObject(PreviewMeshObjectName);
                Undo.RegisterCreatedObjectUndo(go, "Create ShapeStamp Profile Segment Preview");
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
            }

            MeshFilter mf = go.GetComponent<MeshFilter>();
            if (mf == null)
                mf = go.AddComponent<MeshFilter>();

            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr == null)
                mr = go.AddComponent<MeshRenderer>();

            if (mf.sharedMesh != null)
            {
                Object oldMesh = mf.sharedMesh;
                mf.sharedMesh = null;
                Object.DestroyImmediate(oldMesh);
            }

            mf.sharedMesh = mesh;

            int segmentCount = Mathf.Max(0, result.OuterRings.Count - 1);
            int totalSubmeshes = segmentCount + 2;

            Material[] mats = new Material[totalSubmeshes];
            for (int i = 0; i < segmentCount; i++)
                mats[i] = CreateSegmentMaterial(i, segmentCount);

            mats[segmentCount] = CreateCapMaterial(new Color(0.85f, 0.85f, 0.85f));
            mats[segmentCount + 1] = CreateCapMaterial(new Color(0.65f, 0.65f, 0.65f));

            mr.sharedMaterials = mats;
        }

        private static Material CreateLinePreviewMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = "ShapeStamp_ProfileRingPreview_Mat";
            return mat;
        }

        private static Material CreateSegmentMaterial(int index, int count)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader);
            mat.name = $"ShapeStamp_ProfileSegment_{index}_Mat";
            Color color = Color.Lerp(
                new Color(0.25f, 0.85f, 1f),
                new Color(1f, 0.55f, 0.2f),
                Safe01(index, Mathf.Max(2, count))
            );

            if (mat.HasProperty("_Color"))
                mat.color = color;
            else if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            return mat;
        }

        private static Material CreateCapMaterial(Color color)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader);
            mat.name = "ShapeStamp_ProfileCap_Mat";

            if (mat.HasProperty("_Color"))
                mat.color = color;
            else if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            return mat;
        }

        private static void ClearChildrenImmediate(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }

        private static float ComputePreviewWidth(ShapeStampRingBuildResult result)
        {
            if (result == null || result.BaseOuterLoop2D == null || result.BaseOuterLoop2D.Count == 0)
                return DefaultLineWidth;

            Vector2 min = result.BaseOuterLoop2D[0];
            Vector2 max = result.BaseOuterLoop2D[0];

            for (int i = 1; i < result.BaseOuterLoop2D.Count; i++)
            {
                Vector2 p = result.BaseOuterLoop2D[i];
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            float size = Mathf.Max(max.x - min.x, max.y - min.y);
            return Mathf.Max(0.01f, size * 0.015f);
        }

        private static float Safe01(int index, int count)
        {
            if (count <= 1)
                return 0f;
            return Mathf.Clamp01(index / (float)(count - 1));
        }

        private static List<Vector2> OffsetClosedLoop(IList<Vector2> loop, float distance)
        {
            if (loop == null || loop.Count < 3)
                return null;

            if (Mathf.Abs(distance) < 0.000001f)
                return new List<Vector2>(loop);

            bool isClockwise = IsClockwise(loop);
            int count = loop.Count;
            List<Vector2> result = new List<Vector2>(count);

            for (int i = 0; i < count; i++)
            {
                Vector2 prev = loop[(i - 1 + count) % count];
                Vector2 curr = loop[i];
                Vector2 next = loop[(i + 1) % count];

                Vector2 prevDir = (curr - prev).normalized;
                Vector2 nextDir = (next - curr).normalized;

                if (prevDir.sqrMagnitude < ParallelEpsilon || nextDir.sqrMagnitude < ParallelEpsilon)
                {
                    result.Add(curr);
                    continue;
                }

                Vector2 prevOut = GetOutwardNormal(prevDir, isClockwise);
                Vector2 nextOut = GetOutwardNormal(nextDir, isClockwise);

                Vector2 line1Point = curr + prevOut * distance;
                Vector2 line2Point = curr + nextOut * distance;

                if (TryIntersectLines(line1Point, prevDir, line2Point, nextDir, out Vector2 intersection))
                {
                    result.Add(intersection);
                }
                else
                {
                    Vector2 avg = prevOut + nextOut;
                    if (avg.sqrMagnitude < ParallelEpsilon)
                        avg = prevOut;
                    avg.Normalize();
                    result.Add(curr + avg * distance);
                }
            }

            return result;
        }

        private static Vector2 GetOutwardNormal(Vector2 dir, bool isClockwise)
        {
            return isClockwise
                ? new Vector2(-dir.y, dir.x)
                : new Vector2(dir.y, -dir.x);
        }

        private static bool TryIntersectLines(Vector2 p0, Vector2 d0, Vector2 p1, Vector2 d1, out Vector2 intersection)
        {
            float cross = Cross(d0, d1);
            if (Mathf.Abs(cross) < ParallelEpsilon)
            {
                intersection = default;
                return false;
            }

            Vector2 delta = p1 - p0;
            float t = Cross(delta, d1) / cross;
            intersection = p0 + d0 * t;
            return true;
        }

        private static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
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
                    Debug.LogWarning($"ShapeStamperProfileGenerator: Multiple outgoing edges from point {edge.A}.");
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
                    Debug.LogWarning($"ShapeStamperProfileGenerator: Missing point id {currentPointId}.");
                    return null;
                }

                ordered.Add(point.Position);

                if (!edgeByStart.TryGetValue(currentPointId, out CanvasEdge edge))
                {
                    Debug.LogWarning($"ShapeStamperProfileGenerator: No outgoing edge from point id {currentPointId}.");
                    return null;
                }

                if (!visitedStartPoints.Add(currentPointId))
                {
                    Debug.LogWarning("ShapeStamperProfileGenerator: Loop revisited a point before clean closure.");
                    return null;
                }

                currentPointId = edge.B;
            }

            if (currentPointId != startPointId)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Edge walk did not close.");
                return null;
            }

            RemoveDuplicateClosingPoint(ordered);
            return ordered;
        }

        private static List<Vector3> LiftLoopTo3D(IList<Vector2> loop, float z)
        {
            List<Vector3> ring = new List<Vector3>(loop.Count);
            for (int i = 0; i < loop.Count; i++)
            {
                Vector2 p = loop[i];
                ring.Add(new Vector3(p.x, p.y, z));
            }
            return ring;
        }

        private static GeneralPolygon2d ToGeneralPolygon2d(List<Vector2> outerLoop, List<Vector2> innerLoop)
        {
            Polygon2d outer = new Polygon2d();
            for (int i = 0; i < outerLoop.Count; i++)
            {
                Vector2 p = outerLoop[i];
                outer.AppendVertex(new Vector2d(p.x, p.y));
            }

            GeneralPolygon2d polygon = new GeneralPolygon2d(outer);

            if (innerLoop != null && innerLoop.Count >= 3)
            {
                Polygon2d hole = new Polygon2d();
                for (int i = 0; i < innerLoop.Count; i++)
                {
                    Vector2 p = innerLoop[i];
                    hole.AppendVertex(new Vector2d(p.x, p.y));
                }

                polygon.AddHole(hole);
            }

            return polygon;
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
            Vector2 min = points[0];
            Vector2 max = points[0];

            for (int i = 1; i < points.Count; i++)
            {
                Vector2 p = points[i];
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            return (min + max) * 0.5f;
        }

        private static void OffsetLoop(List<Vector2> loop, Vector2 offset)
        {
            for (int i = 0; i < loop.Count; i++)
                loop[i] -= offset;
        }

        private static void FlipLoopY(List<Vector2> loop)
        {
            for (int i = 0; i < loop.Count; i++)
            {
                Vector2 p = loop[i];
                loop[i] = new Vector2(p.x, -p.y);
            }
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

        [System.Serializable]
        public sealed class ShapeStampRingBuildResult
        {
            public List<Vector2> BaseOuterLoop2D = new();
            public List<Vector2> BaseInnerLoop2D;
            public List<List<Vector2>> OuterLoops2D = new();
            public List<List<Vector2>> InnerLoops2D = new();
            public List<List<Vector3>> OuterRings = new();
            public List<List<Vector3>> InnerRings = new();
            public List<ProfileSample> ProfileSamples = new();
        }

        [System.Serializable]
        public struct ProfileSample
        {
            public int Index;
            public float Offset;
            public float Z;
        }

        private sealed class SegmentedMeshBuilder
        {
            private readonly List<Vector3> _vertices = new();
            private readonly List<List<int>> _submeshTriangles = new();

            public int AddSubmesh()
            {
                _submeshTriangles.Add(new List<int>());
                return _submeshTriangles.Count - 1;
            }

            public int AddVertex(Vector3 v)
            {
                _vertices.Add(v);
                return _vertices.Count - 1;
            }

            public void AddTriangle(int submeshIndex, int a, int b, int c)
            {
                _submeshTriangles[submeshIndex].Add(a);
                _submeshTriangles[submeshIndex].Add(b);
                _submeshTriangles[submeshIndex].Add(c);
            }

            public Mesh ToMesh(string name)
            {
                Mesh mesh = new Mesh
                {
                    name = name
                };

                mesh.SetVertices(_vertices);
                mesh.subMeshCount = _submeshTriangles.Count;

                for (int i = 0; i < _submeshTriangles.Count; i++)
                    mesh.SetTriangles(_submeshTriangles[i], i);

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();
                return mesh;
            }
        }
    }
}
