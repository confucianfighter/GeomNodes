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

        [SerializeField] private ShapeCanvasDocument document = new(); [SerializeField] private Vector2 canvasPercentOfWindow = new Vector2(0.8f, 0.8f);
        [SerializeField] private List<Vector2> points = new();

        private ShapeEditorCanvas _shapeCanvas;

        [MenuItem("Tools/DLN/Shape Stamper")]
        public static void Open()
        {
            var window = GetWindow<ShapeStamperWindow>();
            window.titleContent = new GUIContent("Shape Stamper");
            window.minSize = new Vector2(350f, 320f);
            window.Show();
        }

        private void OnEnable()
        {
            _shapeCanvas ??= new ShapeEditorCanvas();
            EnsureValidShape();
            SyncCanvasSelectionFromLegacyData();
        }

        private void OnGUI()
        {
            _shapeCanvas ??= new ShapeEditorCanvas();

            EnsureValidShape();
            SyncCanvasSelectionFromLegacyData();

            DrawTopBar();

            Rect fullCanvasArea = new Rect(
                0f,
                TopBarHeight,
                position.width,
                Mathf.Max(0f, position.height - TopBarHeight)
            );

            Rect allowedRect = DLN.ShapeStamperWindowUtility.GetAllowedCanvasRect(
                fullCanvasArea,
                canvasPercentOfWindow,
                CanvasPadding
            );

            Rect drawRect = DLN.ShapeStamperWindowUtility.GetFittedWorldRect(
                allowedRect,
                document.WorldSizeMeters
            );

            _shapeCanvas.SetScreenRect(drawRect);
            _shapeCanvas.PointHitRadiusPixels = PointHandleRadius;
            _shapeCanvas.SegmentHitDistancePixels = EdgeInsertThreshold;
            _shapeCanvas.DragThresholdPixels = DragStartThresholdPixels;

            HandleInput(drawRect);

            if (Event.current.type == EventType.Repaint)
            {
                UpdateHoverState(drawRect, Event.current.mousePosition);
            }

            DLN.ShapeStamperWindowDrawing.DrawCanvasBackground(allowedRect, drawRect);
            DLN.ShapeStamperWindowDrawing.DrawPolygon(
                document.Points,
                drawRect,
                document.WorldSizeMeters,
                _shapeCanvas.Interaction.HoveredPointId,
                _shapeCanvas.Interaction.HoveredSegmentId,
                GetSelectedPointIndices(),
                GetSelectedEdgeIndices(),
                PointHandleRadius
            );

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
                RescalePointsForWorldSizeChange(document.WorldSizeMeters, newWorldSize);
                document.WorldSizeMeters = newWorldSize;
                ClampAllPointsToWorldBounds();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset Triangle", GUILayout.Width(120f)))
            {
                ResetToDefaultTriangle();
                GUI.FocusControl(null);
            }

            EditorGUI.BeginDisabledGroup(_shapeCanvas.Selection.SelectedPointIds.Count == 0 || points.Count <= 3);
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

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(
                $"Points: {points.Count}   Selected Points: {_shapeCanvas.Selection.SelectedPointIds.Count}   Selected Edges: {_shapeCanvas.Selection.SelectedSegmentIds.Count}",
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

                        int pointIndex = DLN.ShapeStamperWindowUtility.FindPointNearMouse(
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
                                int edgeIndex = DLN.ShapeStamperWindowUtility.FindEdgeNearMouse(
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

                            int edgeIndex = DLN.ShapeStamperWindowUtility.FindEdgeNearMouse(
                                document.Points,
                                drawRect,
                                document.WorldSizeMeters,
                                mouseGui,
                                EdgeInsertThreshold
                            );

                            if (edgeIndex >= 0)
                            {
                                InsertMidpointOnEdge(edgeIndex);
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
                                Vector2 worldMouse = DLN.ShapeStamperWindowUtility.GuiToWorld(mouseGui, drawRect, document.WorldSizeMeters);
                                Vector2 lastWorldMouse = DLN.ShapeStamperWindowUtility.GuiToWorld(
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
                            if (_shapeCanvas.Selection.SelectedPointIds.Count > 0 && points.Count > 3)
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
            if (pointIndex < 0 || pointIndex >= points.Count)
                return;

            bool additive = _shapeCanvas.IsAdditiveSelection(Event.current);
            bool pointWasAlreadySelected = _shapeCanvas.Selection.IsPointSelected(pointIndex);

            if (additive)
            {
                if (!pointWasAlreadySelected)
                {
                    _shapeCanvas.Selection.Clear();
                    _shapeCanvas.Selection.AddPoint(pointIndex);
                }
                else
                {
                    _shapeCanvas.Selection.SelectedSegmentIds.Clear();
                }
            }
            else
            {
                if (!pointWasAlreadySelected)
                {
                    _shapeCanvas.Selection.Clear();
                    _shapeCanvas.Selection.AddPoint(pointIndex);
                }
                else
                {
                    _shapeCanvas.Selection.SelectedSegmentIds.Clear();
                }
            }

            _shapeCanvas.Interaction.BeginDragSelection();
            _shapeCanvas.Interaction.LastMouseScreenPosition = mouseGui;
            _shapeCanvas.Interaction.CurrentMouseScreenPosition = mouseGui;

            Vector2 worldMouse = DLN.ShapeStamperWindowUtility.GuiToWorld(mouseGui, drawRect, document.WorldSizeMeters);
            _shapeCanvas.Interaction.MouseDownCanvasPosition = worldMouse;
        }

        private void CommitPendingPointClick()
        {
            int pointIndex = _shapeCanvas.Interaction.PressedPointId;
            if (pointIndex < 0 || pointIndex >= points.Count)
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
            _shapeCanvas.Interaction.HoveredPointId = DLN.ShapeStamperWindowUtility.FindPointNearMouse(
                document.Points,
                drawRect,
                document.WorldSizeMeters,
                mouseGui,
                PointHandleRadius
            );

            _shapeCanvas.Interaction.HoveredSegmentId = _shapeCanvas.Interaction.HoveredPointId >= 0
                ? -1
                : DLN.ShapeStamperWindowUtility.FindEdgeNearMouse(
                    points,
                    drawRect,
                    document.WorldSizeMeters,
                    mouseGui,
                    EdgeInsertThreshold
                );
        }

        private void InsertMidpointOnEdge(int edgeIndex)
        {
            int next = (edgeIndex + 1) % points.Count;
            Vector2 a = points[edgeIndex];
            Vector2 b = points[next];
            Vector2 mid = (a + b) * 0.5f;
            points.Insert(edgeIndex + 1, mid);
        }

        private void MoveSelectedPoints(Vector2 delta)
        {
            foreach (int index in _shapeCanvas.Selection.SelectedPointIds)
            {
                if (index < 0 || index >= points.Count)
                    continue;

                points[index] = DLN.ShapeStamperWindowUtility.ClampToWorldBounds(points[index] + delta, document.WorldSizeMeters);
            }
        }

        private void DeleteSelectedPoints()
        {
            if (_shapeCanvas.Selection.SelectedPointIds.Count == 0)
                return;

            int maxRemovable = points.Count - 3;
            if (maxRemovable <= 0)
                return;

            List<int> sorted = GetSelectedPointIndices();
            sorted.Sort();
            sorted.Reverse();

            int removed = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                if (removed >= maxRemovable)
                    break;

                int index = sorted[i];
                if (index < 0 || index >= points.Count)
                    continue;

                points.RemoveAt(index);
                removed++;
            }

            _shapeCanvas.Selection.Clear();
        }

        private void EnsureValidShape()
        {
            if (points == null)
                points = new List<Vector2>();

            if (document.WorldSizeMeters.x <= 0f || document.WorldSizeMeters.y <= 0f)
                document.WorldSizeMeters = new Vector2(1f, 1f);

            canvasPercentOfWindow.x = Mathf.Clamp(canvasPercentOfWindow.x, 0.05f, 1f);
            canvasPercentOfWindow.y = Mathf.Clamp(canvasPercentOfWindow.y, 0.05f, 1f);

            if (points.Count < 3)
                ResetToDefaultTriangle();

            ClampAllPointsToWorldBounds();
            PruneInvalidSelections();
        }

        private void ResetToDefaultTriangle()
        {
            points = new List<Vector2>
            {
                new Vector2(document.WorldSizeMeters.x * 0.5f, document.WorldSizeMeters.y * 0.15f),
                new Vector2(document.WorldSizeMeters.x * 0.15f, document.WorldSizeMeters.y * 0.85f),
                new Vector2(document.WorldSizeMeters.x * 0.85f, document.WorldSizeMeters.y * 0.85f),
            };

            _shapeCanvas.Selection.Clear();
            CancelPendingPointPress();
            _shapeCanvas.Interaction.EndMouseInteraction();
        }

        private void ClampAllPointsToWorldBounds()
        {
            for (int i = 0; i < points.Count; i++)
                points[i] = DLN.ShapeStamperWindowUtility.ClampToWorldBounds(points[i], document.WorldSizeMeters);
        }

        private void RescalePointsForWorldSizeChange(Vector2 oldWorldSize, Vector2 newWorldSize)
        {
            float scaleX = oldWorldSize.x > 0f ? newWorldSize.x / oldWorldSize.x : 1f;
            float scaleY = oldWorldSize.y > 0f ? newWorldSize.y / oldWorldSize.y : 1f;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 p = points[i];
                p.x *= scaleX;
                p.y *= scaleY;
                points[i] = p;
            }
        }

        private void PruneInvalidSelections()
        {
            List<int> invalidPoints = null;
            foreach (int index in _shapeCanvas.Selection.SelectedPointIds)
            {
                if (index < 0 || index >= points.Count)
                {
                    invalidPoints ??= new List<int>();
                    invalidPoints.Add(index);
                }
            }

            if (invalidPoints != null)
            {
                for (int i = 0; i < invalidPoints.Count; i++)
                    _shapeCanvas.Selection.RemovePoint(invalidPoints[i]);
            }

            List<int> invalidEdges = null;
            foreach (int index in _shapeCanvas.Selection.SelectedSegmentIds)
            {
                if (index < 0 || index >= points.Count)
                {
                    invalidEdges ??= new List<int>();
                    invalidEdges.Add(index);
                }
            }

            if (invalidEdges != null)
            {
                for (int i = 0; i < invalidEdges.Count; i++)
                    _shapeCanvas.Selection.RemoveSegment(invalidEdges[i]);
            }

            if (_shapeCanvas.Interaction.PressedPointId < 0 || _shapeCanvas.Interaction.PressedPointId >= points.Count)
                CancelPendingPointPress();
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

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }
    }
}