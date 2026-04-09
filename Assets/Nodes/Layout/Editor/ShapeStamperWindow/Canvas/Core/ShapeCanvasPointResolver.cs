using UnityEngine;

namespace DLN
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

                case CanvasAnchorX.None:
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
                    return newBounds.yMin + point.OffsetY;

                case CanvasAnchorY.Center:
                    return newBounds.center.y + point.OffsetY;

                case CanvasAnchorY.Top:
                    return newBounds.yMax + point.OffsetY;

                case CanvasAnchorY.None:
                default:
                    return RemapPreservingRatio(
                        point.Position.y,
                        oldBounds.yMin,
                        oldBounds.yMax,
                        newBounds.yMin,
                        newBounds.yMax);
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
