#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class CanvasMath
    {
        public static Vector2 ScreenToCanvas(Vector2 screenPosition, Rect canvasRect, CanvasViewState view, ICanvasDocument document)
        {
            Rect worldRect = GetWorldRect(document);
            Rect fittedRect = GetFittedWorldScreenRect(canvasRect, view, document);

            float tx = Mathf.InverseLerp(fittedRect.xMin, fittedRect.xMax, screenPosition.x);
            float ty = Mathf.InverseLerp(fittedRect.yMin, fittedRect.yMax, screenPosition.y);

            return new Vector2(
                Mathf.Lerp(worldRect.xMin, worldRect.xMax, tx),
                Mathf.Lerp(worldRect.yMin, worldRect.yMax, ty)
            );
        }

        public static Vector2 CanvasToScreen(Vector2 canvasPosition, Rect canvasRect, CanvasViewState view, ICanvasDocument document)
        {
            Rect worldRect = GetWorldRect(document);
            Rect fittedRect = GetFittedWorldScreenRect(canvasRect, view, document);

            float tx = Mathf.InverseLerp(worldRect.xMin, worldRect.xMax, canvasPosition.x);
            float ty = Mathf.InverseLerp(worldRect.yMin, worldRect.yMax, canvasPosition.y);

            return new Vector2(
                Mathf.Lerp(fittedRect.xMin, fittedRect.xMax, tx),
                Mathf.Lerp(fittedRect.yMin, fittedRect.yMax, ty)
            );
        }

        public static Rect GetFittedWorldScreenRect(Rect canvasRect, CanvasViewState view, ICanvasDocument document)
        {
            Rect worldRect = GetWorldRect(document);

            float padding = view != null ? Mathf.Max(0f, view.WorldPaddingPixels) : 24f;

            float availableWidth = Mathf.Max(1f, canvasRect.width - padding * 2f);
            float availableHeight = Mathf.Max(1f, canvasRect.height - padding * 2f);

            float worldWidth = Mathf.Max(0.0001f, worldRect.width);
            float worldHeight = Mathf.Max(0.0001f, worldRect.height);

            float scale = Mathf.Min(availableWidth / worldWidth, availableHeight / worldHeight);
            scale = Mathf.Max(0.0001f, scale);

            float fittedWidth = worldWidth * scale;
            float fittedHeight = worldHeight * scale;

            float x = canvasRect.x + (canvasRect.width - fittedWidth) * 0.5f;
            float y = canvasRect.y + (canvasRect.height - fittedHeight) * 0.5f;

            return new Rect(x, y, fittedWidth, fittedHeight);
        }

        public static Rect GetWorldRect(ICanvasDocument document)
        {
            if (document is ICanvasBoundsProvider boundsProvider)
                return SanitizeRect(boundsProvider.GetCanvasFrameRect());

            if (document != null && document.Points != null && document.Points.Count > 0)
                return GetCanvasBounds(document.Points);

            return new Rect(0f, 0f, 1f, 1f);
        }

        public static float DistancePointToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 closest = ClosestPointOnSegment(point, a, b);
            return Vector2.Distance(point, closest);
        }

        public static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float lengthSq = ab.sqrMagnitude;

            if (lengthSq <= 0.000001f)
                return a;

            float t = Vector2.Dot(point - a, ab) / lengthSq;
            t = Mathf.Clamp01(t);
            return a + ab * t;
        }

        public static Vector2 GetSegmentMidpoint(Vector2 a, Vector2 b)
        {
            return (a + b) * 0.5f;
        }

        public static Vector2 GetPerpendicular(Vector2 v)
        {
            return new Vector2(-v.y, v.x);
        }

        public static Vector2 GetUnitPerpendicular(Vector2 a, Vector2 b)
        {
            Vector2 dir = b - a;
            if (dir.sqrMagnitude <= 0.000001f)
                return Vector2.up;

            return GetPerpendicular(dir.normalized);
        }

        public static Rect MakeRect(Vector2 a, Vector2 b)
        {
            Vector2 min = Vector2.Min(a, b);
            Vector2 max = Vector2.Max(a, b);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static bool TryGetEdgeCanvasPositions(
            CanvasEdge edge,
            ICanvasDocument document,
            out Vector2 a,
            out Vector2 b)
        {
            a = default;
            b = default;

            if (!TryGetPoint(document, edge.A, out CanvasPoint pointA))
                return false;

            if (!TryGetPoint(document, edge.B, out CanvasPoint pointB))
                return false;

            a = pointA.Position;
            b = pointB.Position;
            return true;
        }

        public static bool TryGetEdgeScreenPositions(
            CanvasEdge edge,
            ICanvasDocument document,
            Rect canvasRect,
            CanvasViewState view,
            out Vector2 a,
            out Vector2 b)
        {
            a = default;
            b = default;

            if (!TryGetEdgeCanvasPositions(edge, document, out Vector2 canvasA, out Vector2 canvasB))
                return false;

            a = CanvasToScreen(canvasA, canvasRect, view, document);
            b = CanvasToScreen(canvasB, canvasRect, view, document);
            return true;
        }

        public static bool TryGetOffsetHandleCanvasPosition(
            CanvasOffsetConstraint offset,
            ICanvasDocument document,
            out Vector2 handleCanvasPosition)
        {
            handleCanvasPosition = default;

            if (!TryGetEdgeById(document, offset.EdgeId, out CanvasEdge edge))
                return false;

            if (!TryGetEdgeCanvasPositions(edge, document, out Vector2 a, out Vector2 b))
                return false;

            Vector2 midpoint = GetSegmentMidpoint(a, b);
            Vector2 normal = GetUnitPerpendicular(a, b);

            handleCanvasPosition = midpoint + normal * offset.Distance;
            return true;
        }

        public static bool TryGetOffsetHandleScreenPosition(
            CanvasOffsetConstraint offset,
            ICanvasDocument document,
            Rect canvasRect,
            CanvasViewState view,
            out Vector2 handleScreenPosition)
        {
            handleScreenPosition = default;

            if (!TryGetOffsetHandleCanvasPosition(offset, document, out Vector2 handleCanvas))
                return false;

            handleScreenPosition = CanvasToScreen(handleCanvas, canvasRect, view, document);
            return true;
        }

        public static bool TryGetPoint(ICanvasDocument document, int pointId, out CanvasPoint point)
        {
            if (document != null)
            {
                foreach (CanvasPoint p in document.Points)
                {
                    if (p.Id == pointId)
                    {
                        point = p;
                        return true;
                    }
                }
            }

            point = default;
            return false;
        }

        public static bool TryGetEdgeById(ICanvasDocument document, int edgeId, out CanvasEdge edge)
        {
            if (document != null)
            {
                foreach (CanvasEdge e in document.Edges)
                {
                    if (e.Id == edgeId)
                    {
                        edge = e;
                        return true;
                    }
                }
            }

            edge = default;
            return false;
        }

        private static Rect GetCanvasBounds(IList<CanvasPoint> points)
        {
            if (points == null || points.Count == 0)
                return new Rect(0f, 0f, 1f, 1f);

            Vector2 min = points[0].Position;
            Vector2 max = points[0].Position;

            for (int i = 1; i < points.Count; i++)
            {
                min = Vector2.Min(min, points[i].Position);
                max = Vector2.Max(max, points[i].Position);
            }

            Vector2 size = max - min;
            if (size.x < 0.001f) size.x = 1f;
            if (size.y < 0.001f) size.y = 1f;

            return new Rect(min, size);
        }

        private static Rect SanitizeRect(Rect rect)
        {
            float width = Mathf.Max(0.0001f, rect.width);
            float height = Mathf.Max(0.0001f, rect.height);
            return new Rect(rect.x, rect.y, width, height);
        }
    }
}
#endif