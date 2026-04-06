using UnityEngine;

namespace DLN
{
    [System.Serializable]
    public struct AxisBordersPadding
    {
        public float negativeBorder;
        public float negativePadding;
        public float minContentsSize;
        public float positivePadding;
        public float positiveBorder;

        // Small positive minimum to avoid degenerate / inverted results.
        public const float DefaultMinContentsSize = 0.0001f;

        public static AxisBordersPadding Default => new AxisBordersPadding
        {
            negativeBorder = 0f,
            negativePadding = 0f,
            minContentsSize = DefaultMinContentsSize,
            positivePadding = 0f,
            positiveBorder = 0f
        };

        public static AxisBordersPadding Zero => Default;

        public AxisBordersPadding(
            float negativeBorder,
            float negativePadding,
            float minContentsSize,
            float positivePadding,
            float positiveBorder)
        {
            this.negativeBorder = negativeBorder;
            this.negativePadding = negativePadding;
            this.minContentsSize = Mathf.Max(0f, minContentsSize);
            this.positivePadding = positivePadding;
            this.positiveBorder = positiveBorder;
        }

        public void ClampToValid()
        {
            negativeBorder = Mathf.Max(0f, negativeBorder);
            negativePadding = Mathf.Max(0f, negativePadding);
            minContentsSize = Mathf.Max(0f, minContentsSize);
            positivePadding = Mathf.Max(0f, positivePadding);
            positiveBorder = Mathf.Max(0f, positiveBorder);
        }

        public static AxisBordersPadding GetClamped(AxisBordersPadding value)
        {
            value.ClampToValid();
            return value;
        }

        public static bool Validate(AxisBordersPadding value)
        {
            return
                value.negativeBorder >= 0f &&
                value.negativePadding >= 0f &&
                value.minContentsSize >= 0f &&
                value.positivePadding >= 0f &&
                value.positiveBorder >= 0f;
        }
    }

    [System.Serializable]
    public struct BordersPadding
    {
        public AxisBordersPadding x;
        public AxisBordersPadding y;
        public AxisBordersPadding z;

        public static BordersPadding Default => new BordersPadding
        {
            x = AxisBordersPadding.Default,
            y = AxisBordersPadding.Default,
            z = AxisBordersPadding.Default
        };

        public static BordersPadding Zero => Default;

        public BordersPadding(
            AxisBordersPadding x,
            AxisBordersPadding y,
            AxisBordersPadding z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void ClampToValid()
        {
            x.ClampToValid();
            y.ClampToValid();
            z.ClampToValid();
        }

        public static BordersPadding GetClamped(BordersPadding value)
        {
            value.ClampToValid();
            return value;
        }

        public static bool Validate(BordersPadding value)
        {
            return
                AxisBordersPadding.Validate(value.x) &&
                AxisBordersPadding.Validate(value.y) &&
                AxisBordersPadding.Validate(value.z);
        }
    }
}