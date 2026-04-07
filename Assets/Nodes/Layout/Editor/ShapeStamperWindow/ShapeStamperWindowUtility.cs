using System.Collections.Generic;
using UnityEngine;

namespace DLN
{
    public static class ShapeStamperWindowUtility
    {
        public static Rect GetFittedCanvasRect(Rect availableRect, Vector2 canvasSize, float padding)
        {
            Rect padded = new Rect(
                availableRect.x + padding,
                availableRect.y + padding,
                Mathf.Max(0f, availableRect.width - padding * 2f),
                Mathf.Max(0f, availableRect.height - padding * 2f)
            );

            if (canvasSize.x <= 0f || canvasSize.y <= 0f || padded.width <= 0f || padded.height <= 0f)
                return padded;

            float canvasAspect = canvasSize.x / canvasSize.y;
            float areaAspect = padded.width / padded.height;

            float width;
            float height;

            if (areaAspect > canvasAspect)
            {
                height = padded.height;
                width = height * canvasAspect;
            }
            else
            {
                width = padded.width;
                height = width / canvasAspect;
            }

            return new Rect(
                padded.x + (padded.width - width) * 0.5f,
                padded.y + (padded.height - height) * 0.5f,
                width,
                height
            );
        }

        public static Vector2 CanvasToGui(Vector2 canvasPoint, Rect drawRect, Vector2 canvasSize)
        {
            float x = drawRect.x + (canvasPoint.x / canvasSize.x) * drawRect.width;
            float y = drawRect.y + (canvasPoint.y / canvasSize.y) * drawRect.height;
            return new Vector2(x, y);
        }

        public static Vector2 GuiToCanvas(Vector2 guiPoint, Rect drawRect, Vector2 canvasSize)
        {
            float x = Mathf.InverseLerp(drawRect.x, drawRect.xMax, guiPoint.x) * canvasSize.x;
            float y = Mathf.InverseLerp(drawRect.y, drawRect.yMax, guiPoint.y) * canvasSize.y;
            return new Vector2(x, y);
        }

        public static Vector2 ClampToCanvas(Vector2 point, Vector2 canvasSize)
        {
            return new Vector2(
                Mathf.Clamp(point.x, 0f, canvasSize.x),
                Mathf.Clamp(point.y, 0f, canvasSize.y)
            );
        }

        public static int FindPointNearMouse(
            List<Vector2> points,
            Rect drawRect,
            Vector2 canvasSize,
            Vector2 mouseGui,
            float pointHandleRadius)
        {
            if (points == null)
                return -1;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 guiPoint = CanvasToGui(points[i], drawRect, canvasSize);
                if (Vector2.Distance(guiPoint, mouseGui) <= pointHandleRadius + 3f)
                    return i;
            }

            return -1;
        }

        public static int FindEdgeNearMouse(
            List<Vector2> points,
            Rect drawRect,
            Vector2 canvasSize,
            Vector2 mouseGui,
            float edgeInsertThreshold)
        {
            if (points == null || points.Count < 2)
                return -1;

            float bestDistance = float.MaxValue;
            int bestEdge = -1;

            for (int i = 0; i < points.Count; i++)
            {
                int next = (i + 1) % points.Count;
                Vector2 a = CanvasToGui(points[i], drawRect, canvasSize);
                Vector2 b = CanvasToGui(points[next], drawRect, canvasSize);

                float dist = DistancePointToSegment(mouseGui, a, b);
                if (dist < edgeInsertThreshold && dist < bestDistance)
                {
                    bestDistance = dist;
                    bestEdge = i;
                }
            }

            return bestEdge;
        }

        public static float DistancePointToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            return Vector2.Distance(p, ClosestPointOnSegment(a, b, p));
        }

        public static Vector2 ClosestPointOnSegment(Vector2 a, Vector2 b, Vector2 p)
        {
            Vector2 ab = b - a;
            float denom = ab.sqrMagnitude;
            if (denom < 0.000001f)
                return a;

            float t = Vector2.Dot(p - a, ab) / denom;
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }
    }
}