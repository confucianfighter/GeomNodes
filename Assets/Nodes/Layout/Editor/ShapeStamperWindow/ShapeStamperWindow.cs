using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public class ShapeStamperWindow : EditorWindow
    {
        private const float TopBarHeight = 116f;
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
            window.minSize = new Vector2(600f, 360f);
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
            if (GUILayout.Button("Bake", GUILayout.Width(100f)))
            {
                ShapeStamperBakeService.BakeShapeFaceToScene(shapeDocument);
            }
            GUILayout.FlexibleSpace();
            bool newHasInnerShape = EditorGUILayout.ToggleLeft("Inner Shape", shapeDocument.HasInnerShape, GUILayout.Width(100f));
            if (newHasInnerShape != shapeDocument.HasInnerShape)
            {
                shapeDocument.HasInnerShape = newHasInnerShape;

                if (newHasInnerShape)
                    shapeDocument.EnsureDefaultInnerShape();
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
            {
                shapeView.ResetView();
            }

            if (GUILayout.Button("Frame Profile", GUILayout.Width(100f)))
            {
                profileView.ResetView();
            }

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

            EditorGUILayout.EndVertical();
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