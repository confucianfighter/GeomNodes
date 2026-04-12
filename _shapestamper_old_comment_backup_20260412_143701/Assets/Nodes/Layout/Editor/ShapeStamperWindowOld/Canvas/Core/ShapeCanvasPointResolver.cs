using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ShapeCanvasPointResolver
    {
        public static Vector2 ResolvePoint(CanvasPoint point, Rect bounds)
        {
            Rect xRegion = GetXRegionRect(point.xRegion, bounds);
            Rect yRegion = GetYRegionRect(point.yRegion, bounds);

            float x = Mathf.Lerp(xRegion.xMin, xRegion.xMax, Clamp01(point.regionLerp.x));
            float y = Mathf.Lerp(yRegion.yMin, yRegion.yMax, Clamp01(point.regionLerp.y));

            return new Vector2(x, y);
        }

        public static Vector2 ResolvePoint(CanvasPoint point, Rect oldBounds, Rect newBounds)
        {
            return ResolvePoint(point, newBounds);
        }

        public static void SetFromPosition(ref CanvasPoint point, Vector2 position, Rect bounds)
        {
            point.xRegion = DetectXRegion(position.x, bounds);
            point.yRegion = DetectYRegion(position.y, bounds);

            Rect xRegion = GetXRegionRect(point.xRegion, bounds);
            Rect yRegion = GetYRegionRect(point.yRegion, bounds);

            point.regionLerp = new Vector2(
                InverseLerpSafe(xRegion.xMin, xRegion.xMax, position.x),
                InverseLerpSafe(yRegion.yMin, yRegion.yMax, position.y)
            );
        }

        public static ShapeRegionX DetectXRegion(float x, Rect bounds)
        {
            float x0 = bounds.xMin;
            float x1 = bounds.xMin + bounds.width / 3f;
            float x2 = bounds.xMin + bounds.width * 2f / 3f;
            float x3 = bounds.xMax;

            if (x <= x1)
                return ShapeRegionX.Negative;

            if (x <= x2)
                return ShapeRegionX.Middle;

            return ShapeRegionX.Positive;
        }

        public static ShapeRegionY DetectYRegion(float y, Rect bounds)
        {
            float y0 = bounds.yMin;
            float y1 = bounds.yMin + bounds.height / 3f;
            float y2 = bounds.yMin + bounds.height * 2f / 3f;
            float y3 = bounds.yMax;

            if (y <= y1)
                return ShapeRegionY.Positive; // top third

            if (y <= y2)
                return ShapeRegionY.Middle;

            return ShapeRegionY.Negative; // bottom third
        }

        public static Rect GetRegionRect(ShapeRegionX xRegion, ShapeRegionY yRegion, Rect bounds)
        {
            Rect xr = GetXRegionRect(xRegion, bounds);
            Rect yr = GetYRegionRect(yRegion, bounds);

            return Rect.MinMaxRect(
                xr.xMin,
                yr.yMin,
                xr.xMax,
                yr.yMax
            );
        }

        private static Rect GetXRegionRect(ShapeRegionX region, Rect bounds)
        {
            float x0 = bounds.xMin;
            float x1 = bounds.xMin + bounds.width / 3f;
            float x2 = bounds.xMin + bounds.width * 2f / 3f;
            float x3 = bounds.xMax;

            return region switch
            {
                ShapeRegionX.Negative => Rect.MinMaxRect(x0, bounds.yMin, x1, bounds.yMax),
                ShapeRegionX.Middle   => Rect.MinMaxRect(x1, bounds.yMin, x2, bounds.yMax),
                ShapeRegionX.Positive => Rect.MinMaxRect(x2, bounds.yMin, x3, bounds.yMax),
                _ => Rect.MinMaxRect(x1, bounds.yMin, x2, bounds.yMax),
            };
        }

        private static Rect GetYRegionRect(ShapeRegionY region, Rect bounds)
        {
            float y0 = bounds.yMin;
            float y1 = bounds.yMin + bounds.height / 3f;
            float y2 = bounds.yMin + bounds.height * 2f / 3f;
            float y3 = bounds.yMax;

            return region switch
            {
                ShapeRegionY.Positive => Rect.MinMaxRect(bounds.xMin, y0, bounds.xMax, y1),
                ShapeRegionY.Middle   => Rect.MinMaxRect(bounds.xMin, y1, bounds.xMax, y2),
                ShapeRegionY.Negative => Rect.MinMaxRect(bounds.xMin, y2, bounds.xMax, y3),
                _ => Rect.MinMaxRect(bounds.xMin, y1, bounds.xMax, y2),
            };
        }

        private static float Clamp01(float value)
        {
            return Mathf.Clamp01(value);
        }

        private static float InverseLerpSafe(float min, float max, float value)
        {
            float size = max - min;
            if (Mathf.Abs(size) < 0.0001f)
                return 0.5f;

            return Mathf.Clamp01((value - min) / size);
        }
    }
}