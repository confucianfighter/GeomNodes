using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN
{
    public class ShapeStamperWindow : EditorWindow
    {
        private const float TopBarHeight = 70f;
        private const float CanvasPadding = 12f;
        private const float PointHandleRadius = 7f;
        private const float EdgeInsertThreshold = 10f;

        [SerializeField] private Vector2 canvasSize = new Vector2(200f, 200f);
        [SerializeField] private List<Vector2> points = new List<Vector2>();

        [SerializeField] private List<int> selectedPointIndices = new List<int>();
        [SerializeField] private List<int> selectedEdgeIndices = new List<int>();

        private int hoveredPointIndex = -1;
        private int hoveredEdgeIndex = -1;

        private bool isDraggingPoints;
        private Vector2 lastDragCanvasMouse;

        [MenuItem("Tools/DLN/Shape Stamper")]
        public static void Open()
        {
            var window = GetWindow<ShapeStamperWindow>();
            window.titleContent = new GUIContent("Shape Stamper");
            window.minSize = new Vector2(350f, 300f);
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

            Rect drawRect = ShapeStamperWindowUtility.GetFittedCanvasRect(
                fullCanvasArea,
                canvasSize,
                CanvasPadding
            );

            ShapeStamperWindowDrawing.DrawCanvasBackground(drawRect);
            ShapeStamperWindowDrawing.DrawPolygon(
                points,
                drawRect,
                canvasSize,
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
            EditorGUILayout.LabelField("Canvas Size", GUILayout.Width(80f));

            EditorGUILayout.LabelField("W", GUILayout.Width(16f));
            float newWidth = EditorGUILayout.FloatField(canvasSize.x, GUILayout.Width(70f));

            EditorGUILayout.LabelField("H", GUILayout.Width(16f));
            float newHeight = EditorGUILayout.FloatField(canvasSize.y, GUILayout.Width(70f));

            newWidth = Mathf.Max(1f, newWidth);
            newHeight = Mathf.Max(1f, newHeight);

            if (!Mathf.Approximately(newWidth, canvasSize.x) || !Mathf.Approximately(newHeight, canvasSize.y))
            {
                canvasSize = new Vector2(newWidth, newHeight);
                ClampAllPointsToCanvas();
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

                    bool additive = evt.shift;
                    int pointIndex = ShapeStamperWindowUtility.FindPointNearMouse(
                        points,
                        drawRect,
                        canvasSize,
                        mouseGui,
                        PointHandleRadius
                    );

                    if (evt.button == 0)
                    {
                        if (pointIndex >= 0)
                        {
                            if (!additive)
                            {
                                ClearSelection();
                                AddPointSelection(pointIndex);
                            }
                            else
                            {
                                TogglePointSelection(pointIndex);
                            }

                            selectedEdgeIndices.Clear();
                            isDraggingPoints = selectedPointIndices.Contains(pointIndex);
                            lastDragCanvasMouse = ShapeStamperWindowUtility.GuiToCanvas(mouseGui, drawRect, canvasSize);
                            evt.Use();
                        }
                        else
                        {
                            int edgeIndex = ShapeStamperWindowUtility.FindEdgeNearMouse(
                                points,
                                drawRect,
                                canvasSize,
                                mouseGui,
                                EdgeInsertThreshold
                            );

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
                                }

                                selectedPointIndices.Clear();
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
                        int edgeIndex = ShapeStamperWindowUtility.FindEdgeNearMouse(
                            points,
                            drawRect,
                            canvasSize,
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
                    if (evt.button == 0 && isDraggingPoints && selectedPointIndices.Count > 0)
                    {
                        Vector2 canvasMouse = ShapeStamperWindowUtility.GuiToCanvas(mouseGui, drawRect, canvasSize);
                        Vector2 delta = canvasMouse - lastDragCanvasMouse;

                        MoveSelectedPoints(delta);

                        lastDragCanvasMouse = canvasMouse;
                        evt.Use();
                    }

                    break;
                }

                case EventType.MouseUp:
                {
                    if (evt.button == 0 && isDraggingPoints)
                    {
                        isDraggingPoints = false;
                        evt.Use();
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

        private void UpdateHoverState(Rect drawRect, Vector2 mouseGui)
        {
            hoveredPointIndex = ShapeStamperWindowUtility.FindPointNearMouse(
                points,
                drawRect,
                canvasSize,
                mouseGui,
                PointHandleRadius
            );

            hoveredEdgeIndex = hoveredPointIndex >= 0
                ? -1
                : ShapeStamperWindowUtility.FindEdgeNearMouse(
                    points,
                    drawRect,
                    canvasSize,
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

                points[index] = ShapeStamperWindowUtility.ClampToCanvas(points[index] + delta, canvasSize);
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

            if (canvasSize.x <= 0f || canvasSize.y <= 0f)
                canvasSize = new Vector2(200f, 200f);

            if (points.Count < 3)
                ResetToDefaultTriangle();

            ClampAllPointsToCanvas();
            PruneInvalidSelections();
        }

        private void ResetToDefaultTriangle()
        {
            points = new List<Vector2>
            {
                new Vector2(canvasSize.x * 0.5f, canvasSize.y * 0.15f),
                new Vector2(canvasSize.x * 0.15f, canvasSize.y * 0.85f),
                new Vector2(canvasSize.x * 0.85f, canvasSize.y * 0.85f),
            };

            ClearSelection();
        }

        private void ClampAllPointsToCanvas()
        {
            for (int i = 0; i < points.Count; i++)
                points[i] = ShapeStamperWindowUtility.ClampToCanvas(points[i], canvasSize);
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
        }
    }
}