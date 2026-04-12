#if false
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ShapeCanvasConstraintUtility
    {
        public static void AssignAnchorsFromCurrentPosition(
            ref CanvasPoint point,
            Rect bounds,
            CanvasAnchorX xAnchor,
            CanvasAnchorY yAnchor)
        {
            point.XAnchor = xAnchor;
            point.YAnchor = yAnchor;
            RecalculateOffsetsFromPosition(ref point, bounds);
        }

        public static void RecalculateOffsetsFromPosition(
            ref CanvasPoint point,
            Rect bounds)
        {
            point.OffsetX = point.XAnchor switch
            {
                CanvasAnchorX.Left => point.Position.x - bounds.xMin,
                CanvasAnchorX.Center => point.Position.x - bounds.center.x,
                CanvasAnchorX.Right => point.Position.x - bounds.xMax,
                _ => point.OffsetX
            };

            point.OffsetY = point.YAnchor switch
            {
                CanvasAnchorY.Bottom => point.Position.y - bounds.yMin,
                CanvasAnchorY.Center => point.Position.y - bounds.center.y,
                CanvasAnchorY.Top => point.Position.y - bounds.yMax,
                _ => point.OffsetY
            };
        }

        public static void SetPointPosition(
            ref CanvasPoint point,
            Vector2 newPosition,
            Rect bounds)
        {
            point.Position = newPosition;
            RecalculateOffsetsFromPosition(ref point, bounds);
        }

        public static void ResolvePointIntoPosition(
            ref CanvasPoint point,
            Rect oldBounds,
            Rect newBounds)
        {
            point.Position = ShapeCanvasPointResolver.ResolvePoint(point, oldBounds, newBounds);
            RecalculateOffsetsFromPosition(ref point, newBounds);
        }

        public static void ResolveAllPointsIntoPosition(
            CanvasPoint[] points,
            Rect oldBounds,
            Rect newBounds)
        {
            if (points == null)
                return;

            for (int i = 0; i < points.Length; i++)
            {
                var point = points[i];
                ResolvePointIntoPosition(ref point, oldBounds, newBounds);
                points[i] = point;
            }
        }
    }
}
#endif
