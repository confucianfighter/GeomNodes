#if UNITY_EDITOR
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class CanvasMath
    {
        public static Vector2 ScreenToCanvas(Vector2 screenPosition, Rect canvasRect, CanvasViewState view)
        {
            return (screenPosition - canvasRect.position - view.Pan) / Mathf.Max(0.0001f, view.Zoom);
        }

        public static Vector2 CanvasToScreen(Vector2 canvasPosition, Rect canvasRect, CanvasViewState view)
        {
            return canvasRect.position + view.Pan + canvasPosition * view.Zoom;
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

            a = CanvasToScreen(canvasA, canvasRect, view);
            b = CanvasToScreen(canvasB, canvasRect, view);
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

            handleScreenPosition = CanvasToScreen(handleCanvas, canvasRect, view);
            return true;
        }

        public static bool TryGetPoint(ICanvasDocument document, int pointId, out CanvasPoint point)
        {
            foreach (var p in document.Points)
            {
                if (p.Id == pointId)
                {
                    point = p;
                    return true;
                }
            }

            point = default;
            return false;
        }

        public static bool TryGetEdgeById(ICanvasDocument document, int edgeId, out CanvasEdge edge)
        {
            foreach (var e in document.Edges)
            {
                if (e.Id == edgeId)
                {
                    edge = e;
                    return true;
                }
            }

            edge = default;
            return false;
        }
    }
}
#endif