using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DLN;

namespace DLN.EditorTools.ShapeStamper
{
    public class ShapeStamperWindow : EditorWindow
    {
        private const float TopBarHeight = 470f;
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
        [SerializeField] private bool autoRegeneratePreview = true;
        [SerializeField] private AdaptiveShape adaptiveShape;
        [SerializeField] private List<Material> segmentMaterials = new();
        [SerializeField] private List<Color> segmentColors = new();
        [SerializeField] private Material startCapMaterial;
        [SerializeField] private Material endCapMaterial;
        [SerializeField] private Color startCapColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        [SerializeField] private Color endCapColor = new Color(0.65f, 0.65f, 0.65f, 1f);

        private EditorCanvas _shapeCanvas;
        private EditorCanvas _profileCanvas;

        private ICanvasToolPolicy _shapePolicy;
        private ICanvasToolPolicy _profilePolicy;

        private bool _isDraggingDivider;
        private SmartBounds _activeSmartBounds;
        private int _lastShapeRevision = -1;
        private int _lastProfileRevision = -1;
        private int _lastMaterialHash;
        private bool _forcePreviewRefresh = true;

        [MenuItem("Tools/DLN/Shape Stamper")]
        public static void Open()
        {
            ShapeStamperWindow window = GetWindow<ShapeStamperWindow>();
            window.titleContent = new GUIContent("Shape Stamper");
            window.minSize = new Vector2(700f, 540f);
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
            segmentColors ??= new List<Color>();

            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            BindToAdaptiveShape(adaptiveShape);

            _forcePreviewRefresh = true;
        }

        private void RecreateCanvasBindings()
        {
            _shapePolicy = new ShapeCanvasPolicy(shapeDocument);
            _profilePolicy = new ProfileCanvasPolicy(profileDocument);

            _shapeCanvas = new EditorCanvas(shapeDocument, _shapePolicy, shapeSelection, shapeInteraction, shapeView);
            _profileCanvas = new EditorCanvas(profileDocument, _profilePolicy, profileSelection, profileInteraction, profileView);

            _forcePreviewRefresh = true;
        }

        private void OnGUI()
        {
            shapeDocument ??= new ShapeCanvasDocument();
            profileDocument ??= new ProfileCanvasDocument();
            segmentMaterials ??= new List<Material>();
            segmentColors ??= new List<Color>();

            shapeDocument.EnsureValidShape();
            profileDocument.EnsureValidProfile();

            SyncDocumentsFromAdaptiveShape();

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

            MaybeAutoRegeneratePreview();

            if (Event.current.type == EventType.Repaint)
                Repaint();
        }

        private void DrawTopBar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(TopBarHeight));
            EditorGUILayout.Space(6f);

