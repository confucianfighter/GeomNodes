using UnityEngine;
using DLN;

namespace DLN
{
    [System.Serializable]
    public struct AxisRegionSelection
    {
        public bool negativeBorderEdge;
        public bool negativeContentEdge;
        public bool negativePaddingEdge;

        public bool center;

        public bool positivePaddingEdge;
        public bool positiveContentEdge;
        public bool positiveBorderEdge;

        public float interpMin;
        public float interpMax;

        public static AxisRegionSelection Default => Borders;

        public static AxisRegionSelection AllEdges => new AxisRegionSelection
        {
            negativeBorderEdge = true,
            negativeContentEdge = true,
            negativePaddingEdge = true,

            center = true,

            positivePaddingEdge = true,
            positiveContentEdge = true,
            positiveBorderEdge = true,

            interpMin = 0f,
            interpMax = 1f
        };
        public static AxisRegionSelection Borders => new AxisRegionSelection
        {
            negativeBorderEdge = true,
            negativeContentEdge = false,
            negativePaddingEdge = false,

            center = false,

            positivePaddingEdge = false,
            positiveContentEdge = false,
            positiveBorderEdge = true,

            interpMin = 0f,
            interpMax = 1f
        };
        public static AxisRegionSelection Contents => new AxisRegionSelection
        {
            negativeBorderEdge = false,
            negativeContentEdge = true,
            negativePaddingEdge = false,

            center = false,

            positivePaddingEdge = false,
            positiveContentEdge = true,
            positiveBorderEdge = false,

            interpMin = 0f,
            interpMax = 1f
        };
        public static AxisRegionSelection Padding => new AxisRegionSelection
        {
            negativeBorderEdge = false,
            negativeContentEdge = false,
            negativePaddingEdge = true,

            center = false,

            positivePaddingEdge = true,
            positiveContentEdge = false,
            positiveBorderEdge = false,

            interpMin = 0f,
            interpMax = 1f
        };

        public static bool Validate(AxisRegionSelection axis)
        {
            return
                axis.negativeBorderEdge ||
                axis.negativeContentEdge ||
                axis.negativePaddingEdge ||
                axis.center ||
                axis.positivePaddingEdge ||
                axis.positiveContentEdge ||
                axis.positiveBorderEdge;
        }

        /// <summary>
        /// Computes a min/max interval from the selected edge positions.
        /// axisMin / axisMax are the CONTENTS min/max for the source bounds on this axis.
        /// If exactly one edge is selected, returns a tiny interval centered on that edge.
        /// Padding is clamped inward and contents size is prevented from collapsing below minContentsSize.
        /// </summary>
        public static bool TryGetAxisExtents(
            out float min,
            out float max,
            float axisMin,
            float axisMax,
            AxisRegionSelection selection,
            AxisBordersPadding bordersPadding,
            float singleEdgeEpsilon = 0.0001f)
        {
            min = 0f;
            max = 0f;

            if (!Validate(selection))
                return false;

            bordersPadding = AxisBordersPadding.GetClamped(bordersPadding);

            float rawContentsSize = axisMax - axisMin;
            float clampedContentsSize = Mathf.Max(rawContentsSize, bordersPadding.minContentsSize);

            float centerValue = (axisMin + axisMax) * 0.5f;

            float contentsMin = centerValue - clampedContentsSize * 0.5f;
            float contentsMax = centerValue + clampedContentsSize * 0.5f;

            float maxNegativePadding = Mathf.Max(0f, clampedContentsSize);
            float maxPositivePadding = Mathf.Max(0f, clampedContentsSize);

            float negativePadding = Mathf.Clamp(bordersPadding.negativePadding, 0f, maxNegativePadding);
            float positivePadding = Mathf.Clamp(bordersPadding.positivePadding, 0f, maxPositivePadding);

            float negativeBorder = contentsMin - bordersPadding.negativeBorder;
            float negativeContent = contentsMin;
            float negativePaddingEdgeValue = Mathf.Min(contentsMin + negativePadding, contentsMax);

            float center = (contentsMin + contentsMax) * 0.5f;

            float positivePaddingEdgeValue = Mathf.Max(contentsMax - positivePadding, contentsMin);
            float positiveContent = contentsMax;
            float positiveBorder = contentsMax + bordersPadding.positiveBorder;

            float selectedMin = float.PositiveInfinity;
            float selectedMax = float.NegativeInfinity;
            int count = 0;

            void Include(bool include, float value)
            {
                if (!include) return;

                selectedMin = Mathf.Min(selectedMin, value);
                selectedMax = Mathf.Max(selectedMax, value);
                count++;
            }

            Include(selection.negativeBorderEdge, negativeBorder);
            Include(selection.negativeContentEdge, negativeContent);
            Include(selection.negativePaddingEdge, negativePaddingEdgeValue);
            Include(selection.center, center);
            Include(selection.positivePaddingEdge, positivePaddingEdgeValue);
            Include(selection.positiveContentEdge, positiveContent);
            Include(selection.positiveBorderEdge, positiveBorder);

            if (count == 0)
                return false;

            float minFinalSize = Mathf.Max(singleEdgeEpsilon, bordersPadding.minContentsSize);

            if (count == 1 || Mathf.Approximately(selectedMin, selectedMax))
            {
                float c = selectedMin;
                min = c - minFinalSize * 0.5f;
                max = c + minFinalSize * 0.5f;
                return true;
            }

            float a = Mathf.LerpUnclamped(selectedMin, selectedMax, selection.interpMin);
            float b = Mathf.LerpUnclamped(selectedMin, selectedMax, selection.interpMax);

            min = Mathf.Min(a, b);
            max = Mathf.Max(a, b);

            if ((max - min) < minFinalSize)
            {
                float c = (min + max) * 0.5f;
                min = c - minFinalSize * 0.5f;
                max = c + minFinalSize * 0.5f;
            }

            return true;
        }
    }

