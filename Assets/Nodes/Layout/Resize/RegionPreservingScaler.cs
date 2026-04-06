using System;
using Microsoft.VisualBasic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using DLN;

namespace DLN
{
    [System.Serializable]
    public struct PreserveRegions
    {
        public SideBand x;
        public SideBand y;
        public SideBand z;
        public static PreserveRegions Init(float regionSizeFrom0To1, float desiredSize) => new PreserveRegions
        {
            x = SideBand.Init(regionSizeFrom0To1: regionSizeFrom0To1, desiredScale: desiredSize),
            y = SideBand.Init(regionSizeFrom0To1: regionSizeFrom0To1, desiredScale: desiredSize),
            z = SideBand.Init(regionSizeFrom0To1: regionSizeFrom0To1, desiredScale: desiredSize)
        };
    }
    [System.Serializable]
    public struct OptionalPreserveRegions
    {
        public PreserveRegions value;
        public bool userOptedIn;

        public PreserveRegions Value => value;
        public static OptionalPreserveRegions Init(float regionSizeFrom0To1, float desiredSize) => new OptionalPreserveRegions
        {
            value = PreserveRegions.Init(regionSizeFrom0To1: regionSizeFrom0To1, desiredSize: desiredSize),
            userOptedIn = false
        };
    }
    [System.Serializable]
    public struct SideBand
    {
        public Band negEdge;
        public Band center;
        public Band posEdge;

        public static SideBand Init(float regionSizeFrom0To1, float desiredScale) =>
            new SideBand
            {
                negEdge = Band.Init(regionSize: regionSizeFrom0To1, desiredScale: desiredScale),
                center = Band.Init(regionSize: regionSizeFrom0To1, desiredScale: desiredScale),
                posEdge = Band.Init(regionSize: regionSizeFrom0To1, desiredScale: desiredScale),

            };



    }
    [System.Serializable]
    public struct Band
    {
        public float regionSizeFrom0To1;
        public float desiredSize;
        public static Band Init(float regionSize, float desiredScale) =>
            new Band { regionSizeFrom0To1 = regionSize, desiredSize = desiredScale };
    }
    public static class MeshResizer
    {
        public static bool ResizeNormal(
            GameObject target,
    Vector3 size,
    Vector3 pivot,
    Allocator tempAllocator = Allocator.Temp
)
        {
            var meshCache = target.AddIfNotExists<MeshCache>();
            var points = meshCache.GetOriginalVerts().ToNativeFloat3Array();

            if (!points.IsCreated || points.Length == 0)
                return false;

            // Compute current bounds from points
            float3 min = points[0];
            float3 max = points[0];

            for (int i = 1; i < points.Length; i++)
            {
                float3 p = points[i];
                min = math.min(min, p);
                max = math.max(max, p);
            }

            float3 currentSize = max - min;
            float3 desiredSize = (float3)size;
            float3 pivot3 = (float3)pivot;

            const float epsilon = 1e-6f;

            // If an axis has no size, we cannot recover meaningful scaling on that axis.
            if ((currentSize.x < epsilon && math.abs(desiredSize.x) > epsilon) ||
                (currentSize.y < epsilon && math.abs(desiredSize.y) > epsilon) ||
                (currentSize.z < epsilon && math.abs(desiredSize.z) > epsilon))
            {
                return false;
            }

            float3 scale = new float3(
                currentSize.x < epsilon ? 1f : desiredSize.x / currentSize.x,
                currentSize.y < epsilon ? 1f : desiredSize.y / currentSize.y,
                currentSize.z < epsilon ? 1f : desiredSize.z / currentSize.z
            );

            for (int i = 0; i < points.Length; i++)
            {
                float3 offset = points[i] - pivot3;
                offset *= scale;
                points[i] = pivot3 + offset;
            }
            meshCache.WriteVerts(points.ToVec());
            points.Dispose();


            return true;
        }
        public static void ResizeAndPreserveRegions(GameObject target, Vector3 size, Vector3 pivot, PreserveRegions preserveRegions)
        {

            var meshCache = target.AddIfNotExists<MeshCache>();
            var points = meshCache.GetOriginalVerts().ToNativeFloat3Array();

            ResizeWithIndependentRegions(
                    ref points,
                    preserveRegions: preserveRegions,

                    targetSize: size,
                    pivot: pivot);

            meshCache.WriteVerts(points.ToVec());
            points.Dispose();

        }

