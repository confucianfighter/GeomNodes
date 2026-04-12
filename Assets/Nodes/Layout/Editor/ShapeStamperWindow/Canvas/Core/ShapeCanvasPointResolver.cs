using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ShapeCanvasPointResolver
    {
        public static Vector2 ResolvePoint(
            CanvasPoint point,
            Rect oldBounds,
            Rect newBounds)
        {
            float x = ResolveX(point, oldBounds, newBounds);
            float y = ResolveY(point, oldBounds, newBounds);
            return new Vector2(x, y);
        }

        public static void RecalculateOffsets(ref CanvasPoint point, Rect bounds)
        {
            point.OffsetX = CalculateOffsetX(point, bounds);
            point.OffsetY = CalculateOffsetY(point, bounds);
        }

        public static void SetAnchorsPreservePosition(
            ref CanvasPoint point,
            CanvasAnchorX xAnchor,
            CanvasAnchorY yAnchor,
            Rect bounds)
        {
            point.XAnchor = xAnchor;
            point.YAnchor = yAnchor;
            RecalculateOffsets(ref point, bounds);
        }

        public static void ResizePointPreservingBehavior(
            ref CanvasPoint point,
            Rect oldBounds,
            Rect newBounds)
        {
            point.Position = ResolvePoint(point, oldBounds, newBounds);
            RecalculateOffsets(ref point, newBounds);
        }

        public static float ResolveX(CanvasPoint point, Rect oldBounds, Rect newBounds)
        {
            switch (point.XAnchor)
            {
                case CanvasAnchorX.Left:
                    return newBounds.xMin + point.OffsetX;

                case CanvasAnchorX.Center:
                    return newBounds.center.x + point.OffsetX;

                case CanvasAnchorX.Right:
                    return newBounds.xMax + point.OffsetX;

                case CanvasAnchorX.Floating:
                default:
                    return RemapPreservingRatio(
                        point.Position.x,
                        oldBounds.xMin,
                        oldBounds.xMax,
                        newBounds.xMin,
                        newBounds.xMax);
            }
        }

        public static float ResolveY(CanvasPoint point, Rect oldBounds, Rect newBounds)
        {
            switch (point.YAnchor)
            {
                case CanvasAnchorY.Bottom:
                    return newBounds.yMax + point.OffsetY;

                case CanvasAnchorY.Center:
                    return newBounds.center.y + point.OffsetY;

                case CanvasAnchorY.Top:
                    return newBounds.yMin + point.OffsetY;

                case CanvasAnchorY.Floating:
                default:
                    return RemapPreservingRatio(
                        point.Position.y,
                        oldBounds.yMin,
                        oldBounds.yMax,
                        newBounds.yMin,
                        newBounds.yMax);
            }
        }

        public static float CalculateOffsetY(CanvasPoint point, Rect bounds)
        {
            switch (point.YAnchor)
            {
                case CanvasAnchorY.Bottom:
                    return point.Position.y - bounds.yMax;

                case CanvasAnchorY.Center:
                    return point.Position.y - bounds.center.y;

                case CanvasAnchorY.Top:
                    return point.Position.y - bounds.yMin;

                case CanvasAnchorY.Floating:
                default:
                    return 0f;
            }
        }
        public static float CalculateOffsetX(CanvasPoint point, Rect bounds)
        {
            switch (point.XAnchor)
            {
                case CanvasAnchorX.Left:
                    return point.Position.x - bounds.xMin;

                case CanvasAnchorX.Center:
                    return point.Position.x - bounds.center.x;

                case CanvasAnchorX.Right:
                    return point.Position.x - bounds.xMax;

                case CanvasAnchorX.Floating:
                default:
                    return 0f;
            }
        }



        private static float RemapPreservingRatio(
            float value,
            float oldMin,
            float oldMax,
            float newMin,
            float newMax)
        {
            float oldSize = oldMax - oldMin;
            if (Mathf.Abs(oldSize) < 0.0001f)
                return newMin;

            float t = (value - oldMin) / oldSize;
            return Mathf.Lerp(newMin, newMax, t);
        }
    }
}
