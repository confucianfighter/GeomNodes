using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public class ShapeStamperWindow : EditorWindow
    {
        private const float TopBarHeight = 92f;
        private const float CanvasPadding = 12f;
        private const float PointHandleRadius = 7f;
        private const float EdgeInsertThreshold = 10f;
        private const float DragStartThresholdPixels = 5f;

        private const float DividerWidth = 6f;
        private const float DividerHitWidth = 12f;
        private const float MinPanelWidth = 120f;

        [SerializeField] private ShapeCanvasDocument document = new();
        [SerializeField] private Vector2 canvasPercentOfWindow = new Vector2(0.8f, 0.8f);
        [SerializeField] private float verticalDividerPercent = 0.65f;

        private ShapeEditorCanvas _shapeCanvas;
        private ShapeEditorCanvas _profileCanvas;

        private bool _isDraggingDivider;

        [MenuItem("Tools/DLN/Shape Stamper")]
        public static void Open()
        {
            var window = GetWindow<ShapeStamperWindow>();
            window.titleContent = new GUIContent("Shape Stamper");
            window.minSize = new Vector2(500f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            _shapeCanvas ??= new ShapeEditorCanvas();
            _profileCanvas ??= new ShapeEditorCanvas();
            document ??= new ShapeCanvasDocument();

            EnsureValidShape();
            SyncCanvasSelectionFromLegacyData();
        }

        private void OnGUI()
        {
            _shapeCanvas ??= new ShapeEditorCanvas();
            _profileCanvas ??= new ShapeEditorCanvas();
            document ??= new ShapeCanvasDocument();

            EnsureValidShape();
            SyncCanvasSelectionFromLegacyData();

            DrawTopBar();

            Rect fullCanvasArea = new Rect(
                0f,
                TopBarHeight,
                position.width,
                Mathf.Max(0f, position.height - TopBarHeight)
            );

            Rect leftPanelRect;
            Rect dividerRect;
            Rect dividerHitRect;
            Rect rightPanelRect;
            ComputeSplitRects(fullCanvasArea, out leftPanelRect, out dividerRect, out dividerHitRect, out rightPanelRect);

            HandleDividerInput(dividerHitRect);

            Rect leftAllowedRect = ShapeCanvasUtility.GetAllowedCanvasRect(
                leftPanelRect,
                canvasPercentOfWindow,
                CanvasPadding
            );

            Rect leftDrawRect = ShapeCanvasUtility.GetFittedWorldRect(
                leftAllowedRect,
                document.WorldSizeMeters
            );

            Rect rightAllowedRect = ShapeCanvasUtility.GetAllowedCanvasRect(
                rightPanelRect,
                canvasPercentOfWindow,
                CanvasPadding
            );

            // Placeholder profile world for now: square-ish canvas.
            Vector2 profileWorldSize = new Vector2(1f, 1f);

            Rect rightDrawRect = ShapeCanvasUtility.GetFittedWorldRect(
                rightAllowedRect,
                profileWorldSize
            );

            _shapeCanvas.SetScreenRect(leftDrawRect);
            _shapeCanvas.PointHitRadiusPixels = PointHandleRadius;
            _shapeCanvas.SegmentHitDistancePixels = EdgeInsertThreshold;
            _shapeCanvas.DragThresholdPixels = DragStartThresholdPixels;

            _profileCanvas.SetScreenRect(rightDrawRect);
            _profileCanvas.PointHitRadiusPixels = PointHandleRadius;
            _profileCanvas.SegmentHitDistancePixels = EdgeInsertThreshold;
            _profileCanvas.DragThresholdPixels = DragStartThresholdPixels;

            HandleInput(leftDrawRect);

            if (Event.current.type == EventType.Repaint)
            {
                UpdateHoverState(leftDrawRect, Event.current.mousePosition);
            }

            DrawPanelBackground(leftPanelRect);
            DrawPanelBackground(rightPanelRect);
            DrawDivider(dividerRect, dividerHitRect);

            ShapeCanvasDrawing.DrawCanvasBackground(leftAllowedRect, leftDrawRect);
            ShapeCanvasDrawing.DrawPolygon(
                document.Points,
                leftDrawRect,
                document.WorldSizeMeters,
                _shapeCanvas.Interaction.HoveredPointId,
                _shapeCanvas.Interaction.HoveredSegmentId,
                GetSelectedPointIndices(),
                GetSelectedEdgeIndices(),
                PointHandleRadius
            );

            DrawProfilePlaceholder(rightAllowedRect, rightDrawRect);

            Repaint();
        }

        private void DrawTopBar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(TopBarHeight));
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("World Size (m)", GUILayout.Width(92f));

            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newWorldWidth = EditorGUILayout.FloatField(document.WorldSizeMeters.x, GUILayout.Width(70f));

            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newWorldHeight = EditorGUILayout.FloatField(document.WorldSizeMeters.y, GUILayout.Width(70f));

            newWorldWidth = Mathf.Max(0.0001f, newWorldWidth);
            newWorldHeight = Mathf.Max(0.0001f, newWorldHeight);

            Vector2 newWorldSize = new Vector2(newWorldWidth, newWorldHeight);
            if (!Approximately(newWorldSize, document.WorldSizeMeters))
            {
                document.WorldSizeMeters = newWorldSize;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset Triangle", GUILayout.Width(120f)))
            {
                ResetToDefaultTriangle();
                GUI.FocusControl(null);
            }

            EditorGUI.BeginDisabledGroup(_shapeCanvas.Selection.SelectedPointIds.Count == 0 || document.PointCount <= 3);
            if (GUILayout.Button("Delete Selected", GUILayout.Width(120f)))
            {
                DeleteSelectedPoints();
                GUI.FocusControl(null);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Canvas %", GUILayout.Width(92f));

            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newPercentWidth = EditorGUILayout.FloatField(canvasPercentOfWindow.x * 100f, GUILayout.Width(70f));

            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newPercentHeight = EditorGUILayout.FloatField(canvasPercentOfWindow.y * 100f, GUILayout.Width(70f));

            newPercentWidth = Mathf.Clamp(newPercentWidth, 5f, 100f);
            newPercentHeight = Mathf.Clamp(newPercentHeight, 5f, 100f);

            canvasPercentOfWindow = new Vector2(newPercentWidth / 100f, newPercentHeight / 100f);

            GUILayout.Space(16f);
            EditorGUILayout.LabelField("Split", GUILayout.Width(30f));
            verticalDividerPercent = GUILayout.HorizontalSlider(verticalDividerPercent, 0.2f, 0.8f, GUILayout.Width(140f));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Points: {document.PointCount}   Selected Points: {_shapeCanvas.Selection.SelectedPointIds.Count}   Selected Edges: {_shapeCanvas.Selection.SelectedSegmentIds.Count}",
                EditorStyles.miniLabel
            );
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void HandleInput(Rect drawRect)
        {
            Event evt = Event.current;
            Vector2 mouseGui = evt.mousePosition;
            bool mouseInsideCanvas = drawRect.Contains(mouseGui);

            switch (evt.type)
            {
                case EventType.MouseDown:
                    {
                        if (!mouseInsideCanvas)
                            break;

                        bool additive = _shapeCanvas.IsAdditiveSelection(evt);

                        int pointIndex = ShapeCanvasUtility.FindPointNearMouse(
                            document.Points,
                            drawRect,
                            document.WorldSizeMeters,
                            mouseGui,
                            PointHandleRadius
                        );

                        if (evt.button == 0)
                        {
                            _shapeCanvas.BeginPrimaryMouseDown(evt);

                            if (pointIndex >= 0)
                            {
                                BeginPendingPointPress(pointIndex, mouseGui);
                                evt.Use();
                            }
                            else
                            {
                                int edgeIndex = ShapeCanvasUtility.FindEdgeNearMouse(
                                    document.Points,
                                    drawRect,
                                    document.WorldSizeMeters,
                                    mouseGui,
                                    EdgeInsertThreshold
                                );

                                CancelPendingPointPress();

                                if (edgeIndex >= 0)
                                {
                                    if (!additive)
                                    {
                                        _shapeCanvas.Selection.Clear();
                                        _shapeCanvas.Selection.AddSegment(edgeIndex);
                                    }
                                    else
                                    {
                                        _shapeCanvas.Selection.ToggleSegment(edgeIndex);
                                        _shapeCanvas.Selection.SelectedPointIds.Clear();
                                    }

                                    evt.Use();
                                }
                                else
                                {
                                    if (!additive)
                                        _shapeCanvas.Selection.Clear();

                                    evt.Use();
                                }
                            }
                        }
                        else if (evt.button == 1)
                        {
                            CancelPendingPointPress();

                            int edgeIndex = ShapeCanvasUtility.FindEdgeNearMouse(
                                document.Points,
                                drawRect,
                                document.WorldSizeMeters,
                                mouseGui,
                                EdgeInsertThreshold
                            );

                            if (edgeIndex >= 0)
                            {
                                document.InsertMidpointOnEdge(edgeIndex);
                                _shapeCanvas.Selection.Clear();
                                _shapeCanvas.Selection.AddPoint(edgeIndex + 1);
                                evt.Use();
                            }
                        }

                        break;
                    }

                case EventType.MouseDrag:
                    {
                        if (evt.button == 0)
                        {
                            _shapeCanvas.Interaction.UpdateMousePosition(mouseGui);

                            if (_shapeCanvas.Interaction.HasMouseDown && !_shapeCanvas.Interaction.IsDraggingSelection)
                            {
                                float dragDistance = Vector2.Distance(
                                    _shapeCanvas.Interaction.MouseDownScreenPosition,
                                    mouseGui
                                );

                                if (dragDistance >= DragStartThresholdPixels &&
                                    _shapeCanvas.Interaction.PressedPointId >= 0)
                                {
                                    StartPointDrag(drawRect, mouseGui);
                                    evt.Use();
                                    break;
                                }
                            }

                            if (_shapeCanvas.Interaction.IsDraggingSelection &&
                                _shapeCanvas.Selection.SelectedPointIds.Count > 0)
                            {
                                Vector2 worldMouse = ShapeCanvasUtility.GuiToWorld(
                                    mouseGui,
                                    drawRect,
                                    document.WorldSizeMeters
                                );

                                Vector2 lastWorldMouse = ShapeCanvasUtility.GuiToWorld(
                                    _shapeCanvas.Interaction.LastMouseScreenPosition,
                                    drawRect,
                                    document.WorldSizeMeters
                                );

                                Vector2 delta = worldMouse - lastWorldMouse;
                                MoveSelectedPoints(delta);

                                evt.Use();
                            }
                        }

                        break;
                    }

                case EventType.MouseUp:
                    {
                        if (evt.button == 0)
                        {
                            if (_shapeCanvas.Interaction.IsDraggingSelection)
                            {
                                _shapeCanvas.Interaction.EndMouseInteraction();
                                CancelPendingPointPress();
                                evt.Use();
                            }
                            else if (_shapeCanvas.Interaction.HasMouseDown &&
                                     _shapeCanvas.Interaction.PressedPointId >= 0)
                            {
                                CommitPendingPointClick();
                                _shapeCanvas.Interaction.EndMouseInteraction();
                                evt.Use();
                            }
                            else
                            {
                                _shapeCanvas.Interaction.EndMouseInteraction();
                            }
                        }

                        break;
                    }

                case EventType.KeyDown:
                    {
                        if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
                        {
                            if (_shapeCanvas.Selection.SelectedPointIds.Count > 0 && document.PointCount > 3)
                            {
                                DeleteSelectedPoints();
                                evt.Use();
                            }
                        }

                        break;
                    }
            }
        }

        private void BeginPendingPointPress(int pointIndex, Vector2 mouseGui)
        {
            _shapeCanvas.Interaction.HoveredPointId = pointIndex;
            _shapeCanvas.Interaction.PressedPointId = pointIndex;
            _shapeCanvas.Interaction.MouseDownScreenPosition = mouseGui;
            _shapeCanvas.Interaction.CurrentMouseScreenPosition = mouseGui;
            _shapeCanvas.Interaction.LastMouseScreenPosition = mouseGui;
        }

        private void StartPointDrag(Rect drawRect, Vector2 mouseGui)
        {
            int pointIndex = _shapeCanvas.Interaction.PressedPointId;
            if (pointIndex < 0 || pointIndex >= document.PointCount)
                return;

            bool additive = _shapeCanvas.IsAdditiveSelection(Event.current);
            bool pointWasAlreadySelected = _shapeCanvas.Selection.IsPointSelected(pointIndex);

            if (!pointWasAlreadySelected)
            {
                _shapeCanvas.Selection.Clear();
                _shapeCanvas.Selection.AddPoint(pointIndex);
            }
            else if (additive)
            {
                _shapeCanvas.Selection.SelectedSegmentIds.Clear();
            }
            else
            {
                _shapeCanvas.Selection.SelectedSegmentIds.Clear();
            }

            _shapeCanvas.Interaction.BeginDragSelection();
            _shapeCanvas.Interaction.LastMouseScreenPosition = mouseGui;
            _shapeCanvas.Interaction.CurrentMouseScreenPosition = mouseGui;

            Vector2 worldMouse = ShapeCanvasUtility.GuiToWorld(
                mouseGui,
                drawRect,
                document.WorldSizeMeters
            );

            _shapeCanvas.Interaction.MouseDownCanvasPosition = worldMouse;
        }

        private void CommitPendingPointClick()
        {
            int pointIndex = _shapeCanvas.Interaction.PressedPointId;
            if (pointIndex < 0 || pointIndex >= document.PointCount)
            {
                CancelPendingPointPress();
                return;
            }

            bool additive = _shapeCanvas.IsAdditiveSelection(Event.current);

            if (additive)
            {
                _shapeCanvas.Selection.SelectedSegmentIds.Clear();
                _shapeCanvas.Selection.TogglePoint(pointIndex);
            }
            else
            {
                _shapeCanvas.Selection.Clear();
                _shapeCanvas.Selection.AddPoint(pointIndex);
            }

            CancelPendingPointPress();
        }

        private void CancelPendingPointPress()
        {
            _shapeCanvas.Interaction.PressedPointId = -1;
            _shapeCanvas.Interaction.PressedSegmentId = -1;
        }

        private void UpdateHoverState(Rect drawRect, Vector2 mouseGui)
        {
            _shapeCanvas.Interaction.HoveredPointId = ShapeCanvasUtility.FindPointNearMouse(
                document.Points,
                drawRect,
                document.WorldSizeMeters,
                mouseGui,
                PointHandleRadius
            );

            _shapeCanvas.Interaction.HoveredSegmentId = _shapeCanvas.Interaction.HoveredPointId >= 0
                ? -1
                : ShapeCanvasUtility.FindEdgeNearMouse(
                    document.Points,
                    drawRect,
                    document.WorldSizeMeters,
                    mouseGui,
                    EdgeInsertThreshold
                );
        }

        private void MoveSelectedPoints(Vector2 delta)
        {
            document.MovePoints(_shapeCanvas.Selection.SelectedPointIds, delta);
        }

        private void DeleteSelectedPoints()
        {
            document.DeletePoints(_shapeCanvas.Selection.SelectedPointIds);
            _shapeCanvas.Selection.Clear();
        }

        private void EnsureValidShape()
        {
            document ??= new ShapeCanvasDocument();
            document.EnsureValidShape();

            canvasPercentOfWindow.x = Mathf.Clamp(canvasPercentOfWindow.x, 0.05f, 1f);
            canvasPercentOfWindow.y = Mathf.Clamp(canvasPercentOfWindow.y, 0.05f, 1f);
            verticalDividerPercent = Mathf.Clamp(verticalDividerPercent, 0.2f, 0.8f);

            PruneInvalidSelections();
        }

        private void ResetToDefaultTriangle()
        {
            document.ResetToDefaultTriangle();
            _shapeCanvas.Selection.Clear();
            CancelPendingPointPress();
            _shapeCanvas.Interaction.EndMouseInteraction();
        }

        private void PruneInvalidSelections()
        {
            document.PruneInvalidPointIndices(_shapeCanvas.Selection.SelectedPointIds);
            document.PruneInvalidEdgeIndices(_shapeCanvas.Selection.SelectedSegmentIds);

            if (_shapeCanvas.Interaction.PressedPointId < 0 ||
                _shapeCanvas.Interaction.PressedPointId >= document.PointCount)
            {
                CancelPendingPointPress();
            }
        }

        private void SyncCanvasSelectionFromLegacyData()
        {
            // Intentionally left as the seam for future migration.
            // Right now the canvas selection is the source of truth.
        }

        private List<int> GetSelectedPointIndices()
        {
            return new List<int>(_shapeCanvas.Selection.SelectedPointIds);
        }

        private List<int> GetSelectedEdgeIndices()
        {
            return new List<int>(_shapeCanvas.Selection.SelectedSegmentIds);
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

            leftWidth = Mathf.Max(0f, leftWidth);
            rightWidth = Mathf.Max(0f, rightWidth);

            float dividerX = fullCanvasArea.x + leftWidth;

            leftPanelRect = new Rect(
                fullCanvasArea.x,
                fullCanvasArea.y,
                leftWidth,
                fullCanvasArea.height
            );

            dividerRect = new Rect(
                dividerX,
                fullCanvasArea.y,
                DividerWidth,
                fullCanvasArea.height
            );

            dividerHitRect = new Rect(
                dividerRect.center.x - DividerHitWidth * 0.5f,
                fullCanvasArea.y,
                DividerHitWidth,
                fullCanvasArea.height
            );

            rightPanelRect = new Rect(
                dividerRect.xMax,
                fullCanvasArea.y,
                rightWidth,
                fullCanvasArea.height
            );
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

        private void DrawProfilePlaceholder(Rect allowedRect, Rect drawRect)
        {
            ShapeCanvasDrawing.DrawCanvasBackground(allowedRect, drawRect);

            Vector2 labelSize = EditorStyles.boldLabel.CalcSize(new GUIContent("Profile Canvas"));
            Vector2 subLabelSize = EditorStyles.miniLabel.CalcSize(new GUIContent("Placeholder for bevel/profile editor"));

            Rect labelRect = new Rect(
                drawRect.center.x - labelSize.x * 0.5f,
                drawRect.center.y - 16f,
                labelSize.x,
                labelSize.y
            );

            Rect subLabelRect = new Rect(
                drawRect.center.x - subLabelSize.x * 0.5f,
                drawRect.center.y + 4f,
                subLabelSize.x,
                subLabelSize.y
            );

            GUI.Label(labelRect, "Profile Canvas", EditorStyles.boldLabel);
            GUI.Label(subLabelRect, "Placeholder for bevel/profile editor", EditorStyles.miniLabel);
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }
    }
}