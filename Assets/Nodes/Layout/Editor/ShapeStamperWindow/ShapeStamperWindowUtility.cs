using System.Collections.Generic;
using UnityEngine;

namespace DLN
{
    public static class ShapeStamperWindowUtility
    {
        public static Rect GetAllowedCanvasRect(Rect availableRect, Vector2 canvasPercentOfWindow, float padding)
        {
            Rect padded = new Rect(
                availableRect.x + padding,
                availableRect.y + padding,
                Mathf.Max(0f, availableRect.width - padding * 2f),
                Mathf.Max(0f, availableRect.height - padding * 2f)
            );

            float percentWidth = Mathf.Clamp01(canvasPercentOfWindow.x);
            float percentHeight = Mathf.Clamp01(canvasPercentOfWindow.y);

            float width = padded.width * percentWidth;
            float height = padded.height * percentHeight;

            return new Rect(
                padded.x + (padded.width - width) * 0.5f,
                padded.y + (padded.height - height) * 0.5f,
                width,
                height
            );
        }

        public static Rect GetFittedWorldRect(Rect allowedRect, Vector2 worldSizeMeters)
        {
            if (worldSizeMeters.x <= 0f || worldSizeMeters.y <= 0f || allowedRect.width <= 0f || allowedRect.height <= 0f)
                return allowedRect;

            float worldAspect = worldSizeMeters.x / worldSizeMeters.y;
            float allowedAspect = allowedRect.width / allowedRect.height;

            float width;
            float height;

            if (allowedAspect > worldAspect)
            {
                height = allowedRect.height;
                width = height * worldAspect;
            }
            else
            {
                width = allowedRect.width;
                height = width / worldAspect;
            }

            return new Rect(
                allowedRect.x + (allowedRect.width - width) * 0.5f,
                allowedRect.y + (allowedRect.height - height) * 0.5f,
                width,
                height
            );
        }

        public static Vector2 WorldToGui(Vector2 worldPoint, Rect drawRect, Vector2 worldSizeMeters)
        {
            float x = drawRect.x + (worldPoint.x / worldSizeMeters.x) * drawRect.width;
            float y = drawRect.y + (worldPoint.y / worldSizeMeters.y) * drawRect.height;
            return new Vector2(x, y);
        }

        public static Vector2 GuiToWorld(Vector2 guiPoint, Rect drawRect, Vector2 worldSizeMeters)
        {
            float x = Mathf.InverseLerp(drawRect.x, drawRect.xMax, guiPoint.x) * worldSizeMeters.x;
            float y = Mathf.InverseLerp(drawRect.y, drawRect.yMax, guiPoint.y) * worldSizeMeters.y;
            return new Vector2(x, y);
        }

        public static Vector2 ClampToWorldBounds(Vector2 point, Vector2 worldSizeMeters)
        {
            return new Vector2(
                Mathf.Clamp(point.x, 0f, worldSizeMeters.x),
                Mathf.Clamp(point.y, 0f, worldSizeMeters.y)
            );
        }

        public static int FindPointNearMouse(
            List<Vector2> points,
            Rect drawRect,
            Vector2 worldSizeMeters,
            Vector2 mouseGui,
            float pointHandleRadius)
        {
            if (points == null)
                return -1;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 guiPoint = WorldToGui(points[i], drawRect, worldSizeMeters);
                if (Vector2.Distance(guiPoint, mouseGui) <= pointHandleRadius + 3f)
                    return i;
            }

            return -1;
        }

        public static int FindEdgeNearMouse(
            List<Vector2> points,
            Rect drawRect,
            Vector2 worldSizeMeters,
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
                Vector2 a = WorldToGui(points[i], drawRect, worldSizeMeters);
                Vector2 b = WorldToGui(points[next], drawRect, worldSizeMeters);

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