        private enum BandMembership
        {
            left,
            leftFill,
            center,
            rightFill,
            right
        }

        public struct AxisBounds
        {
            public float min;
            public float max;
            public float size;
            public float center;
        }
        public static bool ResizeWithIndependentRegions(
            ref NativeArray<float3> points,
            PreserveRegions preserveRegions,
            Vector3 targetSize,
            Vector3 pivot,
            Allocator tempAllocator = Allocator.Temp
        )
        {
            var xValues = points.xValues();
            var yValues = points.yValues();
            var zValues = points.zValues();

            ResizeAxisWithIndependentRegions(
                    ref xValues,
                    negBand: preserveRegions.x.negEdge,
                    centerBand: preserveRegions.x.center,
                    posBand: preserveRegions.x.posEdge,

                    targetSize: targetSize.x,
                    percentPivot: pivot.x);

            ResizeAxisWithIndependentRegions(
                ref yValues,
                negBand: preserveRegions.y.negEdge,
                centerBand: preserveRegions.y.center,
                posBand: preserveRegions.y.posEdge,

                targetSize: targetSize.y,
                percentPivot: pivot.y);

            ResizeAxisWithIndependentRegions(
                ref zValues,
                negBand: preserveRegions.z.negEdge,
                centerBand: preserveRegions.z.center,
                posBand: preserveRegions.z.posEdge,

                targetSize: targetSize.z,
                percentPivot: pivot.z);

            points.SetXYZ(xValues, yValues, zValues);

            xValues.Dispose();
            yValues.Dispose();
            zValues.Dispose();

            return true;


        }

