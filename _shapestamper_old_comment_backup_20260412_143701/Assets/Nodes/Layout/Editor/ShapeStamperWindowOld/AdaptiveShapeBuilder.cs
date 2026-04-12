#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DLN.EditorTools.ShapeStamper
{
    public static class AdaptiveShapeBuilder
    {
        public static void Rebuild(AdaptiveShape adaptiveShape, ShapeStampPreviewMaterialSettings materialSettings = null)
        {
            if (adaptiveShape == null)
            {
                Debug.LogWarning("AdaptiveShapeBuilder: adaptiveShape is null.");
                return;
            }

            adaptiveShape.EnsureReferences();
            adaptiveShape.EnsureEditorState();

            AdaptiveShapeBuildResult buildResult = ShapeStamperProfileGenerator.BuildAdaptiveShape(
                adaptiveShape.ShapeDocument,
                adaptiveShape.ProfileDocument);

            if (buildResult == null || buildResult.RingBuildResult == null)
            {
                Debug.LogWarning("AdaptiveShapeBuilder: BuildAdaptiveShape returned null.");
                return;
            }

            adaptiveShape.EnsureGeneratedHierarchy();

            BuildSegments(adaptiveShape, buildResult, materialSettings);
            BuildCaps(adaptiveShape, buildResult, materialSettings);
            BuildDebugRings(adaptiveShape, buildResult);

            EditorUtility.SetDirty(adaptiveShape);
        }

        private static void BuildSegments(
            AdaptiveShape adaptiveShape,
            AdaptiveShapeBuildResult buildResult,
            ShapeStampPreviewMaterialSettings materialSettings)
        {
            Transform segmentsRoot = adaptiveShape.RingSegmentsRoot;
            EnsureChildCount(segmentsRoot, buildResult.SegmentCount, "Segment");

            for (int i = 0; i < buildResult.SegmentCount; i++)
            {
                Transform child = segmentsRoot.GetChild(i);
                child.name = $"Segment_{i:000}";

                AdaptiveShapeSegment segment = GetOrAddComponent<AdaptiveShapeSegment>(child.gameObject);
                MeshFilter mf = GetOrAddComponent<MeshFilter>(child.gameObject);
                MeshRenderer mr = GetOrAddComponent<MeshRenderer>(child.gameObject);

                segment.SegmentIndex = i;
                segment.MeshFilter = mf;
                segment.MeshRenderer = mr;

                ReplaceSharedMesh(mf, buildResult.SegmentMeshes[i]);

                Material segmentMaterial = ResolveSegmentMaterial(materialSettings, i, buildResult.SegmentCount);
                if (segmentMaterial != null)
                    mr.sharedMaterial = segmentMaterial;

                Transform ringChild = EnsureNamedChild(child, "ProfileRing");
                BuildSegmentProfileRingGroup(
                    ringChild,
                    segment,
                    buildResult,
                    materialSettings,
                    i);
            }
        }

        private static void BuildSegmentProfileRingGroup(
            Transform ringRoot,
            AdaptiveShapeSegment segment,
            AdaptiveShapeBuildResult buildResult,
            ShapeStampPreviewMaterialSettings materialSettings,
            int segmentIndex)
        {
            if (ringRoot == null || segment == null || buildResult == null || buildResult.RingBuildResult == null)
                return;

            var ringData = buildResult.RingBuildResult;
            bool hasOuterStart = ringData.OuterRings != null && segmentIndex >= 0 && segmentIndex < ringData.OuterRings.Count;
            bool hasOuterEnd = ringData.OuterRings != null && (segmentIndex + 1) >= 0 && (segmentIndex + 1) < ringData.OuterRings.Count;
            bool hasInnerStart = ringData.InnerRings != null && segmentIndex >= 0 && segmentIndex < ringData.InnerRings.Count;
            bool hasInnerEnd = ringData.InnerRings != null && (segmentIndex + 1) >= 0 && (segmentIndex + 1) < ringData.InnerRings.Count;

            Transform startOuter = EnsureNamedChild(ringRoot, "StartOuterRing");
            Transform endOuter = EnsureNamedChild(ringRoot, "EndOuterRing");
            Transform startInner = EnsureNamedChild(ringRoot, "StartInnerRing");
            Transform endInner = EnsureNamedChild(ringRoot, "EndInnerRing");

            ConfigureSegmentRingLine(
                startOuter.gameObject,
                materialSettings,
                segmentIndex,
                brightness: 0.90f);

            ConfigureSegmentRingLine(
                endOuter.gameObject,
                materialSettings,
                segmentIndex,
                brightness: 1.15f);

            ConfigureInnerSegmentRingLine(startInner.gameObject, new Color(1f, 0.55f, 0.2f, 1f), 0.90f);
            ConfigureInnerSegmentRingLine(endInner.gameObject, new Color(1f, 0.55f, 0.2f, 1f), 1.15f);

            SetRingPositions(
                startOuter.GetComponent<LineRenderer>(),
                hasOuterStart ? ringData.OuterRings[segmentIndex] : null);

            SetRingPositions(
                endOuter.GetComponent<LineRenderer>(),
                hasOuterEnd ? ringData.OuterRings[segmentIndex + 1] : null);

            SetRingPositions(
                startInner.GetComponent<LineRenderer>(),
                hasInnerStart ? ringData.InnerRings[segmentIndex] : null);

            SetRingPositions(
                endInner.GetComponent<LineRenderer>(),
                hasInnerEnd ? ringData.InnerRings[segmentIndex + 1] : null);

            segment.ProfileRingObject = ringRoot.gameObject;
            segment.ProfileStartOuterRingObject = startOuter.gameObject;
            segment.ProfileEndOuterRingObject = endOuter.gameObject;
            segment.ProfileStartInnerRingObject = hasInnerStart ? startInner.gameObject : null;
            segment.ProfileEndInnerRingObject = hasInnerEnd ? endInner.gameObject : null;
        }

        private static void ConfigureSegmentRingLine(
            GameObject go,
            ShapeStampPreviewMaterialSettings materialSettings,
            int segmentIndex,
            float brightness)
        {
            LineRenderer lr = GetOrAddComponent<LineRenderer>(go);
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.widthMultiplier = 0.01f;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.alignment = LineAlignment.View;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sharedMaterial = CreateDebugLineMaterial($"AdaptiveShape_ProfileRing_{segmentIndex:000}_Mat");

            Color baseColor = GetSegmentColor(materialSettings, segmentIndex, Mathf.Max(segmentIndex + 2, 2));
            Color color = ScaleColor(baseColor, brightness);
            lr.startColor = color;
            lr.endColor = color;
        }

        private static void ConfigureInnerSegmentRingLine(GameObject go, Color baseColor, float brightness)
        {
            LineRenderer lr = GetOrAddComponent<LineRenderer>(go);
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.widthMultiplier = 0.01f;
            lr.numCapVertices = 2;
            lr.numCornerVertices = 2;
            lr.alignment = LineAlignment.View;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.sharedMaterial = CreateDebugLineMaterial("AdaptiveShape_InnerProfileRing_Mat");

            Color color = ScaleColor(baseColor, brightness);
            lr.startColor = color;
            lr.endColor = color;
        }

        private static Color ScaleColor(Color color, float brightness)
        {
            return new Color(
                Mathf.Clamp01(color.r * brightness),
                Mathf.Clamp01(color.g * brightness),
                Mathf.Clamp01(color.b * brightness),
                color.a);
        }

        private static void BuildCaps(
            AdaptiveShape adaptiveShape,
            AdaptiveShapeBuildResult buildResult,
            ShapeStampPreviewMaterialSettings materialSettings)
        {
            BuildCapObject(
                adaptiveShape.StartCapRoot,
                buildResult.StartCapMesh,
                ResolveStartCapMaterial(materialSettings));

            BuildCapObject(
                adaptiveShape.EndCapRoot,
                buildResult.EndCapMesh,
                ResolveEndCapMaterial(materialSettings));
        }

        private static void BuildCapObject(Transform capRoot, Mesh mesh, Material material)
        {
            if (capRoot == null)
                return;

            MeshFilter mf = GetOrAddComponent<MeshFilter>(capRoot.gameObject);
            MeshRenderer mr = GetOrAddComponent<MeshRenderer>(capRoot.gameObject);

            ReplaceSharedMesh(mf, mesh);

            if (material != null)
                mr.sharedMaterial = material;
        }

        private static void BuildDebugRings(AdaptiveShape adaptiveShape, AdaptiveShapeBuildResult buildResult)
        {
            Transform debugRoot = adaptiveShape.DebugRoot;
            Transform outerRoot = EnsureNamedChild(debugRoot, "OuterRings");
            Transform innerRoot = EnsureNamedChild(debugRoot, "InnerRings");
            EnsureNamedChild(debugRoot, "Guides");

            List<List<Vector3>> outerRings = buildResult.RingBuildResult.OuterRings ?? new List<List<Vector3>>();
            List<List<Vector3>> innerRings = buildResult.RingBuildResult.InnerRings ?? new List<List<Vector3>>();

            EnsureRingLineChildren(outerRoot, outerRings.Count, "OuterRing", new Color(0.15f, 0.85f, 1f, 1f));
            EnsureRingLineChildren(innerRoot, innerRings.Count, "InnerRing", new Color(1f, 0.55f, 0.2f, 1f));

            for (int i = 0; i < outerRings.Count; i++)
                SetRingPositions(outerRoot.GetChild(i).GetComponent<LineRenderer>(), outerRings[i]);

            for (int i = 0; i < innerRings.Count; i++)
                SetRingPositions(innerRoot.GetChild(i).GetComponent<LineRenderer>(), innerRings[i]);
        }

        private static void EnsureRingLineChildren(Transform parent, int count, string baseName, Color color)
        {
            EnsureChildCount(parent, count, baseName);

            for (int i = 0; i < count; i++)
            {
                Transform child = parent.GetChild(i);
                child.name = $"{baseName}_{i:000}";

                LineRenderer lr = GetOrAddComponent<LineRenderer>(child.gameObject);
                lr.useWorldSpace = false;
                lr.loop = true;
                lr.widthMultiplier = 0.01f;
                lr.numCapVertices = 2;
                lr.numCornerVertices = 2;
                lr.alignment = LineAlignment.View;
                lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                lr.receiveShadows = false;
                lr.sharedMaterial = CreateDebugLineMaterial($"{baseName}_Mat");
                lr.startColor = color;
                lr.endColor = color;
            }
        }

        private static void SetRingPositions(LineRenderer lr, List<Vector3> ring)
        {
            if (lr == null)
                return;

            if (ring == null || ring.Count < 2)
            {
                lr.positionCount = 0;
                return;
            }

            lr.positionCount = ring.Count;
            lr.SetPositions(ring.ToArray());
        }

        private static void EnsureChildCount(Transform parent, int desiredCount, string baseName)
        {
            if (parent == null)
                return;

            while (parent.childCount < desiredCount)
            {
                GameObject go = new GameObject($"{baseName}_{parent.childCount:000}");
                Undo.RegisterCreatedObjectUndo(go, $"Create {baseName}");
                go.transform.SetParent(parent, false);
            }

            while (parent.childCount > desiredCount)
            {
                Transform child = parent.GetChild(parent.childCount - 1);
                if (child != null)
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        private static Transform EnsureNamedChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            Transform existing = parent.Find(childName);
            if (existing != null)
                return existing;

            GameObject go = new GameObject(childName);
            Undo.RegisterCreatedObjectUndo(go, $"Create {childName}");
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            if (!go.TryGetComponent(out T component))
                component = Undo.AddComponent<T>(go);
            return component;
        }

        private static void ReplaceSharedMesh(MeshFilter mf, Mesh mesh)
        {
            if (mf == null)
                return;

            if (mf.sharedMesh != null && mf.sharedMesh != mesh)
            {
                Object oldMesh = mf.sharedMesh;
                mf.sharedMesh = null;
                Object.DestroyImmediate(oldMesh);
            }

            mf.sharedMesh = mesh;
        }

        private static Material ResolveSegmentMaterial(ShapeStampPreviewMaterialSettings settings, int index, int count)
        {
            if (settings != null &&
                settings.SegmentMaterials != null &&
                index >= 0 &&
                index < settings.SegmentMaterials.Count &&
                settings.SegmentMaterials[index] != null)
            {
                return settings.SegmentMaterials[index];
            }

            return CreatePreviewMaterial(
                GetSegmentColor(settings, index, count),
                $"AdaptiveShape_Segment_{index:000}_Mat");
        }

        private static Material ResolveStartCapMaterial(ShapeStampPreviewMaterialSettings settings)
        {
            if (settings != null && settings.StartCapMaterial != null)
                return settings.StartCapMaterial;

            Color c = settings != null ? settings.StartCapColor : new Color(0.85f, 0.85f, 0.85f, 1f);
            return CreatePreviewMaterial(c, "AdaptiveShape_StartCap_Mat");
        }

        private static Material ResolveEndCapMaterial(ShapeStampPreviewMaterialSettings settings)
        {
            if (settings != null && settings.EndCapMaterial != null)
                return settings.EndCapMaterial;

            Color c = settings != null ? settings.EndCapColor : new Color(0.65f, 0.65f, 0.65f, 1f);
            return CreatePreviewMaterial(c, "AdaptiveShape_EndCap_Mat");
        }

        private static Color GetSegmentColor(ShapeStampPreviewMaterialSettings settings, int index, int count)
        {
            if (settings != null &&
                settings.SegmentColors != null &&
                index >= 0 &&
                index < settings.SegmentColors.Count)
            {
                return settings.SegmentColors[index];
            }

            float t = count <= 1 ? 0f : Mathf.Clamp01(index / (float)(count - 1));
            float gray = Mathf.Lerp(0.3f, 0.8f, t);
            return new Color(gray, gray, gray, 1f);
        }

        private static Material CreateDebugLineMaterial(string name)
        {
            return CreatePreviewMaterial(new Color(0.75f, 0.75f, 0.75f, 1f), name);
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

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 0f);

            return mat;
        }
    }
}
#endif
