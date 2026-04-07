using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN
{
    public class ShapeStamperWindow : EditorWindow
    {
        private const float TopBarHeight = 92f;
        private const float CanvasPadding = 12f;
        private const float PointHandleRadius = 7f;
        private const float EdgeInsertThreshold = 10f;
        private const float DragStartThresholdPixels = 5f;

        [SerializeField] private Vector2 worldSizeMeters = new Vector2(1f, 1f);
        [SerializeField] private Vector2 canvasPercentOfWindow = new Vector2(0.8f, 0.8f);
        [SerializeField] private List<Vector2> points = new List<Vector2>();

        [SerializeField] private List<int> selectedPointIndices = new List<int>();
        [SerializeField] private List<int> selectedEdgeIndices = new List<int>();

        private int hoveredPointIndex = -1;
        private int hoveredEdgeIndex = -1;

        private bool isDraggingPoints;
        private Vector2 lastDragWorldMouse;

        private bool hasPendingPointPress;
        private int pendingPointIndex = -1;
        private bool pendingAdditive;
        private bool pendingPointWasAlreadySelected;
        private Vector2 pendingMouseDownGui;

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
            EnsureValidShape();
        }

        private void OnGUI()
        {
            EnsureValidShape();

            DrawTopBar();

            Rect fullCanvasArea = new Rect(
                0f,
                TopBarHeight,
                position.width,
                Mathf.Max(0f, position.height - TopBarHeight)
            );

            Rect allowedRect = ShapeStamperWindowUtility.GetAllowedCanvasRect(
                fullCanvasArea,
                canvasPercentOfWindow,
                CanvasPadding
            );

            Rect drawRect = ShapeStamperWindowUtility.GetFittedWorldRect(
                allowedRect,
                worldSizeMeters
            );

            ShapeStamperWindowDrawing.DrawCanvasBackground(allowedRect, drawRect);
            ShapeStamperWindowDrawing.DrawPolygon(
                points,
                drawRect,
                worldSizeMeters,
                hoveredPointIndex,
                hoveredEdgeIndex,
                selectedPointIndices,
                selectedEdgeIndices,
                PointHandleRadius
            );

            HandleInput(drawRect);

            if (Event.current.type == EventType.Repaint)
            {
                UpdateHoverState(drawRect, Event.current.mousePosition);
            }

            Repaint();
        }

        private void DrawTopBar()
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(TopBarHeight));
            EditorGUILayout.Space(6f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("World Size (m)", GUILayout.Width(92f));

            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newWorldWidth = EditorGUILayout.FloatField(worldSizeMeters.x, GUILayout.Width(70f));

            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newWorldHeight = EditorGUILayout.FloatField(worldSizeMeters.y, GUILayout.Width(70f));

            newWorldWidth = Mathf.Max(0.0001f, newWorldWidth);
            newWorldHeight = Mathf.Max(0.0001f, newWorldHeight);

            Vector2 newWorldSize = new Vector2(newWorldWidth, newWorldHeight);
            if (!Approximately(newWorldSize, worldSizeMeters))
            {
                RescalePointsForWorldSizeChange(worldSizeMeters, newWorldSize);
                worldSizeMeters = newWorldSize;
                ClampAllPointsToWorldBounds();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Reset Triangle", GUILayout.Width(120f)))
            {
                ResetToDefaultTriangle();
                GUI.FocusControl(null);
            }

            EditorGUI.BeginDisabledGroup(selectedPointIndices.Count == 0 || points.Count <= 3);
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
                $"Points: {points.Count}   Selected Points: {selectedPointIndices.Count}   Selected Edges: {selectedEdgeIndices.Count}",
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

                    bool additive = evt.shift || evt.control || evt.command;

                    int pointIndex = ShapeStamperWindowUtility.FindPointNearMouse(
                        points,
                        drawRect,
                        worldSizeMeters,
                        mouseGui,
                        PointHandleRadius
                    );

                    if (evt.button == 0)
                    {
                        if (pointIndex >= 0)
                        {
                            BeginPendingPointPress(pointIndex, additive, mouseGui);
                            evt.Use();
                        }
                        else
                        {
                            int edgeIndex = ShapeStamperWindowUtility.FindEdgeNearMouse(
                                points,
                                drawRect,
                                worldSizeMeters,
                                mouseGui,
                                EdgeInsertThreshold
                            );

                            CancelPendingPointPress();

                            if (edgeIndex >= 0)
                            {
                                if (!additive)
                                {
                                    ClearSelection();
                                    AddEdgeSelection(edgeIndex);
                                }
                                else
                                {
                                    ToggleEdgeSelection(edgeIndex);
                                    selectedPointIndices.Clear();
                                }

                                evt.Use();
                            }
                            else
                            {
                                if (!additive)
                                    ClearSelection();

                                evt.Use();
                            }
                        }
                    }
                    else if (evt.button == 1)
                    {
                        CancelPendingPointPress();

                        int edgeIndex = ShapeStamperWindowUtility.FindEdgeNearMouse(
                            points,
                            drawRect,
                            worldSizeMeters,
                            mouseGui,
                            EdgeInsertThreshold
                        );

                        if (edgeIndex >= 0)
                        {
                            InsertMidpointOnEdge(edgeIndex);
                            ClearSelection();
                            AddPointSelection(edgeIndex + 1);
                            evt.Use();
                        }
                    }

                    break;
                }

                case EventType.MouseDrag:
                {
                    if (evt.button == 0)
                    {
                        if (hasPendingPointPress && !isDraggingPoints)
                        {
                            float dragDistance = Vector2.Distance(mouseGui, pendingMouseDownGui);
                            if (dragDistance >= DragStartThresholdPixels)
                            {
                                StartPointDrag(drawRect, mouseGui);
                                evt.Use();
                                break;
                            }
                        }

                        if (isDraggingPoints && selectedPointIndices.Count > 0)
                        {
                            Vector2 worldMouse = ShapeStamperWindowUtility.GuiToWorld(mouseGui, drawRect, worldSizeMeters);
                            Vector2 delta = worldMouse - lastDragWorldMouse;

                            MoveSelectedPoints(delta);

                            lastDragWorldMouse = worldMouse;
                            evt.Use();
                        }
                    }

                    break;
                }

                case EventType.MouseUp:
                {
                    if (evt.button == 0)
                    {
                        if (isDraggingPoints)
                        {
                            isDraggingPoints = false;
                            CancelPendingPointPress();
                            evt.Use();
                        }
                        else if (hasPendingPointPress)
                        {
                            CommitPendingPointClick();
                            evt.Use();
                        }
                    }

                    break;
                }

                case EventType.KeyDown:
                {
                    if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
                    {
                        if (selectedPointIndices.Count > 0 && points.Count > 3)
                        {
                            DeleteSelectedPoints();
                            evt.Use();
                        }
                    }

                    break;
                }
            }
        }

        private void BeginPendingPointPress(int pointIndex, bool additive, Vector2 mouseGui)
        {
            hasPendingPointPress = true;
            pendingPointIndex = pointIndex;
            pendingAdditive = additive;
            pendingPointWasAlreadySelected = selectedPointIndices.Contains(pointIndex);
            pendingMouseDownGui = mouseGui;
        }

        private void StartPointDrag(Rect drawRect, Vector2 mouseGui)
        {
            if (!hasPendingPointPress || pendingPointIndex < 0 || pendingPointIndex >= points.Count)
                return;

            if (pendingAdditive)
            {
                if (!pendingPointWasAlreadySelected)
                {
                    selectedPointIndices.Clear();
                    selectedEdgeIndices.Clear();
                    AddPointSelection(pendingPointIndex);
                }
                else
                {
                    selectedEdgeIndices.Clear();
                }
            }
            else
            {
                if (!pendingPointWasAlreadySelected)
                {
                    ClearSelection();
                    AddPointSelection(pendingPointIndex);
                }
                else
                {
                    selectedEdgeIndices.Clear();
                }
            }

            isDraggingPoints = selectedPointIndices.Contains(pendingPointIndex);
            lastDragWorldMouse = ShapeStamperWindowUtility.GuiToWorld(mouseGui, drawRect, worldSizeMeters);
        }

        private void CommitPendingPointClick()
        {
            if (!hasPendingPointPress || pendingPointIndex < 0 || pendingPointIndex >= points.Count)
            {
                CancelPendingPointPress();
                return;
            }

            if (pendingAdditive)
            {
                selectedEdgeIndices.Clear();
                TogglePointSelection(pendingPointIndex);
            }
            else
            {
                ClearSelection();
                AddPointSelection(pendingPointIndex);
            }

            CancelPendingPointPress();
        }

        private void CancelPendingPointPress()
        {
            hasPendingPointPress = false;
            pendingPointIndex = -1;
            pendingAdditive = false;
            pendingPointWasAlreadySelected = false;
        }

        private void UpdateHoverState(Rect drawRect, Vector2 mouseGui)
        {
            hoveredPointIndex = ShapeStamperWindowUtility.FindPointNearMouse(
                points,
                drawRect,
                worldSizeMeters,
                mouseGui,
                PointHandleRadius
            );

            hoveredEdgeIndex = hoveredPointIndex >= 0
                ? -1
                : ShapeStamperWindowUtility.FindEdgeNearMouse(
                    points,
                    drawRect,
                    worldSizeMeters,
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
            for (int i = 0; i < selectedPointIndices.Count; i++)
            {
                int index = selectedPointIndices[i];
                if (index < 0 || index >= points.Count)
                    continue;

                points[index] = ShapeStamperWindowUtility.ClampToWorldBounds(points[index] + delta, worldSizeMeters);
            }
        }

        private void DeleteSelectedPoints()
        {
            if (selectedPointIndices.Count == 0)
                return;

            int maxRemovable = points.Count - 3;
            if (maxRemovable <= 0)
                return;

            selectedPointIndices.Sort();
            selectedPointIndices.Reverse();

            int removed = 0;
            for (int i = 0; i < selectedPointIndices.Count; i++)
            {
                if (removed >= maxRemovable)
                    break;

                int index = selectedPointIndices[i];
                if (index < 0 || index >= points.Count)
                    continue;

                points.RemoveAt(index);
                removed++;
            }

            selectedPointIndices.Clear();
            selectedEdgeIndices.Clear();
        }

        private void EnsureValidShape()
        {
            if (points == null)
                points = new List<Vector2>();

            if (selectedPointIndices == null)
                selectedPointIndices = new List<int>();

            if (selectedEdgeIndices == null)
                selectedEdgeIndices = new List<int>();

            if (worldSizeMeters.x <= 0f || worldSizeMeters.y <= 0f)
                worldSizeMeters = new Vector2(1f, 1f);

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
                new Vector2(worldSizeMeters.x * 0.5f, worldSizeMeters.y * 0.15f),
                new Vector2(worldSizeMeters.x * 0.15f, worldSizeMeters.y * 0.85f),
                new Vector2(worldSizeMeters.x * 0.85f, worldSizeMeters.y * 0.85f),
            };

            ClearSelection();
            CancelPendingPointPress();
            isDraggingPoints = false;
        }

        private void ClampAllPointsToWorldBounds()
        {
            for (int i = 0; i < points.Count; i++)
                points[i] = ShapeStamperWindowUtility.ClampToWorldBounds(points[i], worldSizeMeters);
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

        private void ClearSelection()
        {
            selectedPointIndices.Clear();
            selectedEdgeIndices.Clear();
        }

        private void AddPointSelection(int index)
        {
            if (!selectedPointIndices.Contains(index))
                selectedPointIndices.Add(index);
        }

        private void AddEdgeSelection(int index)
        {
            if (!selectedEdgeIndices.Contains(index))
                selectedEdgeIndices.Add(index);
        }

        private void TogglePointSelection(int index)
        {
            if (selectedPointIndices.Contains(index))
                selectedPointIndices.Remove(index);
            else
                selectedPointIndices.Add(index);
        }

        private void ToggleEdgeSelection(int index)
        {
            if (selectedEdgeIndices.Contains(index))
                selectedEdgeIndices.Remove(index);
            else
                selectedEdgeIndices.Add(index);
        }

        private void PruneInvalidSelections()
        {
            for (int i = selectedPointIndices.Count - 1; i >= 0; i--)
            {
                int index = selectedPointIndices[i];
                if (index < 0 || index >= points.Count)
                    selectedPointIndices.RemoveAt(i);
            }

            for (int i = selectedEdgeIndices.Count - 1; i >= 0; i--)
            {
                int index = selectedEdgeIndices[i];
                if (index < 0 || index >= points.Count)
                    selectedEdgeIndices.RemoveAt(i);
            }

            if (pendingPointIndex < 0 || pendingPointIndex >= points.Count)
                CancelPendingPointPress();
        }

        private static bool Approximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
        }
    }
}