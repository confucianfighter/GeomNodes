using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public readonly struct ProfileRegionSpan
    {
        public readonly float Min;
        public readonly float Max;

        public ProfileRegionSpan(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Evaluate(float t)
        {
            t = Mathf.Clamp01(t);
            return Mathf.Lerp(Min, Max, t);
        }

        public float InverseLerp(float value)
        {
            if (Mathf.Abs(Max - Min) < 0.000001f)
                return 0f;

            return Mathf.Clamp01(Mathf.InverseLerp(Min, Max, value));
        }
    }

    public readonly struct ProfileRegionLayoutData
    {
        public readonly ProfileRegionSpan PositiveXPadding;
        public readonly ProfileRegionSpan PositiveXContent;
        public readonly ProfileRegionSpan PositiveXBorder;

        public readonly ProfileRegionSpan PositiveZBorder;
        public readonly ProfileRegionSpan PositiveZContent;
        public readonly ProfileRegionSpan PositiveZPadding;
        public readonly ProfileRegionSpan MainDepth;
        public readonly ProfileRegionSpan NegativeZPadding;
        public readonly ProfileRegionSpan NegativeZContent;
        public readonly ProfileRegionSpan NegativeZBorder;

        public ProfileRegionLayoutData(
            ProfileRegionSpan positiveXPadding,
            ProfileRegionSpan positiveXContent,
            ProfileRegionSpan positiveXBorder,
            ProfileRegionSpan positiveZBorder,
            ProfileRegionSpan positiveZContent,
            ProfileRegionSpan positiveZPadding,
            ProfileRegionSpan mainDepth,
            ProfileRegionSpan negativeZPadding,
            ProfileRegionSpan negativeZContent,
            ProfileRegionSpan negativeZBorder)
        {
            PositiveXPadding = positiveXPadding;
            PositiveXContent = positiveXContent;
            PositiveXBorder = positiveXBorder;
            PositiveZBorder = positiveZBorder;
            PositiveZContent = positiveZContent;
            PositiveZPadding = positiveZPadding;
            MainDepth = mainDepth;
            NegativeZPadding = negativeZPadding;
            NegativeZContent = negativeZContent;
            NegativeZBorder = negativeZBorder;
        }

        public ProfileRegionSpan GetXSpan(ProfileXRegion region)
        {
            switch (region)
            {
                case ProfileXRegion.PositivePadding: return PositiveXPadding;
                case ProfileXRegion.PositiveContent: return PositiveXContent;
                case ProfileXRegion.PositiveBorder: return PositiveXBorder;
                default: return PositiveXContent;
            }
        }

        public ProfileRegionSpan GetZSpan(ProfileZRegion region)
        {
            switch (region)
            {
                case ProfileZRegion.PositiveBorder: return PositiveZBorder;
                case ProfileZRegion.PositiveContent: return PositiveZContent;
                case ProfileZRegion.PositivePadding: return PositiveZPadding;
                case ProfileZRegion.MainDepth: return MainDepth;
                case ProfileZRegion.NegativePadding: return NegativeZPadding;
                case ProfileZRegion.NegativeContent: return NegativeZContent;
                case ProfileZRegion.NegativeBorder: return NegativeZBorder;
                default: return MainDepth;
            }
        }
    }

    public static class ProfileRegionLayout
    {
        public static ProfileRegionLayoutData Build(ProfileCanvasDocument document)
        {
            float x0 = 0f;
            float x1 = Mathf.Max(x0, document.PaddingGuideX);
            float x2 = Mathf.Max(x1, document.BorderGuideX);

            float z0 = 0f;
            float z1 = Mathf.Max(z0, document.TopBorder);
            float z2 = Mathf.Max(z1, document.TopBorder + document.TopPadding);
            float z3 = Mathf.Max(z2, document.TopBorder + document.TopPadding + document.FrontPaddingDepth);

            float totalHeight = Mathf.Max(z3, document.WorldSizeMeters.y);

            float bottomPaddingStart = Mathf.Max(z3, totalHeight - document.BottomBorder - document.BottomPadding - document.FrontPaddingDepth);
            float bottomContentStart = Mathf.Max(bottomPaddingStart, totalHeight - document.BottomBorder - document.BottomPadding);
            float bottomBorderStart = Mathf.Max(bottomContentStart, totalHeight - document.BottomBorder);

            return new ProfileRegionLayoutData(
                positiveXPadding: new ProfileRegionSpan(x0, x1),
                positiveXContent: new ProfileRegionSpan(x1, x2),
                positiveXBorder: new ProfileRegionSpan(x2, x2),

                positiveZBorder: new ProfileRegionSpan(z0, z1),
                positiveZContent: new ProfileRegionSpan(z1, z2),
                positiveZPadding: new ProfileRegionSpan(z2, z3),
                mainDepth: new ProfileRegionSpan(z3, bottomPaddingStart),
                negativeZPadding: new ProfileRegionSpan(bottomPaddingStart, bottomContentStart),
                negativeZContent: new ProfileRegionSpan(bottomContentStart, bottomBorderStart),
                negativeZBorder: new ProfileRegionSpan(bottomBorderStart, totalHeight));
        }
    }
}
