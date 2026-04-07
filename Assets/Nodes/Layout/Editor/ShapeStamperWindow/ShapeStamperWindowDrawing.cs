using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN
{
    public static class ShapeStamperWindowDrawing
    {
        public static void DrawCanvasBackground(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.14f, 0.14f, 0.14f));
            Handles.color = new Color(0.32f, 0.32f, 0.32f);
            Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0.4f, 0.4f, 0.4f));
        }

        public static void DrawPolygon(
            List<Vector2> points,
            Rect drawRect,
            Vector2 canvasSize,
            int hoveredPointIndex,
            int hoveredEdgeIndex,
            List<int> selectedPointIndices,
            List<int> selectedEdgeIndices,
            float pointHandleRadius)
        {
            if (points == null || points.Count < 2)
                return;

            Handles.BeginGUI();

            DrawEdges(points, drawRect, canvasSize, hoveredEdgeIndex, selectedEdgeIndices);
            DrawPoints(points, drawRect, canvasSize, hoveredPointIndex, selectedPointIndices, pointHandleRadius);

            Handles.EndGUI();
        }

        private static void DrawEdges(
            List<Vector2> points,
            Rect drawRect,
            Vector2 canvasSize,
            int hoveredEdgeIndex,
            List<int> selectedEdgeIndices)
        {
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;

                Vector2 a = ShapeStamperWindowUtility.CanvasToGui(points[i], drawRect, canvasSize);
                Vector2 b = ShapeStamperWindowUtility.CanvasToGui(points[next], drawRect, canvasSize);

                Handles.color = selectedEdgeIndices.Contains(i)
                    ? Color.green
                    : (i == hoveredEdgeIndex ? Color.yellow : Color.white);

                Handles.DrawLine(a, b);
            }
        }

        private static void DrawPoints(
            List<Vector2> points,
            Rect drawRect,
            Vector2 canvasSize,
            int hoveredPointIndex,
            List<int> selectedPointIndices,
            float pointHandleRadius)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 guiPoint = ShapeStamperWindowUtility.CanvasToGui(points[i], drawRect, canvasSize);

                Color color = Color.white;
                if (selectedPointIndices.Contains(i))
                    color = Color.green;
                else if (i == hoveredPointIndex)
                    color = Color.yellow;

                Handles.color = color;
                Handles.DrawSolidDisc(guiPoint, Vector3.forward, pointHandleRadius);
            }
        }
    }
}