            DrawAdaptiveShapeBindingRow();

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
                RegeneratePreview();
            }

            if (GUILayout.Button("Bake", GUILayout.Width(100f)))
            {
                ShapeStamperBakeService.BakeShapeFaceToScene(shapeDocument);
            }

            GUILayout.FlexibleSpace();

            autoRegeneratePreview = EditorGUILayout.ToggleLeft("Auto Preview", autoRegeneratePreview, GUILayout.Width(100f));

            bool newHasInnerShape = EditorGUILayout.ToggleLeft("Inner Shape", shapeDocument.HasInnerShape, GUILayout.Width(100f));
            if (newHasInnerShape && !shapeDocument.HasInnerShape)
            {
                shapeDocument.HasInnerShape = true;
                shapeDocument.EnsureDefaultInnerShape();
                shapeDocument.EditMode = ShapeCanvasDocument.ShapeLoopEditMode.Inner;
                shapeDocument.MarkDirty();
                shapeSelection.Clear();
                shapeInteraction.Clear();
                _forcePreviewRefresh = true;
            }
            else if (!newHasInnerShape && shapeDocument.HasInnerShape)
            {
                shapeDocument.HasInnerShape = false;
                shapeDocument.EditMode = ShapeCanvasDocument.ShapeLoopEditMode.Outer;
                shapeDocument.MarkDirty();
                shapeSelection.Clear();
                shapeInteraction.Clear();
                _forcePreviewRefresh = true;
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
                _forcePreviewRefresh = true;
            }

            if (GUILayout.Button("Reset Profile", GUILayout.Width(100f)))
            {
                profileDocument.ResetDefaultProfile();
                profileSelection.Clear();
                profileInteraction.Clear();
                profileView.ResetView();
                GUI.FocusControl(null);
                _forcePreviewRefresh = true;
            }

            EditorGUILayout.EndHorizontal();

            Vector2 requestedShapeSize = new Vector2(
                Mathf.Max(0.0001f, newShapeWidth),
                Mathf.Max(0.0001f, newShapeHeight)
            );

            if (requestedShapeSize != shapeDocument.WorldSizeMeters)
            {
                shapeDocument.ResizeWorld(requestedShapeSize);
                _forcePreviewRefresh = true;
            }

            Vector2 requestedProfileSize = new Vector2(
                Mathf.Max(0.0001f, newProfileWidth),
                Mathf.Max(0.0001f, newProfileHeight)
            );

            if (requestedProfileSize != profileDocument.WorldSizeMeters)
            {
                profileDocument.ResizeWorld(requestedProfileSize);
                _forcePreviewRefresh = true;
            }

            DrawAdaptiveShapeSummary();
            DrawSelectedShapeElementInspector();
            DrawSelectedProfilePointInspector();

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

        private void DrawAdaptiveShapeBindingRow()
        {
            EditorGUILayout.BeginHorizontal();

            AdaptiveShape selectedAdaptiveShape = TryGetSelectedAdaptiveShape();

            EditorGUILayout.LabelField(
                adaptiveShape != null
                    ? $"Adaptive Shape: {adaptiveShape.name}"
                    : "Adaptive Shape: none",
                EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Use Selected", GUILayout.Width(100f)))
            {
                if (selectedAdaptiveShape != null)
                    BindToAdaptiveShape(selectedAdaptiveShape);
            }

            if (GUILayout.Button("Create Adaptive Shape", GUILayout.Width(160f)))
            {
                CreateAdaptiveShapeGameObject();
            }

            EditorGUI.BeginDisabledGroup(adaptiveShape == null);
            if (GUILayout.Button("Ping", GUILayout.Width(60f)))
            {
                EditorGUIUtility.PingObject(adaptiveShape);
                Selection.activeObject = adaptiveShape != null ? adaptiveShape.gameObject : null;
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAdaptiveShapeSummary()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Adaptive Shape", EditorStyles.boldLabel);

            if (adaptiveShape == null)
            {
                EditorGUILayout.HelpBox(
                    "Use Selected to bind the window to the currently selected AdaptiveShape, or Create Adaptive Shape to make a new one.",
                    MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            SmartBounds smartBounds = adaptiveShape.SmartBounds;
            BordersPadding bp = adaptiveShape.GetEffectiveBordersPadding();

            EditorGUILayout.LabelField(
                smartBounds != null
                    ? $"SmartBounds Source: {smartBounds.name}"
                    : "SmartBounds Source: none",
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                $"Using SmartBounds Borders/Padding: {adaptiveShape.PreferSmartBoundsBordersPadding}",
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                $"X  -B {bp.x.negativeBorder:0.###}   -P {bp.x.negativePadding:0.###}   +P {bp.x.positivePadding:0.###}   +B {bp.x.positiveBorder:0.###}",
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                $"Y  -B {bp.y.negativeBorder:0.###}   -P {bp.y.negativePadding:0.###}   +P {bp.y.positivePadding:0.###}   +B {bp.y.positiveBorder:0.###}",
                EditorStyles.miniLabel);

            EditorGUILayout.LabelField(
                $"Z  -B {bp.z.negativeBorder:0.###}   -P {bp.z.negativePadding:0.###}   +P {bp.z.positivePadding:0.###}   +B {bp.z.positiveBorder:0.###}",
                EditorStyles.miniLabel);

            EditorGUILayout.HelpBox(
                "Edit borders/padding on SmartBounds for now. The Shape Stamper window reads them and updates its guides/documents.",
                MessageType.None);

            if (adaptiveShape.MainShapeRoot != null)
                EditorGUILayout.LabelField($"MainShape Root: {adaptiveShape.MainShapeRoot.name}", EditorStyles.miniLabel);
            if (adaptiveShape.RingSegmentsRoot != null)
                EditorGUILayout.LabelField($"RingSegments Root: {adaptiveShape.RingSegmentsRoot.name}", EditorStyles.miniLabel);
            if (adaptiveShape.DebugRoot != null)
                EditorGUILayout.LabelField($"Debug Root: {adaptiveShape.DebugRoot.name}", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        private AdaptiveShape TryGetSelectedAdaptiveShape()
        {
            GameObject selectedGo = Selection.activeGameObject;
            if (selectedGo == null)
                return null;

            selectedGo.TryGetComponent(out AdaptiveShape selectedAdaptiveShape);
            return selectedAdaptiveShape;
        }

        private void CreateAdaptiveShapeGameObject()
        {
            GameObject go = new GameObject("Adaptive Shape");
            Undo.RegisterCreatedObjectUndo(go, "Create Adaptive Shape");

            SmartBounds smartBounds = Undo.AddComponent<SmartBounds>(go);
            AdaptiveShape newAdaptiveShape = Undo.AddComponent<AdaptiveShape>(go);

            newAdaptiveShape.EnsureReferences();
            newAdaptiveShape.PullFromSmartBounds();
#if UNITY_EDITOR
            newAdaptiveShape.EnsureEditorState();
#endif

            Selection.activeGameObject = go;
            BindToAdaptiveShape(newAdaptiveShape);

            EditorGUIUtility.PingObject(go);
        }

        private void BindToAdaptiveShape(AdaptiveShape newAdaptiveShape)
        {
            adaptiveShape = newAdaptiveShape;
            _activeSmartBounds = null;

            if (adaptiveShape == null)
            {
                shapeDocument ??= new ShapeCanvasDocument();
                profileDocument ??= new ProfileCanvasDocument();

                shapeDocument.EnsureValidShape();
                profileDocument.EnsureValidProfile();

                RecreateCanvasBindings();
                return;
            }

            adaptiveShape.EnsureReferences();
#if UNITY_EDITOR
            adaptiveShape.EnsureEditorState();
            shapeDocument = adaptiveShape.ShapeDocument;
            profileDocument = adaptiveShape.ProfileDocument;
#endif
            _activeSmartBounds = adaptiveShape.SmartBounds;

            SyncDocumentsFromAdaptiveShape();
            RecreateCanvasBindings();

            shapeSelection.Clear();
            shapeInteraction.Clear();
            profileSelection.Clear();
            profileInteraction.Clear();

            shapeView.ResetView();
            profileView.ResetView();

            _forcePreviewRefresh = true;
        }

        private void SyncDocumentsFromAdaptiveShape()
        {
            if (adaptiveShape == null)
                return;

            BordersPadding bp = adaptiveShape.GetEffectiveBordersPadding();
            bp.ClampToValid();

            bool shapeChanged =
                !Mathf.Approximately(shapeDocument.LeftBorder, bp.x.negativeBorder) ||
                !Mathf.Approximately(shapeDocument.LeftPadding, bp.x.negativePadding) ||
                !Mathf.Approximately(shapeDocument.RightPadding, bp.x.positivePadding) ||
                !Mathf.Approximately(shapeDocument.RightBorder, bp.x.positiveBorder) ||
                !Mathf.Approximately(shapeDocument.TopBorder, bp.y.negativeBorder) ||
                !Mathf.Approximately(shapeDocument.TopPadding, bp.y.negativePadding) ||
                !Mathf.Approximately(shapeDocument.BottomPadding, bp.y.positivePadding) ||
                !Mathf.Approximately(shapeDocument.BottomBorder, bp.y.positiveBorder);

            if (shapeChanged)
            {
                shapeDocument.LeftBorder = bp.x.negativeBorder;
                shapeDocument.LeftPadding = bp.x.negativePadding;
                shapeDocument.RightPadding = bp.x.positivePadding;
                shapeDocument.RightBorder = bp.x.positiveBorder;

                shapeDocument.TopBorder = bp.y.negativeBorder;
                shapeDocument.TopPadding = bp.y.negativePadding;
                shapeDocument.BottomPadding = bp.y.positivePadding;
                shapeDocument.BottomBorder = bp.y.positiveBorder;

                shapeDocument.MarkDirty();
                _forcePreviewRefresh = true;
            }

            bool profileChanged =
                !Mathf.Approximately(profileDocument.LeftPadding, bp.x.negativePadding) ||
                !Mathf.Approximately(profileDocument.RightPadding, bp.x.positivePadding) ||
                !Mathf.Approximately(profileDocument.TopPadding, bp.y.negativePadding) ||
                !Mathf.Approximately(profileDocument.BottomPadding, bp.y.positivePadding) ||
                !Mathf.Approximately(profileDocument.LeftBorder, bp.x.negativeBorder) ||
                !Mathf.Approximately(profileDocument.RightBorder, bp.x.positiveBorder) ||
                !Mathf.Approximately(profileDocument.TopBorder, bp.y.negativeBorder) ||
                !Mathf.Approximately(profileDocument.BottomBorder, bp.y.positiveBorder) ||
                !Mathf.Approximately(profileDocument.FrontPaddingDepth, Mathf.Max(bp.z.negativePadding, bp.z.positivePadding)) ||
                !Mathf.Approximately(profileDocument.FrontBorderDepth, Mathf.Max(bp.z.negativeBorder, bp.z.positiveBorder));

            if (profileChanged)
            {
                profileDocument.SetGuideValues(
                    bp.x.negativePadding,
                    bp.x.positivePadding,
                    bp.y.negativePadding,
                    bp.y.positivePadding,
                    bp.x.negativeBorder,
                    bp.x.positiveBorder,
                    bp.y.negativeBorder,
                    bp.y.positiveBorder);

                profileDocument.FrontPaddingDepth = Mathf.Max(bp.z.negativePadding, bp.z.positivePadding);
                profileDocument.FrontBorderDepth = Mathf.Max(bp.z.negativeBorder, bp.z.positiveBorder);
                profileDocument.MarkDirty();
                _forcePreviewRefresh = true;
            }
        }

        private void DrawSelectedShapeElementInspector()
        {
            if (shapeSelection == null || shapeSelection.Count != 1)
                return;

            CanvasElementRef selected = GetSingleSelection(shapeSelection);
            if (!selected.IsValid)
                return;

            if (selected.IsPoint)
                DrawSelectedShapePointInspector(selected.Id);
            else if (selected.IsEdge)
                DrawSelectedShapeEdgeInspector(selected.Id);
        }

        private void DrawSelectedShapePointInspector(int pointId)
        {
            IList<CanvasPoint> points = shapeDocument.Points;
            int index = FindPointIndex(points, pointId);
            if (index < 0)
                return;

            ProfilePoint point = points[index];
            Rect bounds = shapeDocument.GetCanvasFrameRect();

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Shape Point {point.Id}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();

            CanvasAnchorX newXAnchor = (CanvasAnchorX)EditorGUILayout.EnumPopup("X Anchor", point.XAnchor);
            CanvasAnchorY newYAnchor = (CanvasAnchorY)EditorGUILayout.EnumPopup("Y Anchor", point.YAnchor);

            Vector2 newPosition = EditorGUILayout.Vector2Field("Position", point.Position);

            bool canEditOffsetX = point.XAnchor != CanvasAnchorX.Floating;
            bool canEditOffsetY = point.YAnchor != CanvasAnchorY.Floating;

            EditorGUI.BeginDisabledGroup(!canEditOffsetX);
            float newOffsetX = EditorGUILayout.FloatField("Offset X", point.OffsetX);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(!canEditOffsetY);
            float newOffsetY = EditorGUILayout.FloatField("Offset Y", point.OffsetY);
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                if (newXAnchor != point.XAnchor || newYAnchor != point.YAnchor)
                    ShapeCanvasPointResolver.SetAnchorsPreservePosition(ref point, newXAnchor, newYAnchor, bounds);

                point.Position = new Vector2(
                    Mathf.Clamp(newPosition.x, 0f, shapeDocument.WorldSizeMeters.x),
                    Mathf.Clamp(newPosition.y, 0f, shapeDocument.WorldSizeMeters.y));

                if (point.XAnchor != CanvasAnchorX.Floating)
                    point.OffsetX = newOffsetX;
                if (point.YAnchor != CanvasAnchorY.Floating)
                    point.OffsetY = newOffsetY;

                point.Position = ShapeCanvasPointResolver.ResolvePoint(point, bounds, bounds);
                points[index] = point;
                shapeDocument.MarkDirty();
                _forcePreviewRefresh = true;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedShapeEdgeInspector(int edgeId)
        {
            IList<CanvasEdge> edges = shapeDocument.Edges;
            int index = FindEdgeIndex(edges, edgeId);
            if (index < 0)
                return;

            CanvasEdge edge = edges[index];

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Shape Edge {edge.Id}", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            float newScale = EditorGUILayout.FloatField("Profile X Scale", edge.ProfileXScale);

            if (EditorGUI.EndChangeCheck())
            {
                edge.ProfileXScale = Mathf.Max(0f, newScale);
                edges[index] = edge;
                shapeDocument.MarkDirty();
                _forcePreviewRefresh = true;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedProfilePointInspector()
        {
            if (profileSelection == null || profileSelection.Count != 1)
                return;

            CanvasElementRef selected = GetSingleSelection(profileSelection);
            if (!selected.IsPoint)
                return;

            IList<ProfilePoint> points = profileDocument.ProfilePoints;
            int index = FindProfilePointIndex(points, selected.Id);
            if (index < 0)
                return;

            ProfilePoint point = points[index];
            Rect bounds = profileDocument.GetCanvasFrameRect();
            float paddingGuideX = profileDocument.PaddingGuideX;
            float borderGuideX = profileDocument.BorderGuideX;

            EditorGUILayout.Space(6f);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Profile Point {point.Id}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"X:{point.XSpan}   Z:{point.ZSpan}   XT:{point.XT:0.###}   ZT:{point.ZT:0.###}",
                EditorStyles.miniLabel);

            EditorGUI.BeginChangeCheck();

            ProfileXSpan newXSpan = (ProfileXSpan)EditorGUILayout.EnumPopup("Profile X Span", point.XSpan);
            ProfileZSpan newZSpan = (ProfileZSpan)EditorGUILayout.EnumPopup("Profile Z Span", point.ZSpan);
            CanvasAnchorY newYAnchor = (CanvasAnchorY)EditorGUILayout.EnumPopup("Profile Y Anchor", point.YAnchor);
            Vector2 newPosition = EditorGUILayout.Vector2Field("Position", point.Position);
            float newXT = EditorGUILayout.Slider("Profile X T", point.XT, 0f, 1f);
            float newZT = EditorGUILayout.Slider("Profile Z T", point.ZT, 0f, 1f);

            bool canEditOffsetY = point.YAnchor != CanvasAnchorY.Floating;
            EditorGUI.BeginDisabledGroup(!canEditOffsetY);
            float newOffsetY = EditorGUILayout.FloatField("Offset Y", point.OffsetY);
            EditorGUI.EndDisabledGroup();

            if (EditorGUI.EndChangeCheck())
            {
                point.XSpan = newXSpan;
                point.ZSpan = newZSpan;
                point.YAnchor = newYAnchor;
                point.Position = new Vector2(
                    Mathf.Clamp(newPosition.x, 0f, profileDocument.WorldSizeMeters.x),
                    Mathf.Clamp(newPosition.y, 0f, profileDocument.WorldSizeMeters.y));
                point.XT = Mathf.Clamp01(newXT);
                point.ZT = Mathf.Clamp01(newZT);

                if (point.YAnchor != CanvasAnchorY.Floating)
                    point.OffsetY = newOffsetY;

                ProfileCanvasPointResolver.SetSpansFromPosition(
                    ref point,
                    bounds,
                    paddingGuideX,
                    borderGuideX);

                profileDocument.ProfilePoints[index] = point;
                profileDocument.SyncDisplayPointsFromProfilePoints();
                profileDocument.MarkDirty();
                _forcePreviewRefresh = true;
                Repaint();
            }

            EditorGUILayout.EndVertical();
        }

        private static int FindProfilePointIndex(IList<ProfilePoint> points, int pointId)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Id == pointId)
                    return i;
            }

            return -1;
        }

        private static CanvasElementRef GetSingleSelection(CanvasSelection selection)
        {
            foreach (CanvasElementRef element in selection.Elements)
                return element;

            return default;
        }

        private static int FindPointIndex(IList<CanvasPoint> points, int pointId)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].Id == pointId)
                    return i;
            }

            return -1;
        }

        private static int FindEdgeIndex(IList<CanvasEdge> edges, int edgeId)
        {
            for (int i = 0; i < edges.Count; i++)
            {
                if (edges[i].Id == edgeId)
                    return i;
            }

            return -1;
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

            while (segmentColors.Count < newSize)
            {
                float gray = Mathf.Lerp(0.3f, 0.8f, newSize <= 1 ? 0f : (segmentColors.Count / (float)(newSize - 1)));
                segmentColors.Add(new Color(gray, gray, gray, 1f));
            }

            while (segmentMaterials.Count > newSize)
                segmentMaterials.RemoveAt(segmentMaterials.Count - 1);

            while (segmentColors.Count > newSize)
                segmentColors.RemoveAt(segmentColors.Count - 1);

            for (int i = 0; i < segmentMaterials.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                segmentMaterials[i] = (Material)EditorGUILayout.ObjectField(
                    $"Segment {i} Material",
                    segmentMaterials[i],
                    typeof(Material),
                    false);

                segmentColors[i] = EditorGUILayout.ColorField(
                    $"Color",
                    segmentColors[i],
                    GUILayout.Width(180f));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            startCapMaterial = (Material)EditorGUILayout.ObjectField(
                "Start Cap Material",
                startCapMaterial,
                typeof(Material),
                false);
            startCapColor = EditorGUILayout.ColorField("Color", startCapColor, GUILayout.Width(180f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            endCapMaterial = (Material)EditorGUILayout.ObjectField(
                "End Cap Material",
                endCapMaterial,
                typeof(Material),
                false);
            endCapColor = EditorGUILayout.ColorField("Color", endCapColor, GUILayout.Width(180f));
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        private void MaybeAutoRegeneratePreview()
        {
            if (!autoRegeneratePreview)
                return;

            int materialHash = ComputeMaterialHash();
            bool changed =
                _forcePreviewRefresh ||
                _lastShapeRevision != shapeDocument.Revision ||
                _lastProfileRevision != profileDocument.Revision ||
                _lastMaterialHash != materialHash;

            if (!changed)
                return;

            RegeneratePreview();
        }

        private void RegeneratePreview()
        {
            ShapeStampPreviewMaterialSettings materialSettings = BuildPreviewMaterialSettings();

            if (adaptiveShape != null)
            {
                AdaptiveShapeBuilder.Rebuild(adaptiveShape, materialSettings);
            }
            else
            {
                ShapeStamperProfileGenerator.Generate(
                    shapeDocument,
                    profileDocument,
                    materialSettings);
            }

            _lastShapeRevision = shapeDocument.Revision;
            _lastProfileRevision = profileDocument.Revision;
            _lastMaterialHash = ComputeMaterialHash();
            _forcePreviewRefresh = false;
        }

        private int ComputeMaterialHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (startCapMaterial != null ? startCapMaterial.GetInstanceID() : 0);
                hash = hash * 31 + (endCapMaterial != null ? endCapMaterial.GetInstanceID() : 0);

                if (segmentMaterials != null)
                {
                    for (int i = 0; i < segmentMaterials.Count; i++)
                        hash = hash * 31 + (segmentMaterials[i] != null ? segmentMaterials[i].GetInstanceID() : 0);
                }

                if (segmentColors != null)
                {
                    for (int i = 0; i < segmentColors.Count; i++)
                        hash = hash * 31 + segmentColors[i].GetHashCode();
                }

                hash = hash * 31 + startCapColor.GetHashCode();
                hash = hash * 31 + endCapColor.GetHashCode();

                return hash;
            }
        }

        private ShapeStampPreviewMaterialSettings BuildPreviewMaterialSettings()
        {
            ShapeStampPreviewMaterialSettings settings = new ShapeStampPreviewMaterialSettings();
            settings.StartCapMaterial = startCapMaterial;
            settings.EndCapMaterial = endCapMaterial;
            settings.StartCapColor = startCapColor;
            settings.EndCapColor = endCapColor;

            if (segmentMaterials != null)
            {
                for (int i = 0; i < segmentMaterials.Count; i++)
                    settings.SegmentMaterials.Add(segmentMaterials[i]);
            }

            if (segmentColors != null)
            {
                for (int i = 0; i < segmentColors.Count; i++)
                    settings.SegmentColors.Add(segmentColors[i]);
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
