using UnityEngine;

namespace DLN
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
            ProfileCanvasDocument newDoc = BuildDoc(newBounds, newPaddingGuideX, newBorderGuideX);
            ProfileSpanLayoutData newLayout = ProfileSpanLayout.Build(newDoc);

            float x = newLayout.GetXSpan(point.XSpan).Evaluate(point.XT);
            float y = ResolveY(point, newBounds, newLayout);

            return new Vector2(x, y);
        }

        public static void ResizePointPreservingBehavior(
            ref ProfilePoint point,
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
        }

        public static void SetSpansFromPosition(
            ref ProfilePoint point,
            Rect bounds,
            float paddingGuideX,
            float borderGuideX)
        {
            ProfileCanvasDocument doc = BuildDoc(bounds, paddingGuideX, borderGuideX);
            ProfileSpanLayoutData layout = ProfileSpanLayout.Build(doc);

            point.XSpan = DetectXSpan(point.Position.x, layout);
            point.ZSpan = DetectZSpan(point.Position.y, layout);

            point.XT = layout.GetXSpan(point.XSpan).InverseLerp(point.Position.x);
            point.ZT = layout.GetZSpan(point.ZSpan).InverseLerp(point.Position.y);
        }

        private static float ResolveY(ProfilePoint point, Rect bounds, ProfileSpanLayoutData layout)
        {
            if (point.YAnchor != CanvasAnchorY.Floating)
            {
                switch (point.YAnchor)
                {
                    case CanvasAnchorY.Top:
                        return bounds.yMin + point.OffsetY;
                    case CanvasAnchorY.Bottom:
                        return bounds.yMax + point.OffsetY;
                    case CanvasAnchorY.Center:
                        return bounds.center.y + point.OffsetY;
                }
            }

            return layout.GetZSpan(point.ZSpan).Evaluate(point.ZT);
        }

        private static ProfileXSpan DetectXSpan(float x, ProfileSpanLayoutData layout)
        {
            if (x <= layout.XPaddingToContent.Max)
                return ProfileXSpan.PaddingToContent;

            return ProfileXSpan.ContentToBorder;
        }

        private static ProfileZSpan DetectZSpan(float z, ProfileSpanLayoutData layout)
        {
            if (z <= layout.ZPositiveBorderToContent.Max)
                return ProfileZSpan.PositiveBorderToContent;
            if (z <= layout.ZPositiveContentToPadding.Max)
                return ProfileZSpan.PositiveContentToPadding;
            if (z <= layout.ZMainDepth.Max)
                return ProfileZSpan.MainDepth;
            if (z <= layout.ZNegativePaddingToContent.Max)
                return ProfileZSpan.NegativePaddingToContent;

            return ProfileZSpan.NegativeContentToBorder;
        }

        private static ProfileCanvasDocument BuildDoc(Rect bounds, float paddingGuideX, float borderGuideX)
        {
            ProfileCanvasDocument doc = new ProfileCanvasDocument();
            doc.ResizeWorld(new Vector2(Mathf.Max(0.0001f, bounds.width), Mathf.Max(0.0001f, bounds.height)));

            float borderOnly = Mathf.Max(0f, borderGuideX - paddingGuideX);

            doc.SetGuideValues(
                leftPadding: paddingGuideX,
                rightPadding: paddingGuideX,
                topPadding: paddingGuideX,
                bottomPadding: paddingGuideX,
                leftBorder: borderOnly,
                rightBorder: borderOnly,
                topBorder: borderOnly,
                bottomBorder: borderOnly);

            doc.FrontPaddingDepth = paddingGuideX;
            doc.FrontBorderDepth = borderOnly;

            return doc;
        }
    }
}
