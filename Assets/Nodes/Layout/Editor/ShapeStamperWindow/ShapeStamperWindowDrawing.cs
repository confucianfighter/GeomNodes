using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DLN
{
    public static class ShapeStamperWindowDrawing
    {
        public static void DrawCanvasBackground(Rect allowedRect, Rect drawRect)
        {
            EditorGUI.DrawRect(allowedRect, new Color(0.11f, 0.11f, 0.11f));
            EditorGUI.DrawRect(drawRect, new Color(0.14f, 0.14f, 0.14f));

            Handles.color = new Color(0.4f, 0.4f, 0.4f);
            Handles.DrawSolidRectangleWithOutline(allowedRect, Color.clear, new Color(0.32f, 0.32f, 0.32f));
            Handles.DrawSolidRectangleWithOutline(drawRect, Color.clear, new Color(0.5f, 0.5f, 0.5f));
        }

        public static void DrawPolygon(
            List<Vector2> points,
            Rect drawRect,
            Vector2 worldSizeMeters,
            int hoveredPointIndex,
            int hoveredEdgeIndex,
            List<int> selectedPointIndices,
            List<int> selectedEdgeIndices,
            float pointHandleRadius)
        {
            if (points == null || points.Count < 2)
                return;

            Handles.BeginGUI();

            DrawEdges(points, drawRect, worldSizeMeters, hoveredEdgeIndex, selectedEdgeIndices);
            DrawPoints(points, drawRect, worldSizeMeters, hoveredPointIndex, selectedPointIndices, pointHandleRadius);

            Handles.EndGUI();
        }

        private static void DrawEdges(
            List<Vector2> points,
            Rect drawRect,
            Vector2 worldSizeMeters,
            int hoveredEdgeIndex,
            List<int> selectedEdgeIndices)
        {
            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;

                Vector2 a = ShapeStamperWindowUtility.WorldToGui(points[i], drawRect, worldSizeMeters);
                Vector2 b = ShapeStamperWindowUtility.WorldToGui(points[next], drawRect, worldSizeMeters);

                Handles.color = selectedEdgeIndices.Contains(i)
                    ? Color.green
                    : (i == hoveredEdgeIndex ? Color.yellow : Color.white);

                Handles.DrawLine(a, b);
            }
        }

        private static void DrawPoints(
            List<Vector2> points,
            Rect drawRect,
            Vector2 worldSizeMeters,
            int hoveredPointIndex,
            List<int> selectedPointIndices,
            float pointHandleRadius)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 guiPoint = ShapeStamperWindowUtility.WorldToGui(points[i], drawRect, worldSizeMeters);

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