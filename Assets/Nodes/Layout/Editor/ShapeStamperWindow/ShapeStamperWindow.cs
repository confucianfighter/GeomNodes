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

            Vector2 requestedShapeSize = new Vector2(
                Mathf.Max(0.0001f, newShapeWidth),
                Mathf.Max(0.0001f, newShapeHeight)
            );

            if (requestedShapeSize != shapeDocument.WorldSizeMeters)
                shapeDocument.ResizeWorld(requestedShapeSize);

            profileDocument.WorldSizeMeters = new Vector2(
                Mathf.Max(0.0001f, newProfileWidth),
                Mathf.Max(0.0001f, newProfileHeight)
            );

            DrawSelectedShapePointInspector();

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

        private void DrawSelectedShapePointInspector()
        {
            if (shapeSelection == null || shapeSelection.Count != 1)
                return;

            CanvasElementRef selected = default;
            foreach (CanvasElementRef element in shapeSelection.Elements)
            {
                selected = element;
                break;
            }

            if (!selected.IsPoint)
                return;

            IList<CanvasPoint> points = shapeDocument.Points;
            int index = -1;
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Id == selected.Id)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
                return;

            CanvasPoint point = points[index];
            Rect bounds = shapeDocument.GetCanvasFrameRect();

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Point {point.Id}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            CanvasAnchorX newXAnchor = (CanvasAnchorX)EditorGUILayout.EnumPopup("X Anchor", point.XAnchor);
            CanvasAnchorY newYAnchor = (CanvasAnchorY)EditorGUILayout.EnumPopup("Y Anchor", point.YAnchor);

            EditorGUILayout.LabelField("Position", $"{point.Position.x:0.###}, {point.Position.y:0.###}");
            EditorGUILayout.LabelField("Offset", $"{point.OffsetX:0.###}, {point.OffsetY:0.###}");

            if (EditorGUI.EndChangeCheck())
            {
                ShapeCanvasPointResolver.SetAnchorsPreservePosition(ref point, newXAnchor, newYAnchor, bounds);
                points[index] = point;
                shapeDocument.MarkDirty();
                Repaint();
            }

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
