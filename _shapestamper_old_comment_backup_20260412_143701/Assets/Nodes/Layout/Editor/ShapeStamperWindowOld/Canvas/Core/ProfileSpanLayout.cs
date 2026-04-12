using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public readonly struct ProfileSpan
    {
        public readonly float Min;
        public readonly float Max;

        public ProfileSpan(float min, float max)
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
            if (Mathf.Abs(Max - Min) < 0.000001f)
                return 0f;

            return Mathf.Clamp01(Mathf.InverseLerp(Min, Max, value));
        }
    }

    public readonly struct ProfileSpanLayoutData
    {
        public readonly ProfileSpan XPaddingToContent;
        public readonly ProfileSpan XContentToBorder;

        public readonly ProfileSpan ZPositiveBorderToContent;
        public readonly ProfileSpan ZPositiveContentToPadding;
        public readonly ProfileSpan ZMainDepth;
        public readonly ProfileSpan ZNegativePaddingToContent;
        public readonly ProfileSpan ZNegativeContentToBorder;

        public ProfileSpanLayoutData(
            ProfileSpan xPaddingToContent,
            ProfileSpan xContentToBorder,
            ProfileSpan zPositiveBorderToContent,
            ProfileSpan zPositiveContentToPadding,
            ProfileSpan zMainDepth,
            ProfileSpan zNegativePaddingToContent,
            ProfileSpan zNegativeContentToBorder)
        {
            XPaddingToContent = xPaddingToContent;
            XContentToBorder = xContentToBorder;
            ZPositiveBorderToContent = zPositiveBorderToContent;
            ZPositiveContentToPadding = zPositiveContentToPadding;
            ZMainDepth = zMainDepth;
            ZNegativePaddingToContent = zNegativePaddingToContent;
            ZNegativeContentToBorder = zNegativeContentToBorder;
        }

        public ProfileSpan GetXSpan(ProfileXSpan span)
        {
            switch (span)
            {
                case ProfileXSpan.PaddingToContent:
                    return XPaddingToContent;
                case ProfileXSpan.ContentToBorder:
                default:
                    return XContentToBorder;
            }
        }

        public ProfileSpan GetZSpan(ProfileZSpan span)
        {
            switch (span)
            {
                case ProfileZSpan.PositiveBorderToContent:
                    return ZPositiveBorderToContent;
                case ProfileZSpan.PositiveContentToPadding:
                    return ZPositiveContentToPadding;
                case ProfileZSpan.MainDepth:
                    return ZMainDepth;
                case ProfileZSpan.NegativePaddingToContent:
                    return ZNegativePaddingToContent;
                case ProfileZSpan.NegativeContentToBorder:
                default:
                    return ZNegativeContentToBorder;
            }
        }
    }

    public static class ProfileSpanLayout
    {
        public static ProfileSpanLayoutData Build(ProfileCanvasDocument document)
        {
            float xPadding = 0f;
            float xContent = Mathf.Max(xPadding, document.PaddingGuideX);
            float xBorder = Mathf.Max(xContent, document.BorderGuideX);

            float zTop = 0f;
            float zPosBorderToContentEnd = Mathf.Max(zTop, document.TopBorder);
            float zPosContentToPaddingEnd = Mathf.Max(zPosBorderToContentEnd, document.TopBorder + document.TopPadding);

            float zMainStart = zPosContentToPaddingEnd;
            float zMainEnd = Mathf.Max(zMainStart, document.WorldSizeMeters.y - document.BottomBorder - document.BottomPadding);

            float zNegPaddingToContentEnd = Mathf.Max(zMainEnd, document.WorldSizeMeters.y - document.BottomBorder);
            float zNegContentToBorderEnd = Mathf.Max(zNegPaddingToContentEnd, document.WorldSizeMeters.y);

            return new ProfileSpanLayoutData(
                xPaddingToContent: new ProfileSpan(xPadding, xContent),
                xContentToBorder: new ProfileSpan(xContent, xBorder),
                zPositiveBorderToContent: new ProfileSpan(zTop, zPosBorderToContentEnd),
                zPositiveContentToPadding: new ProfileSpan(zPosBorderToContentEnd, zPosContentToPaddingEnd),
                zMainDepth: new ProfileSpan(zMainStart, zMainEnd),
                zNegativePaddingToContent: new ProfileSpan(zMainEnd, zNegPaddingToContentEnd),
                zNegativeContentToBorder: new ProfileSpan(zNegPaddingToContentEnd, zNegContentToBorderEnd));
        }
    }
}
