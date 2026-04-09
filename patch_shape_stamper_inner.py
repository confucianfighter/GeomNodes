from pathlib import Path
import textwrap

ROOT = Path.cwd()

WINDOW_PATH = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperWindow.cs"
GENERATOR_PATH = ROOT / "Assets/Nodes/Layout/Editor/ShapeStamperWindow/ShapeStamperProfileGenerator.cs"

for path in (WINDOW_PATH, GENERATOR_PATH):
    if not path.exists():
        raise FileNotFoundError(f"Missing file: {path}")

for path in (WINDOW_PATH, GENERATOR_PATH):
    backup = path.with_suffix(path.suffix + ".materials.bak")
    if not backup.exists():
        backup.write_text(path.read_text(encoding="utf-8"), encoding="utf-8")

window_content = r'''
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public class ShapeStamperWindow : EditorWindow
    {
        private const float TopBarHeight = 260f;
        private const float CanvasPadding = 12f;

        private const float DividerWidth = 6f;
        private const float DividerHitWidth = 12f;
        private const float MinPanelWidth = 120f;

        [SerializeField] private ShapeCanvasDocument shapeDocument = new();
        [SerializeField] private ProfileCanvasDocument profileDocument = new();
        [SerializeField] private Vector2 canvasPercentOfWindow = new Vector2(0.8f, 0.8f);
        [SerializeField] private float verticalDividerPercent = 0.65f;

        [SerializeField] private CanvasSelection shapeSelection = new();
        [SerializeField] private CanvasInteractionState shapeInteraction = new();
        [SerializeField] private CanvasViewState shapeView = new();

        [SerializeField] private CanvasSelection profileSelection = new();
        [SerializeField] private CanvasInteractionState profileInteraction = new();
        [SerializeField] private CanvasViewState profileView = new();

        [SerializeField] private bool showMaterialSettings = true;
        [SerializeField] private List<Material> segmentMaterials = new();
        [SerializeField] private Material startCapMaterial;
        [SerializeField] private Material endCapMaterial;

        private EditorCanvas _shapeCanvas;
        private EditorCanvas _profileCanvas;

        private ICanvasToolPolicy _shapePolicy;
        private ICanvasToolPolicy _profilePolicy;

        private bool _isDraggingDivider;

        [MenuItem("Tools/DLN/Shape Stamper")]
        public static void Open()
        {
            ShapeStamperWindow window = GetWindow<ShapeStamperWindow>();
            window.titleContent = new GUIContent("Shape Stamper");
            window.minSize = new Vector2(700f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            shapeDocument ??= new ShapeCanvasDocument();
            profileDocument ??= new ProfileCanvasDocument();

            shapeSelection ??= new CanvasSelection();
            shapeInteraction ??= new CanvasInteractionState();
            shapeView ??= new CanvasViewState();

            profileSelection ??= new CanvasSelection();
            profileInteraction ??= new CanvasInteractionState();
            profileView ??= new CanvasViewState();

            segmentMaterials ??= new List<Material>();

            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            _shapePolicy ??= new ShapeCanvasPolicy(shapeDocument);
            _profilePolicy ??= new ProfileCanvasPolicy(profileDocument);

            _shapeCanvas = new EditorCanvas(shapeDocument, _shapePolicy, shapeSelection, shapeInteraction, shapeView);
            _profileCanvas = new EditorCanvas(profileDocument, _profilePolicy, profileSelection, profileInteraction, profileView);
        }

        private void OnGUI()
        {
            shapeDocument ??= new ShapeCanvasDocument();
            profileDocument ??= new ProfileCanvasDocument();
            segmentMaterials ??= new List<Material>();

            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            DrawTopBar();

            Rect fullCanvasArea = new Rect(
                0f,
                TopBarHeight,
                position.width,
                Mathf.Max(0f, position.height - TopBarHeight)
            );

            ComputeSplitRects(fullCanvasArea, out Rect leftPanelRect, out Rect dividerRect, out Rect dividerHitRect, out Rect rightPanelRect);
            HandleDividerInput(dividerHitRect);

            Rect leftAllowedRect = ShapeCanvasUtility.GetAllowedCanvasRect(leftPanelRect, canvasPercentOfWindow, CanvasPadding);
            Rect rightAllowedRect = ShapeCanvasUtility.GetAllowedCanvasRect(rightPanelRect, canvasPercentOfWindow, CanvasPadding);

            DrawPanelBackground(leftPanelRect);
            DrawPanelBackground(rightPanelRect);
            DrawDivider(dividerRect, dividerHitRect);

            _shapeCanvas.Draw(leftAllowedRect);
            _profileCanvas.Draw(rightAllowedRect);

            if (Event.current.type == EventType.Repaint)
                Repaint();
        }

        private void DrawTopBar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(TopBarHeight));
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Shape World", GUILayout.Width(82f));
            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newShapeWidth = EditorGUILayout.FloatField(shapeDocument.WorldSizeMeters.x, GUILayout.Width(70f));
            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newShapeHeight = EditorGUILayout.FloatField(shapeDocument.WorldSizeMeters.y, GUILayout.Width(70f));

            GUILayout.Space(18f);

            EditorGUILayout.LabelField("Profile World", GUILayout.Width(84f));
            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newProfileWidth = EditorGUILayout.FloatField(profileDocument.WorldSizeMeters.x, GUILayout.Width(70f));
            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newProfileHeight = EditorGUILayout.FloatField(profileDocument.WorldSizeMeters.y, GUILayout.Width(70f));

            if (GUILayout.Button("Generate Profile", GUILayout.Width(120f)))
            {
                ShapeStamperProfileGenerator.Generate(
                    shapeDocument,
                    profileDocument,
                    BuildPreviewMaterialSettings());
            }

            if (GUILayout.Button("Bake", GUILayout.Width(100f)))
            {
                ShapeStamperBakeService.BakeShapeFaceToScene(shapeDocument);
            }

            GUILayout.FlexibleSpace();

            bool newHasInnerShape = EditorGUILayout.ToggleLeft("Inner Shape", shapeDocument.HasInnerShape, GUILayout.Width(100f));
            if (newHasInnerShape && !shapeDocument.HasInnerShape)
            {
                shapeDocument.HasInnerShape = true;
                shapeDocument.EnsureDefaultInnerShape();
                shapeDocument.EditMode = ShapeCanvasDocument.ShapeLoopEditMode.Inner;
                shapeSelection.Clear();
                shapeInteraction.Clear();
            }
            else if (!newHasInnerShape && shapeDocument.HasInnerShape)
            {
                shapeDocument.HasInnerShape = false;
                shapeDocument.EditMode = ShapeCanvasDocument.ShapeLoopEditMode.Outer;
                shapeSelection.Clear();
                shapeInteraction.Clear();
            }

            if (shapeDocument.HasInnerShape)
            {
                var newEditMode = (ShapeCanvasDocument.ShapeLoopEditMode)EditorGUILayout.EnumPopup(
                    shapeDocument.EditMode,
                    GUILayout.Width(90f));

                if (newEditMode != shapeDocument.EditMode)
                {
                    shapeDocument.EditMode = newEditMode;
                    shapeSelection.Clear();
                    shapeInteraction.Clear();
                }
            }

            if (GUILayout.Button("Reset Shape", GUILayout.Width(100f)))
            {
                shapeDocument.ResetToDefaultTriangle();
                shapeSelection.Clear();
                shapeInteraction.Clear();
                shapeView.ResetView();
                GUI.FocusControl(null);
            }

            if (GUILayout.Button("Reset Profile", GUILayout.Width(100f)))
            {
                profileDocument.ResetDefaultProfile();
                profileSelection.Clear();
                profileInteraction.Clear();
                profileView.ResetView();
                GUI.FocusControl(null);
            }

            EditorGUILayout.EndHorizontal();

            shapeDocument.WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newShapeWidth),
                Mathf.Max(0.0001f, newShapeHeight)
            );

            profileDocument.WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newProfileWidth),
                Mathf.Max(0.0001f, newProfileHeight)
            );

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Canvas %", GUILayout.Width(82f));

            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newPercentWidth = EditorGUILayout.FloatField(canvasPercentOfWindow.x * 100f, GUILayout.Width(70f));

            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newPercentHeight = EditorGUILayout.FloatField(canvasPercentOfWindow.y * 100f, GUILayout.Width(70f));

            canvasPercentOfWindow = new Vector2(
                Mathf.Clamp(newPercentWidth, 5f, 100f) / 100f,
                Mathf.Clamp(newPercentHeight, 5f, 100f) / 100f
            );

            GUILayout.Space(18f);
            EditorGUILayout.LabelField("Split", GUILayout.Width(30f));
            verticalDividerPercent = GUILayout.HorizontalSlider(verticalDividerPercent, 0.2f, 0.8f, GUILayout.Width(140f));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Frame Shape", GUILayout.Width(100f)))
                shapeView.ResetView();

            if (GUILayout.Button("Frame Profile", GUILayout.Width(100f)))
                profileView.ResetView();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Shape Points: {shapeDocument.PointCount}   Selected: {shapeSelection.Count}",
                EditorStyles.miniLabel
            );
            GUILayout.Space(20f);
            EditorGUILayout.LabelField(
                $"Profile Points: {profileDocument.Points.Count}   Selected: {profileSelection.Count}",
                EditorStyles.miniLabel
            );
            EditorGUILayout.EndHorizontal();

            DrawMaterialSettings();

            EditorGUILayout.EndVertical();
        }

        private void DrawMaterialSettings()
        {
            EditorGUILayout.Space(6f);
            showMaterialSettings = EditorGUILayout.Foldout(showMaterialSettings, "Materials", true);

            if (!showMaterialSettings)
                return;

            EditorGUI.indentLevel++;

            int requiredSegments = Mathf.Max(0, profileDocument.Points.Count - 1);
            EditorGUILayout.LabelField(
                $"Profile segments currently needed: {requiredSegments}",
                EditorStyles.miniLabel);

            int newSize = EditorGUILayout.IntField("Segment Slots", segmentMaterials.Count);
            newSize = Mathf.Max(0, newSize);

            while (segmentMaterials.Count < newSize)
                segmentMaterials.Add(null);

            while (segmentMaterials.Count > newSize)
                segmentMaterials.RemoveAt(segmentMaterials.Count - 1);

            for (int i = 0; i < segmentMaterials.Count; i++)
            {
                segmentMaterials[i] = (Material)EditorGUILayout.ObjectField(
                    $"Segment {i}",
                    segmentMaterials[i],
                    typeof(Material),
                    false);
            }

            startCapMaterial = (Material)EditorGUILayout.ObjectField(
                "Start Cap",
                startCapMaterial,
                typeof(Material),
                false);

            endCapMaterial = (Material)EditorGUILayout.ObjectField(
                "End Cap",
                endCapMaterial,
                typeof(Material),
                false);

            EditorGUI.indentLevel--;
        }

        private ShapeStampPreviewMaterialSettings BuildPreviewMaterialSettings()
        {
            ShapeStampPreviewMaterialSettings settings = new ShapeStampPreviewMaterialSettings();
            settings.StartCapMaterial = startCapMaterial;
            settings.EndCapMaterial = endCapMaterial;

            if (segmentMaterials != null)
            {
                for (int i = 0; i < segmentMaterials.Count; i++)
                    settings.SegmentMaterials.Add(segmentMaterials[i]);
            }

            return settings;
        }

        private void ComputeSplitRects(
            Rect fullCanvasArea,
            out Rect leftPanelRect,
            out Rect dividerRect,
            out Rect dividerHitRect,
            out Rect rightPanelRect)
        {
            float clampedPercent = Mathf.Clamp(verticalDividerPercent, 0.2f, 0.8f);

            float totalWidth = fullCanvasArea.width;
            float availableWidth = Mathf.Max(0f, totalWidth - DividerWidth);

            float leftWidth = Mathf.Round(availableWidth * clampedPercent);
            float rightWidth = availableWidth - leftWidth;

            if (leftWidth < MinPanelWidth)
            {
                leftWidth = MinPanelWidth;
                rightWidth = availableWidth - leftWidth;
            }

            if (rightWidth < MinPanelWidth)
            {
                rightWidth = MinPanelWidth;
                leftWidth = availableWidth - rightWidth;
            }

            float dividerX = fullCanvasArea.x + leftWidth;

            leftPanelRect = new Rect(fullCanvasArea.x, fullCanvasArea.y, Mathf.Max(0f, leftWidth), fullCanvasArea.height);
            dividerRect = new Rect(dividerX, fullCanvasArea.y, DividerWidth, fullCanvasArea.height);
            dividerHitRect = new Rect(dividerRect.center.x - DividerHitWidth * 0.5f, fullCanvasArea.y, DividerHitWidth, fullCanvasArea.height);
            rightPanelRect = new Rect(dividerRect.xMax, fullCanvasArea.y, Mathf.Max(0f, rightWidth), fullCanvasArea.height);
        }

        private void HandleDividerInput(Rect dividerHitRect)
        {
            Event evt = Event.current;
            EditorGUIUtility.AddCursorRect(dividerHitRect, MouseCursor.ResizeHorizontal);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (evt.button == 0 && dividerHitRect.Contains(evt.mousePosition))
                    {
                        _isDraggingDivider = true;
                        evt.Use();
                    }
                    break;

                case EventType.MouseDrag:
                    if (_isDraggingDivider && evt.button == 0)
                    {
                        float usableWidth = Mathf.Max(1f, position.width - DividerWidth);
                        verticalDividerPercent = Mathf.Clamp(evt.mousePosition.x / usableWidth, 0.2f, 0.8f);
                        evt.Use();
                    }
                    break;

                case EventType.MouseUp:
                    if (_isDraggingDivider && evt.button == 0)
                    {
                        _isDraggingDivider = false;
                        evt.Use();
                    }
                    break;
            }
        }

        private void DrawPanelBackground(Rect panelRect)
        {
            EditorGUI.DrawRect(panelRect, new Color(0.10f, 0.10f, 0.10f));
        }

        private void DrawDivider(Rect dividerRect, Rect dividerHitRect)
        {
            Color dividerColor = _isDraggingDivider
                ? new Color(0.55f, 0.55f, 0.55f)
                : (dividerHitRect.Contains(Event.current.mousePosition)
                    ? new Color(0.42f, 0.42f, 0.42f)
                    : new Color(0.28f, 0.28f, 0.28f));

            EditorGUI.DrawRect(dividerRect, dividerColor);
        }
    }
}
'''

