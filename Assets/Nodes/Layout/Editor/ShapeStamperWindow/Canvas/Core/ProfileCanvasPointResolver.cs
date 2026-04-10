using UnityEngine;

namespace DLN
{
    public static class ProfileCanvasPointResolver
    {
        public static Vector2 ResolvePoint(
            CanvasPoint point,
            Rect oldBounds,
            Rect newBounds,
            float oldPaddingGuideX,
            float oldBorderGuideX,
            float newPaddingGuideX,
            float newBorderGuideX)
        {
            float x = ResolveX(point, oldBounds, newBounds, oldPaddingGuideX, oldBorderGuideX, newPaddingGuideX, newBorderGuideX);
            float y = ResolveY(point, oldBounds, newBounds);
            return new Vector2(x, y);
        }

        public static void RecalculateOffsets(
            ref CanvasPoint point,
            Rect bounds,
            float paddingGuideX,
            float borderGuideX)
        {
            point.OffsetX = CalculateOffsetX(point, bounds, paddingGuideX, borderGuideX);
            point.OffsetY = CalculateOffsetY(point, bounds);
        }

        public static void SetAnchorsPreservePosition(
            ref CanvasPoint point,
            ProfileAnchorX xAnchor,
            CanvasAnchorY yAnchor,
            Rect bounds,
            float paddingGuideX,
            float borderGuideX)
        {
            point.ProfileXAnchor = xAnchor;
            point.YAnchor = yAnchor;
            RecalculateOffsets(ref point, bounds, paddingGuideX, borderGuideX);
        }

        public static void ResizePointPreservingBehavior(
            ref CanvasPoint point,
            Rect oldBounds,
            Rect newBounds,
            float oldPaddingGuideX,
            float oldBorderGuideX,
            float newPaddingGuideX,
            float newBorderGuideX)
        {
            point.Position = ResolvePoint(
                point,
                oldBounds,
                newBounds,
                oldPaddingGuideX,
                oldBorderGuideX,
                newPaddingGuideX,
                newBorderGuideX);

            RecalculateOffsets(ref point, newBounds, newPaddingGuideX, newBorderGuideX);
        }

        public static float ResolveX(
            CanvasPoint point,
            Rect oldBounds,
            Rect newBounds,
            float oldPaddingGuideX,
            float oldBorderGuideX,
            float newPaddingGuideX,
            float newBorderGuideX)
        {
            switch (point.ProfileXAnchor)
            {
                case ProfileAnchorX.Content:
                    return newBounds.xMin + point.OffsetX;

                case ProfileAnchorX.Padding:
                    return newPaddingGuideX + point.OffsetX;

                case ProfileAnchorX.Border:
                    return newBorderGuideX + point.OffsetX;

                case ProfileAnchorX.Floating:
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

        public static float CalculateOffsetX(
            CanvasPoint point,
            Rect bounds,
            float paddingGuideX,
            float borderGuideX)
        {
            switch (point.ProfileXAnchor)
            {
                case ProfileAnchorX.Content:
                    return point.Position.x - bounds.xMin;

                case ProfileAnchorX.Padding:
                    return point.Position.x - paddingGuideX;

                case ProfileAnchorX.Border:
                    return point.Position.x - borderGuideX;

                case ProfileAnchorX.Floating:
                default:
                    return 0f;
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
