using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public static class ProfileCanvasPointResolver
    {
        public static Vector2 ResolvePoint(
            ProfilePoint point,
            Rect oldBounds,
            Rect newBounds,
            float oldPaddingGuideX,
            float oldBorderGuideX,
            float newPaddingGuideX,
            float newBorderGuideX)
        {
            return ResolvePoint(point, newBounds, newPaddingGuideX, newBorderGuideX);
        }

        public static Vector2 ResolvePoint(
            ProfilePoint point,
            Rect bounds,
            float paddingGuideX,
            float borderGuideX)
        {
            ProfileCanvasDocument doc = BuildDoc(bounds, paddingGuideX, borderGuideX);
            ProfileSpanLayoutData layout = ProfileSpanLayout.Build(doc);

            FloatSpan xSpan = GetXSpan(point.xRegion, layout);
            FloatSpan zSpan = GetZSpan(point.zRegion, layout);

            float x = xSpan.Evaluate(Clamp01(point.regionLerp.x));
            float y = zSpan.Evaluate(Clamp01(point.regionLerp.y));

            return new Vector2(x, y);
        }

        public static void SetFromPosition(
            ref ProfilePoint point,
            Vector2 position,
            Rect bounds,
            float paddingGuideX,
            float borderGuideX)
        {
            ProfileCanvasDocument doc = BuildDoc(bounds, paddingGuideX, borderGuideX);
            ProfileSpanLayoutData layout = ProfileSpanLayout.Build(doc);

            point.xRegion = DetectXRegion(position.x, layout);
            point.zRegion = DetectZRegion(position.y, layout);

            FloatSpan xSpan = GetXSpan(point.xRegion, layout);
            FloatSpan zSpan = GetZSpan(point.zRegion, layout);

            point.regionLerp = new Vector2(
                Clamp01(xSpan.InverseLerp(position.x)),
                Clamp01(zSpan.InverseLerp(position.y))
            );
        }

        private static ProfileXRegion DetectXRegion(float x, ProfileSpanLayoutData layout)
        {
            if (x <= layout.XPaddingToContent.Max)
                return ProfileXRegion.Inner;

            return ProfileXRegion.Outer;
        }

        private static ProfileZRegion DetectZRegion(float z, ProfileSpanLayoutData layout)
        {
            if (z <= layout.ZPositiveBorderToContent.Max)
                return ProfileZRegion.PositiveOuter;

            if (z <= layout.ZPositiveContentToPadding.Max)
                return ProfileZRegion.PositiveInner;

            if (z <= layout.ZMainDepth.Max)
                return ProfileZRegion.Center;

            if (z <= layout.ZNegativePaddingToContent.Max)
                return ProfileZRegion.NegativeInner;

            return ProfileZRegion.NegativeOuter;
        }

        private static FloatSpan GetXSpan(ProfileXRegion region, ProfileSpanLayoutData layout)
        {
            return region switch
            {
                ProfileXRegion.Inner => layout.XPaddingToContent,
                ProfileXRegion.Outer => layout.XContentToBorder,
                _ => layout.XPaddingToContent,
            };
        }

        private static FloatSpan GetZSpan(ProfileZRegion region, ProfileSpanLayoutData layout)
        {
            return region switch
            {
                ProfileZRegion.PositiveOuter => layout.ZPositiveBorderToContent,
                ProfileZRegion.PositiveInner => layout.ZPositiveContentToPadding,
                ProfileZRegion.Center => layout.ZMainDepth,
                ProfileZRegion.NegativeInner => layout.ZNegativePaddingToContent,
                ProfileZRegion.NegativeOuter => layout.ZNegativeContentToBorder,
                _ => layout.ZMainDepth,
            };
        }

        private static ProfileCanvasDocument BuildDoc(Rect bounds, float paddingGuideX, float borderGuideX)
        {
            ProfileCanvasDocument doc = new ProfileCanvasDocument();
            doc.ResizeWorld(new Vector2(Mathf.Max(0.0001f, bounds.width), Mathf.Max(0.0001f, bounds.height)));

            float borderOnly = Mathf.Max(0f, borderGuideX - paddingGuideX);

            doc.SetGuideValues(
                paddingGuideX,
                paddingGuideX,
                paddingGuideX,
                paddingGuideX,
                borderOnly,
                borderOnly,
                borderOnly,
                borderOnly);

            doc.FrontPaddingDepth = paddingGuideX;
            doc.FrontBorderDepth = borderOnly;

            return doc;
        }

        private static float Clamp01(float value)
        {
            return Mathf.Clamp01(value);
        }
    }

    public readonly struct FloatSpan
    {
        public readonly float Min;
        public readonly float Max;

        public FloatSpan(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Evaluate(float t)
        {
            return Mathf.Lerp(Min, Max, Mathf.Clamp01(t));
        }

        public float InverseLerp(float value)
        {
            float size = Max - Min;
            if (Mathf.Abs(size) < 0.0001f)
                return 0.5f;

            return (value - Min) / size;
        }

        public static implicit operator FloatSpan(ProfileSpan span)
        {
            return new FloatSpan(span.Min, span.Max);
        }
    }
}