generator_content = r'''
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
        public Material StartCapMaterial;
        public Material EndCapMaterial;
    }

    public static class ShapeStamperProfileGenerator
    {
        private const string PreviewRootName = "ShapeStamp_ProfileRings";
        private const string PreviewMeshObjectName = "ShapeStamp_ProfileSegments";
        private const float ParallelEpsilon = 0.000001f;
        private const float DefaultLineWidth = 0.02f;

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

                for (int i = settings.SegmentMaterials.Count - 1; i >= 0; i--)
                {
                    if (settings.SegmentMaterials[i] != null)
                        return settings.SegmentMaterials[i];
                }
            }

            return CreateSegmentMaterial(index, count);
        }

        private static Material ResolveStartCapMaterial(ShapeStampPreviewMaterialSettings settings)
        {
            if (settings != null && settings.StartCapMaterial != null)
                return settings.StartCapMaterial;

            return CreateCapMaterial(new Color(0.85f, 0.85f, 0.85f), "ShapeStamp_StartCap_Mat");
        }

        private static Material ResolveEndCapMaterial(ShapeStampPreviewMaterialSettings settings)
        {
            if (settings != null && settings.EndCapMaterial != null)
                return settings.EndCapMaterial;

            return CreateCapMaterial(new Color(0.65f, 0.65f, 0.65f), "ShapeStamp_EndCap_Mat");
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

        private static Material CreateCapMaterial(Color color, string name)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material mat = new Material(shader);
            mat.name = name;

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
'''

WINDOW_PATH.write_text(textwrap.dedent(window_content).lstrip("\n"), encoding="utf-8")
GENERATOR_PATH.write_text(textwrap.dedent(generator_content).lstrip("\n"), encoding="utf-8")

print(f"Patched {WINDOW_PATH.relative_to(ROOT)}")
print(f"Patched {GENERATOR_PATH.relative_to(ROOT)}")
print("\nBackups were written as *.materials.bak")