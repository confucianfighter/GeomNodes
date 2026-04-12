using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using g4;

namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public sealed class ShapeStampPreviewMaterialSettings
    {
        public List<Material> SegmentMaterials = new();
        public List<Color> SegmentColors = new();
        public Material StartCapMaterial;
        public Material EndCapMaterial;
        public Color StartCapColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        public Color EndCapColor = new Color(0.65f, 0.65f, 0.65f, 1f);
    }

    public static class ShapeStamperProfileGenerator
    {
        private const string PreviewRootName = "ShapeStamp_ProfileRings";
        private const string PreviewMeshObjectName = "ShapeStamp_ProfileSegments";
        private const float ParallelEpsilon = 0.000001f;
        private const float DefaultLineWidth = 0.02f;
        private const float SafeEpsilon = 0.000001f;

        public static void Generate(
            ShapeCanvasDocument shapeDocument,
            ProfileCanvasDocument profileDocument,
            ShapeStampPreviewMaterialSettings materialSettings = null)
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
                CreateOrUpdateSegmentMeshPreview(segmentedMesh, result, materialSettings);

            Debug.Log(
                $"ShapeStamperProfileGenerator: Built {result.OuterRings.Count} outer ring(s), " +
                $"{result.InnerRings.Count} inner ring set(s), " +
                $"{result.ProfileSamples.Count} profile sample(s).");
        }

        public static AdaptiveShapeBuildResult BuildAdaptiveShape(
            ShapeCanvasDocument shapeDocument,
            ProfileCanvasDocument profileDocument)
        {
            ShapeStampRingBuildResult ringResult = BuildRings(shapeDocument, profileDocument);
            if (ringResult == null)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Failed to build adaptive shape result.");
                return null;
            }

            AdaptiveShapeBuildResult buildResult = new AdaptiveShapeBuildResult
            {
                RingBuildResult = ringResult
            };

            buildResult.SegmentMeshes = BuildSegmentMeshes(ringResult);

            if (ringResult.OuterLoops2D != null &&
                ringResult.OuterLoops2D.Count > 0 &&
                ringResult.ProfileSamples != null &&
                ringResult.ProfileSamples.Count > 0)
            {
                buildResult.StartCapMesh = BuildCapMesh(
    ringResult.OuterLoops2D[0],
    ringResult.InnerLoops2D.Count > 0 ? ringResult.InnerLoops2D[0] : null,
    ringResult.ProfileSamples[0].Z,
    reverseWinding: false,
    meshName: "AdaptiveShape_StartCap");

                int last = ringResult.ProfileSamples.Count - 1;
                buildResult.EndCapMesh = BuildCapMesh(
                    ringResult.OuterLoops2D[last],
                    ringResult.InnerLoops2D.Count > 0 ? ringResult.InnerLoops2D[last] : null,
                    ringResult.ProfileSamples[last].Z,
                    reverseWinding: true,
                    meshName: "AdaptiveShape_EndCap");
            }

            return buildResult;
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

            if (profileDocument.ProfilePoints == null || profileDocument.ProfilePoints.Count < 2)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Profile needs at least 2 points.");
                return null;
            }

            List<Vector2> baseOuterLoop2D = BuildOrderedLoop(shapeDocument.OuterPoints, shapeDocument.OuterEdges, out List<float> outerEdgeScales);
            if (baseOuterLoop2D == null || baseOuterLoop2D.Count < 3)
            {
                Debug.LogWarning("ShapeStamperProfileGenerator: Failed to build ordered outer loop.");
                return null;
            }

            CleanupLoopInPlace(baseOuterLoop2D);

            List<Vector2> baseInnerLoop2D = null;
            List<float> innerEdgeScales = null;
            if (shapeDocument.HasInnerShape && shapeDocument.InnerPoints != null && shapeDocument.InnerEdges != null && shapeDocument.InnerPoints.Count >= 3)
            {
                baseInnerLoop2D = BuildOrderedLoop(shapeDocument.InnerPoints, shapeDocument.InnerEdges, out innerEdgeScales);
                if (baseInnerLoop2D != null && baseInnerLoop2D.Count < 3)
                    baseInnerLoop2D = null;

                if (baseInnerLoop2D != null)
                    CleanupLoopInPlace(baseInnerLoop2D);
            }

            Vector2 sharedCenter = CalculateCenter(baseOuterLoop2D);
            OffsetLoop(baseOuterLoop2D, sharedCenter);
            if (baseInnerLoop2D != null)
                OffsetLoop(baseInnerLoop2D, sharedCenter);

            FlipLoopY(baseOuterLoop2D);
            if (baseInnerLoop2D != null)
                FlipLoopY(baseInnerLoop2D);

            EnsureCounterClockwise(baseOuterLoop2D, outerEdgeScales);
            if (baseInnerLoop2D != null)
                EnsureClockwise(baseInnerLoop2D, innerEdgeScales);

            ShapeStampRingBuildResult result = new ShapeStampRingBuildResult
            {
                BaseOuterLoop2D = new List<Vector2>(baseOuterLoop2D),
                BaseInnerLoop2D = baseInnerLoop2D != null ? new List<Vector2>(baseInnerLoop2D) : null,
                OuterEdgeScales = outerEdgeScales != null ? new List<float>(outerEdgeScales) : new List<float>(),
                InnerEdgeScales = innerEdgeScales != null ? new List<float>(innerEdgeScales) : new List<float>()
            };

            for (int i = 0; i < profileDocument.ProfilePoints.Count; i++)
            {
                ProfilePoint p = profileDocument.ProfilePoints[i];
                ProfileSample sample = new ProfileSample
                {
                    Index = i,
                    Offset = p.Position.x,
                    Z = ResolveProfilePointZ(i, profileDocument.ProfilePoints, profileDocument),
                    Point = p
                };
                result.ProfileSamples.Add(sample);

                List<float> outerDistances = BuildEdgeOffsetDistances(
                    result.BaseOuterLoop2D,
                    result.OuterEdgeScales,
                    profileDocument,
                    profileDocument.ProfilePoints,
                    i);

                List<Vector2> outerLoopForSample = OffsetClosedLoop(result.BaseOuterLoop2D, outerDistances);
                if (outerLoopForSample == null || outerLoopForSample.Count < 3)
                {
                    Debug.LogWarning($"ShapeStamperProfileGenerator: Failed to offset outer loop for profile sample {i}.");
                    return null;
                }

                CleanupLoopInPlace(outerLoopForSample);
                if (outerLoopForSample.Count < 3)
                {
                    Debug.LogWarning($"ShapeStamperProfileGenerator: Outer loop collapsed after cleanup for profile sample {i}.");
                    return null;
                }

                result.OuterLoops2D.Add(outerLoopForSample);
                result.OuterRings.Add(LiftLoopTo3D(outerLoopForSample, sample.Z));

                if (result.BaseInnerLoop2D != null && result.BaseInnerLoop2D.Count >= 3)
                {
                    List<float> innerDistances = BuildEdgeOffsetDistances(
                        result.BaseInnerLoop2D,
                        result.InnerEdgeScales,
                        profileDocument,
                        profileDocument.ProfilePoints,
                        i);

                    List<Vector2> innerLoopForSample = OffsetClosedLoop(result.BaseInnerLoop2D, innerDistances);
                    if (innerLoopForSample == null || innerLoopForSample.Count < 3)
                    {
                        Debug.LogWarning($"ShapeStamperProfileGenerator: Failed to offset inner loop for profile sample {i}.");
                        return null;
                    }

                    CleanupLoopInPlace(innerLoopForSample);
                    if (innerLoopForSample.Count < 3)
                    {
                        Debug.LogWarning($"ShapeStamperProfileGenerator: Inner loop collapsed after cleanup for profile sample {i}.");
                        return null;
                    }

                    result.InnerLoops2D.Add(innerLoopForSample);
                    result.InnerRings.Add(LiftLoopTo3D(innerLoopForSample, sample.Z));
                }
            }

            return result;
        }

        private static List<float> BuildEdgeOffsetDistances(
            IList<Vector2> loop,
            IList<float> edgeScales,
            ProfileCanvasDocument profileDocument,
            IList<ProfilePoint> profilePoints,
            int pointIndex)
        {
            List<float> distances = new List<float>();
            if (loop == null || loop.Count < 3)
                return distances;

            bool isClockwise = IsClockwise(loop);

            for (int i = 0; i < loop.Count; i++)
            {
                Vector2 a = loop[i];
                Vector2 b = loop[(i + 1) % loop.Count];
                Vector2 dir = b - a;

                if (dir.sqrMagnitude < SafeEpsilon)
                {
                    distances.Add(0f);
                    continue;
                }

                dir.Normalize();
                Vector2 outward = GetOutwardNormal(dir, isClockwise);
                EdgeProfileDrive drive = BuildEdgeProfileDrive(outward, edgeScales, i, profileDocument);

                float baseX = ResolveProfilePointX(pointIndex, profilePoints, drive);
                float distance = Mathf.Max(0f, SafeFinite(baseX * drive.Scale, 0f));
                distances.Add(distance);
            }

            return distances;
        }

        private static EdgeProfileDrive BuildEdgeProfileDrive(
            Vector2 outwardNormal,
            IList<float> edgeScales,
            int edgeIndex,
            ProfileCanvasDocument profileDocument)
        {
            outwardNormal = SafeNormalized(outwardNormal, Vector2.right);

            float rightW = Mathf.Max(0f, outwardNormal.x);
            float leftW = Mathf.Max(0f, -outwardNormal.x);
            float topW = Mathf.Max(0f, -outwardNormal.y);
            float bottomW = Mathf.Max(0f, outwardNormal.y);

            float sum = rightW + leftW + topW + bottomW;
            if (sum > SafeEpsilon)
            {
                float inv = 1f / sum;
                rightW *= inv;
                leftW *= inv;
                topW *= inv;
                bottomW *= inv;
            }
            else
            {
                rightW = leftW = topW = bottomW = 0.25f;
            }

            float blendedPadding =
                leftW * profileDocument.LeftPadding +
                rightW * profileDocument.RightPadding +
                topW * profileDocument.TopPadding +
                bottomW * profileDocument.BottomPadding;

            float blendedBorder =
                leftW * profileDocument.LeftBorder +
                rightW * profileDocument.RightBorder +
                topW * profileDocument.TopBorder +
                bottomW * profileDocument.BottomBorder;

            float scale = 1f;
            if (edgeScales != null && edgeIndex >= 0 && edgeIndex < edgeScales.Count)
                scale = Mathf.Max(0f, edgeScales[edgeIndex]);

            return new EdgeProfileDrive
            {
                LeftWeight = leftW,
                RightWeight = rightW,
                TopWeight = topW,
                BottomWeight = bottomW,
                BlendedPadding = SafeFinite(blendedPadding, 0f),
                BlendedBorder = SafeFinite(blendedBorder, 0f),
                Scale = SafeFinite(scale, 1f)
            };
        }

        private static float ResolveProfilePointZ(int pointIndex, IList<ProfilePoint> profilePoints, ProfileCanvasDocument profileDocument)
        {
            if (profilePoints == null || pointIndex < 0 || pointIndex >= profilePoints.Count)
                return 0f;

            ProfilePoint point = profilePoints[pointIndex];
            ProfileSpanLayoutData layout = ProfileSpanLayout.Build(profileDocument);
            return layout.GetZSpan(point.ZSpan).Evaluate(point.ZT);
        }

        private static float ResolveProfilePointX(int pointIndex, IList<ProfilePoint> profilePoints, EdgeProfileDrive drive)
        {
            if (profilePoints == null || pointIndex < 0 || pointIndex >= profilePoints.Count)
                return 0f;

            ProfilePoint point = profilePoints[pointIndex];

            float paddingWidth = Mathf.Max(0f, drive.BlendedPadding);
            float borderWidth = Mathf.Max(0f, drive.BlendedBorder);

            ProfileSpan paddingToContent = new ProfileSpan(0f, paddingWidth);
            ProfileSpan contentToBorder = new ProfileSpan(paddingWidth, paddingWidth + borderWidth);

            float raw;
            switch (point.XSpan)
            {
                case ProfileXSpan.PaddingToContent:
                    raw = paddingToContent.Evaluate(point.XT);
                    break;

                case ProfileXSpan.ContentToBorder:
                default:
                    raw = contentToBorder.Evaluate(point.XT);
                    break;
            }

            return Mathf.Max(0f, SafeFinite(raw * drive.Scale, 0f));
        }

        private static List<Mesh> BuildSegmentMeshes(ShapeStampRingBuildResult result)
        {
            List<Mesh> segmentMeshes = new List<Mesh>();

            if (result == null || result.OuterRings == null || result.OuterRings.Count < 2)
                return segmentMeshes;

            int segmentCount = result.OuterRings.Count - 1;
            bool hasInnerRings =
                result.InnerRings != null &&
                result.InnerRings.Count == result.OuterRings.Count;

            for (int i = 0; i < segmentCount; i++)
            {
                List<Vector3> outerA = result.OuterRings[i];
                List<Vector3> outerB = result.OuterRings[i + 1];

                List<Vector3> innerA = hasInnerRings ? result.InnerRings[i] : null;
                List<Vector3> innerB = hasInnerRings ? result.InnerRings[i + 1] : null;

                Mesh segmentMesh = BuildSingleBridgeMesh(
                    outerA,
                    outerB,
                    innerA,
                    innerB,
                    i);

                if (segmentMesh != null)
                    segmentMeshes.Add(segmentMesh);
            }

            return segmentMeshes;
        }

        private static Mesh BuildSingleBridgeMesh(
            List<Vector3> outerA,
            List<Vector3> outerB,
            List<Vector3> innerA,
            List<Vector3> innerB,
            int segmentIndex)
        {
            if (outerA == null || outerB == null)
                return null;

            if (outerA.Count < 3 || outerB.Count < 3)
                return null;

            SegmentedMeshBuilder builder = new SegmentedMeshBuilder();
            int submeshIndex = builder.AddSubmesh();

            AddRingBridge(builder, outerA, outerB, submeshIndex);

            if (innerA != null && innerB != null &&
                innerA.Count >= 3 && innerB.Count >= 3)
            {
                AddRingBridge(builder, innerB, innerA, submeshIndex);
            }

            return builder.ToMesh($"AdaptiveShape_Segment_{segmentIndex:000}");
        }

        private static Mesh BuildCapMesh(
            List<Vector2> outerLoop,
            List<Vector2> innerLoop,
            float z,
            bool reverseWinding,
            string meshName)
        {
            if (outerLoop == null || outerLoop.Count < 3)
                return null;

            SegmentedMeshBuilder builder = new SegmentedMeshBuilder();
            int submeshIndex = builder.AddSubmesh();

            AddCap(
                builder,
                outerLoop,
                innerLoop,
                z,
                submeshIndex,
                reverseWinding);

            return builder.ToMesh(meshName);
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
            for (int i = 0; i < segmentCount; i++)
                builder.AddSubmesh();

            int startCapSubmesh = builder.AddSubmesh();
            int endCapSubmesh = builder.AddSubmesh();

            for (int i = 0; i < segmentCount; i++)
            {
                AddRingBridge(builder, result.OuterRings[i], result.OuterRings[i + 1], i);

                if (result.InnerRings != null && result.InnerRings.Count == result.OuterRings.Count)
                {
                    AddRingBridge(builder, result.InnerRings[i + 1], result.InnerRings[i], i);
                }
            }

            AddCap(
    builder,
    result.OuterLoops2D[0],
    result.InnerLoops2D.Count > 0 ? result.InnerLoops2D[0] : null,
    result.ProfileSamples[0].Z,
    startCapSubmesh,
    reverseWinding: false);

            int last = result.ProfileSamples.Count - 1;
            AddCap(
                builder,
                result.OuterLoops2D[last],
                result.InnerLoops2D.Count > 0 ? result.InnerLoops2D[last] : null,
                result.ProfileSamples[last].Z,
                endCapSubmesh,
                reverseWinding: true);
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

                builder.AddTriangle(submeshIndex, a0, b0, b1);
                builder.AddTriangle(submeshIndex, a0, b1, a1);
            }
        }

        private static void CreateOrUpdateSegmentMeshPreview(
            Mesh mesh,
            ShapeStampRingBuildResult result,
            ShapeStampPreviewMaterialSettings materialSettings)
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
                mats[i] = ResolveSegmentMaterial(materialSettings, i, segmentCount);

            mats[segmentCount] = ResolveStartCapMaterial(materialSettings);
            mats[segmentCount + 1] = ResolveEndCapMaterial(materialSettings);

            mr.sharedMaterials = mats;
        }

        private static Material ResolveSegmentMaterial(ShapeStampPreviewMaterialSettings settings, int index, int count)
        {
            if (settings != null && settings.SegmentMaterials != null)
            {
                if (index >= 0 && index < settings.SegmentMaterials.Count && settings.SegmentMaterials[index] != null)
                    return settings.SegmentMaterials[index];
            }

            Color color = GetSegmentColor(settings, index, count);
            return CreatePreviewMaterial(color, $"ShapeStamp_ProfileSegment_{index}_Mat");
        }

        private static Material ResolveStartCapMaterial(ShapeStampPreviewMaterialSettings settings)
        {
            if (settings != null && settings.StartCapMaterial != null)
                return settings.StartCapMaterial;

            Color color = settings != null ? settings.StartCapColor : new Color(0.85f, 0.85f, 0.85f, 1f);
            return CreatePreviewMaterial(color, "ShapeStamp_StartCap_Mat");
        }

        private static Material ResolveEndCapMaterial(ShapeStampPreviewMaterialSettings settings)
        {
            if (settings != null && settings.EndCapMaterial != null)
                return settings.EndCapMaterial;

            Color color = settings != null ? settings.EndCapColor : new Color(0.65f, 0.65f, 0.65f, 1f);
            return CreatePreviewMaterial(color, "ShapeStamp_EndCap_Mat");
        }

        private static Color GetSegmentColor(ShapeStampPreviewMaterialSettings settings, int index, int count)
        {
            if (settings != null && settings.SegmentColors != null && index >= 0 && index < settings.SegmentColors.Count)
                return settings.SegmentColors[index];

            float t = Safe01(index, Mathf.Max(2, count));
            float gray = Mathf.Lerp(0.3f, 0.8f, t);
            return new Color(gray, gray, gray, 1f);
        }

        private static Material CreateLinePreviewMaterial()
        {
            return CreatePreviewMaterial(new Color(0.72f, 0.72f, 0.72f, 1f), "ShapeStamp_ProfileRingPreview_Mat");
        }

        private static Material CreatePreviewMaterial(Color color, string name)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Standard");

            Material mat = new Material(shader);
            mat.name = name;
            SetMaterialColor(mat, color);

            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 0f);

            return mat;
        }

        private static void SetMaterialColor(Material mat, Color color)
        {
            if (mat == null)
                return;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
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

        private static List<Vector2> OffsetClosedLoop(IList<Vector2> loop, IList<float> edgeDistances)
        {
            if (loop == null || loop.Count < 3)
                return null;

            if (edgeDistances == null || edgeDistances.Count != loop.Count)
                return null;

            bool isClockwise = IsClockwise(loop);
            int count = loop.Count;
            List<Vector2> result = new List<Vector2>(count);

            for (int i = 0; i < count; i++)
            {
                Vector2 prev = loop[(i - 1 + count) % count];
                Vector2 curr = loop[i];
                Vector2 next = loop[(i + 1) % count];

                Vector2 prevDir = SafeNormalized(curr - prev, Vector2.right);
                Vector2 nextDir = SafeNormalized(next - curr, Vector2.right);

                if (prevDir.sqrMagnitude < SafeEpsilon || nextDir.sqrMagnitude < SafeEpsilon)
                {
                    result.Add(curr);
                    continue;
                }

                int prevEdgeIndex = (i - 1 + count) % count;
                int nextEdgeIndex = i;

                float prevDistance = SafeFinite(edgeDistances[prevEdgeIndex], 0f);
                float nextDistance = SafeFinite(edgeDistances[nextEdgeIndex], 0f);

                Vector2 prevOut = GetOutwardNormal(prevDir, isClockwise);
                Vector2 nextOut = GetOutwardNormal(nextDir, isClockwise);

                Vector2 line1Point = curr + prevOut * prevDistance;
                Vector2 line2Point = curr + nextOut * nextDistance;

                if (TryIntersectLines(line1Point, prevDir, line2Point, nextDir, out Vector2 intersection))
                {
                    result.Add(intersection);
                }
                else
                {
                    Vector2 avg = prevOut * prevDistance + nextOut * nextDistance;
                    if (avg.sqrMagnitude < ParallelEpsilon)
                        avg = prevOut * prevDistance;
                    result.Add(curr + avg);
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

        private static List<Vector2> BuildOrderedLoop(IList<CanvasPoint> points, IList<CanvasEdge> edges, out List<float> orderedEdgeScales)
        {
            orderedEdgeScales = new List<float>();

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

                orderedEdgeScales.Add(Mathf.Max(0f, edge.ProfileXScale) <= 0f ? 1f : edge.ProfileXScale);

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

        private static void CleanupLoopInPlace(List<Vector2> loop, float epsilon = 0.0001f)
        {
            if (loop == null || loop.Count < 3)
                return;

            RemoveDuplicateClosingPoint(loop, epsilon);
            RemoveNearDuplicateNeighbors(loop, epsilon);
            RemoveNearlyCollinearVertices(loop, epsilon);
            RemoveNearDuplicateNeighbors(loop, epsilon);
            RemoveDuplicateClosingPoint(loop, epsilon);
        }

        private static void RemoveNearDuplicateNeighbors(List<Vector2> loop, float epsilon)
        {
            if (loop == null || loop.Count < 2)
                return;

            float epsilonSq = epsilon * epsilon;

            for (int i = loop.Count - 1; i >= 0; i--)
            {
                if (loop.Count < 2)
                    return;

                int next = (i + 1) % loop.Count;
                if (i == next)
                    continue;

                if ((loop[i] - loop[next]).sqrMagnitude <= epsilonSq)
                {
                    loop.RemoveAt(i);

                    if (loop.Count < 3)
                        return;
                }
            }
        }

        private static void RemoveNearlyCollinearVertices(List<Vector2> loop, float epsilon)
        {
            if (loop == null || loop.Count < 3)
                return;

            float epsilonSq = epsilon * epsilon;

            bool removedAny = true;
            while (removedAny && loop.Count >= 3)
            {
                removedAny = false;

                for (int i = loop.Count - 1; i >= 0; i--)
                {
                    int prev = (i - 1 + loop.Count) % loop.Count;
                    int next = (i + 1) % loop.Count;

                    Vector2 a = loop[prev];
                    Vector2 b = loop[i];
                    Vector2 c = loop[next];

                    Vector2 ab = b - a;
                    Vector2 bc = c - b;

                    if (ab.sqrMagnitude <= epsilonSq || bc.sqrMagnitude <= epsilonSq)
                    {
                        loop.RemoveAt(i);
                        removedAny = true;
                        break;
                    }

                    float cross = Mathf.Abs(ab.x * bc.y - ab.y * bc.x);
                    float dot = Vector2.Dot(ab.normalized, bc.normalized);

                    if (cross <= epsilon && dot > 0.9999f)
                    {
                        loop.RemoveAt(i);
                        removedAny = true;
                        break;
                    }
                }
            }
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

        private static void EnsureCounterClockwise(List<Vector2> loop, List<float> edgeScales = null)
        {
            if (IsClockwise(loop))
                ReverseLoopAndEdgeScalesTogether(loop, edgeScales);
        }

        private static void EnsureClockwise(List<Vector2> loop, List<float> edgeScales = null)
        {
            if (!IsClockwise(loop))
                ReverseLoopAndEdgeScalesTogether(loop, edgeScales);
        }

        private static void ReverseLoopAndEdgeScalesTogether(List<Vector2> loop, List<float> edgeScales)
        {
            if (loop == null)
                return;

            loop.Reverse();

            if (edgeScales == null || edgeScales.Count != loop.Count)
                return;

            List<float> remapped = new List<float>(edgeScales.Count);
            remapped.AddRange(edgeScales);

            int count = loop.Count;
            for (int i = 0; i < count; i++)
            {
                int originalEdgeIndex = (count - 2 - i + count) % count;
                remapped[i] = edgeScales[originalEdgeIndex];
            }

            for (int i = 0; i < count; i++)
                edgeScales[i] = remapped[i];
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

        private static float SafeFinite(float value, float fallback = 0f)
        {
            return (float.IsNaN(value) || float.IsInfinity(value)) ? fallback : value;
        }

        private static Vector2 SafeNormalized(Vector2 value, Vector2 fallback)
        {
            if (float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsInfinity(value.x) || float.IsInfinity(value.y))
                return fallback.normalized;

            if (value.sqrMagnitude < SafeEpsilon)
                return fallback.normalized;

            return value.normalized;
        }

        [System.Serializable]
        public sealed class ShapeStampRingBuildResult
        {
            public List<Vector2> BaseOuterLoop2D = new();
            public List<Vector2> BaseInnerLoop2D;
            public List<float> OuterEdgeScales = new();
            public List<float> InnerEdgeScales = new();
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
            public ProfilePoint Point;
        }

        private struct EdgeProfileDrive
        {
            public float LeftWeight;
            public float RightWeight;
            public float TopWeight;
            public float BottomWeight;
            public float BlendedPadding;
            public float BlendedBorder;
            public float Scale;
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