        // my own attempt at this
        public static bool ResizeAxisWithIndependentRegions(
            ref NativeArray<float> points,
            Band negBand,
            Band centerBand,
            Band posBand,

            float targetSize,
            float percentPivot = 0.5f,
            Allocator tempAllocator = Allocator.Temp
        )
        {
            Debug.Log($"Resizing Axis to {targetSize}");
            if (points.Length == 0) return false;
            negBand.Sanitize();
            centerBand.Sanitize();
            posBand.Sanitize();
            targetSize = math.abs(targetSize);

            GetMinMax(values: points, out var srcMin, out var srcMax);

            var srcBounds = CreateBounds(min: srcMin, max: srcMax);
            // shift so start is zero

            if (srcBounds.size < Constants.Epsilon)
            {
                Debug.LogError("Trying to scale something with no size");
                return false;
            }
            var dstBounds = CreateBounds(min: 0, max: targetSize);
            var pivotSize = dstBounds.size * percentPivot;
            dstBounds.Shift(-pivotSize);

            // step one make array of interval membership indices same length as num points
            var bandMembership = new NativeArray<BandMembership>(points.Length, tempAllocator);

            var srcLeftBounds = CreateBounds(
                min: srcBounds.min,
                max: srcBounds.min + srcBounds.size * negBand.regionSizeFrom0To1);
            var srcCenterBounds = CreateBounds(
                min: srcBounds.center - srcBounds.size * centerBand.regionSizeFrom0To1 / 2f,
                max: srcBounds.center + srcBounds.size * centerBand.regionSizeFrom0To1 / 2f);
            var srcRightBounds = CreateBounds(
                min: srcBounds.max - srcBounds.size * posBand.regionSizeFrom0To1,
                max: srcBounds.max);

            var srcLeftFillBounds = CreateBounds(
                min: srcLeftBounds.max,
                max: srcCenterBounds.min);
            var srcRightFillBounds = CreateBounds(
                 min: srcCenterBounds.max,
                 max: srcRightBounds.min);
            // for now just throw warning if initial bands overlap

            // assign band memberships
            for (int i = 0; i < points.Length; i++)
            {
                if (points[i] <= srcLeftBounds.max) bandMembership[i] = BandMembership.left;
                else if (points[i] <= srcLeftFillBounds.max) bandMembership[i] = BandMembership.leftFill;
                else if (points[i] <= srcCenterBounds.max) bandMembership[i] = BandMembership.center;
                else if (points[i] <= srcRightFillBounds.max) bandMembership[i] = BandMembership.rightFill;
                else bandMembership[i] = BandMembership.right;
            }
            // get scaled intervals
            var dstLeftBounds = CreateBounds(
                min: dstBounds.min,
                max: dstBounds.min + negBand.desiredSize);


            var dstCenterBounds = CreateBounds(
                min: dstBounds.center - centerBand.desiredSize / 2,
                max: dstBounds.center + centerBand.desiredSize / 2);

            var dstRightBounds = CreateBounds(
                min: dstBounds.max - posBand.desiredSize,
                max: dstBounds.max);

            AdjustBounds(ref dstLeftBounds, ref dstCenterBounds, ref dstRightBounds);

            var dstLeftFillBounds = CreateBounds(
                min: dstLeftBounds.max,
                max: dstCenterBounds.min
            );
            var dstRightFillBounds = CreateBounds(
                min: dstCenterBounds.max,
                max: dstRightBounds.min
            );



            for (int i = 0; i < points.Length; i++)
            {
                switch (bandMembership[i])
                {
                    case BandMembership.left:
                        points[i] = Remap(points[i], srcLeftBounds, dstLeftBounds);
                        break;
                    case BandMembership.leftFill:
                        points[i] = Remap(points[i], srcLeftFillBounds, dstLeftFillBounds);
                        break;
                    case BandMembership.center:
                        points[i] = Remap(points[i], srcCenterBounds, dstCenterBounds);

                        break;
                    case BandMembership.rightFill:
                        points[i] = Remap(points[i], srcRightFillBounds, dstRightFillBounds);
                        break;
                    case BandMembership.right:
                        points[i] = Remap(points[i], srcRightBounds, dstRightBounds);
                        break;
                }
            }
            GetMinMax(values: points, out var newMin, out var newMax);
            Debug.Log($"new min/max is: {newMin}, {newMax}");

            var intermediateBounds = CreateBounds(dstLeftBounds.min, dstRightBounds.max);

            if (math.abs(intermediateBounds.min - dstBounds.min) > Constants.Epsilon ||
                math.abs(intermediateBounds.max - dstBounds.max) > Constants.Epsilon)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i] = Remap(points[i], intermediateBounds, dstBounds);
                }
            }

            bandMembership.Dispose();
            return true;

        }
        public static bool AdjustBounds(ref AxisBounds left, ref AxisBounds center, ref AxisBounds right)
        {
            bool wasOverlapping = false;
            if (areBandsOverlapping(
                left: left,
                right: right,
                center: center

            ))
            {
                wasOverlapping = true;
                var leftGap = center.min - left.max;
                if (leftGap <= 0)
                {
                    center.Shift(-leftGap + Constants.Epsilon);
                }
                var rightGap = right.min - center.max;
                if (rightGap <= 0)
                {
                    right.Shift(-rightGap + Constants.Epsilon);
                }
            }
            return wasOverlapping;
        }

        private static void Shift(this ref AxisBounds bounds, float amount)
        {
            bounds.min += amount;
            bounds.center += amount;
            bounds.max += amount;
        }
        private static void Sanitize(this ref Band band)
        {
            band.regionSizeFrom0To1 = math.abs(band.regionSizeFrom0To1);
            band.desiredSize = math.abs(band.desiredSize);
        }
        // may not be used
        private static AxisBounds Mul(ref this AxisBounds bounds, float factor)
        {
            return CreateBounds(min: bounds.min * factor, max: bounds.max * factor);
        }

        private static AxisBounds CreateBounds(float min, float max)
        {
            return new AxisBounds
            {
                min = min,
                max = max,
                size = max - min,
                center = max - (max - min) / 2
            };
        }

        private static float Remap(float value, AxisBounds source, AxisBounds dst)
        {
            if (source.size < Constants.Epsilon) return dst.center;
            return math.remap(
                srcStart: source.min,
                srcEnd: source.max,
                dstStart: dst.min,
                dstEnd: dst.max,
                x: value
            );
        }
        private static bool areBandsOverlapping(AxisBounds left, AxisBounds center, AxisBounds right)
        {
            bool result = false;
            if (left.max > center.min || left.max > right.min) result = true;
            else if (center.max > right.min) result = true;
            return result;
        }
        public static void GetMinMax(NativeArray<float> values, out float min, out float max)
        {
            min = values[0];
            max = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                float p = values[i];
                if (p < min) min = p;
                if (p > max) max = p;
            }
        }
    }
}