    [System.Serializable]
    public struct RegionSelection
    {
        public AxisRegionSelection x;
        public AxisRegionSelection y;
        public AxisRegionSelection z;

        public static RegionSelection Default => new RegionSelection
        {
            x = AxisRegionSelection.Default,
            y = AxisRegionSelection.Default,
            z = AxisRegionSelection.Default
        };

        public static RegionSelection AllEdges => new RegionSelection
        {
            x = AxisRegionSelection.AllEdges,
            y = AxisRegionSelection.AllEdges,
            z = AxisRegionSelection.AllEdges
        };
        public static RegionSelection Contents => new RegionSelection
        {
            x = AxisRegionSelection.Contents,
            y = AxisRegionSelection.Contents,
            z = AxisRegionSelection.Contents
        };

        public static RegionSelection Padding => new RegionSelection
        {
            x = AxisRegionSelection.Padding,
            y = AxisRegionSelection.Padding,
            z = AxisRegionSelection.Padding
        };

        public static RegionSelection Borders => new RegionSelection
        {
            x = AxisRegionSelection.Borders,
            y = AxisRegionSelection.Borders,
            z = AxisRegionSelection.Borders
        };

        public static bool Validate(RegionSelection selection)
        {
            if (AxisRegionSelection.Validate(selection.x) &&
                AxisRegionSelection.Validate(selection.y) &&
                AxisRegionSelection.Validate(selection.z))
            {
                return true;
            }

            Debug.LogError("At least one bool must be checked in each axis region selection.");
            return false;
        }

        public static bool CalculateRegion(
            Bounds sourceBounds,
            RegionSelection selection,
            BordersPadding bordersPadding,
            out Bounds result,
            float singleEdgeEpsilon = 0.0001f)
        {
            result = default;

            if (!Validate(selection))
                return false;

            bordersPadding = BordersPadding.GetClamped(bordersPadding);

            if (!AxisRegionSelection.TryGetAxisExtents(
                    out float minX,
                    out float maxX,
                    axisMin: sourceBounds.min.x,
                    axisMax: sourceBounds.max.x,
                    selection: selection.x,
                    bordersPadding: bordersPadding.x,
                    singleEdgeEpsilon: singleEdgeEpsilon))
            {
                return false;
            }

            if (!AxisRegionSelection.TryGetAxisExtents(
                    out float minY,
                    out float maxY,
                    axisMin: sourceBounds.min.y,
                    axisMax: sourceBounds.max.y,
                    selection: selection.y,
                    bordersPadding: bordersPadding.y,
                    singleEdgeEpsilon: singleEdgeEpsilon))
            {
                return false;
            }

            if (!AxisRegionSelection.TryGetAxisExtents(
                    out float minZ,
                    out float maxZ,
                    axisMin: sourceBounds.min.z,
                    axisMax: sourceBounds.max.z,
                    selection: selection.z,
                    bordersPadding: bordersPadding.z,
                    singleEdgeEpsilon: singleEdgeEpsilon))
            {
                return false;
            }

            Vector3 min = new Vector3(minX, minY, minZ);
            Vector3 max = new Vector3(maxX, maxY, maxZ);

            result = new Bounds();
            result.SetMinMax(min, max);
            return true;
        }

        public static Bounds CalculateRegion(
            Bounds sourceBounds,
            RegionSelection selection,
            BordersPadding bordersPadding,
            float singleEdgeEpsilon = 0.0001f)
        {
            if (!CalculateRegion(sourceBounds, selection, bordersPadding, out var result, singleEdgeEpsilon))
            {
                return new Bounds(sourceBounds.center, Vector3.zero);
            }

            return result;
        }
    }
    [System.Serializable]
    public struct OptionalRegionSelection
    {
        [SerializeField] private bool hasValue;
        [SerializeField] private RegionSelection value;

        public bool HasValue => hasValue;

        public RegionSelection Value
        {
            get
            {
                if (!hasValue)
                    throw new System.InvalidOperationException("OptionalRegionSelection has no value.");
                return value;
            }
            set
            {
                this.value = value;
                hasValue = true;
            }
        }

        public void Set(RegionSelection value)
        {
            this.value = value;
            hasValue = true;
        }

        public void Clear()
        {
            value = default;
            hasValue = false;
        }

        public RegionSelection ValueOr(RegionSelection fallback)
        {
            return hasValue ? value : fallback;
        }

        public static OptionalRegionSelection WithValue(RegionSelection value)
        {
            var result = new OptionalRegionSelection();
            result.Set(value);
            return result;
        }
        public static RegionSelection Contents => new RegionSelection
        {
            x = AxisRegionSelection.Contents,
            y = AxisRegionSelection.Contents,
            z = AxisRegionSelection.Contents
        };

        public static RegionSelection Padding => new RegionSelection
        {
            x = AxisRegionSelection.Padding,
            y = AxisRegionSelection.Padding,
            z = AxisRegionSelection.Padding
        };

        public static RegionSelection Borders => new RegionSelection
        {
            x = AxisRegionSelection.Borders,
            y = AxisRegionSelection.Borders,
            z = AxisRegionSelection.Borders
        };
    }
}