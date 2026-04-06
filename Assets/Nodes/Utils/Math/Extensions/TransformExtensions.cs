using UnityEngine;

namespace DLN
{
    public static class TransformScaleExtensions
    {
        /// <summary>
        /// Sets this transform's localScale, optionally compensating direct children so their
        /// world-space scale and/or position are preserved.
        ///
        /// preserveChildScales:
        ///     true  -> children are counter-scaled so they keep their previous world scale
        ///     false -> children inherit the parent's new scale normally
        ///
        /// preserveChildPositions:
        ///     true  -> children are counter-moved so they keep their previous world position
        ///     false -> children "explode" outward/inward with the parent scale
        ///
        /// uniformScaleChildren:
        ///     Only relevant when preserveChildScales is true.
        ///     true  -> children receive a correction that results in a UNIFORM world-scale change
        ///              instead of per-axis preservation.
        ///     false -> children preserve their exact previous world scale per axis.
        ///
        /// Returns false if the parent's previous scale was too close to zero on any axis,
        /// meaning exact preservation could not be computed safely.
        /// </summary>
        public static bool SetScale(
            this Transform tx,
            Vector3 newLocalScale,
            bool preserveChildScales,
            bool preserveChildPositions,
            bool uniformScaleChildren,
            float epsilon = 0.000001f)
        {
            if (tx == null)
                return false;

            Vector3 oldLocalScale = tx.localScale;

            if (Approximately(oldLocalScale, newLocalScale, epsilon))
                return true;

            bool validX = Mathf.Abs(oldLocalScale.x) > epsilon;
            bool validY = Mathf.Abs(oldLocalScale.y) > epsilon;
            bool validZ = Mathf.Abs(oldLocalScale.z) > epsilon;
            bool fullyRecoverable = validX && validY && validZ;

            // Scale delta: new / old
            Vector3 scaleDelta = new Vector3(
                SafeDivide(newLocalScale.x, oldLocalScale.x, 1f, epsilon),
                SafeDivide(newLocalScale.y, oldLocalScale.y, 1f, epsilon),
                SafeDivide(newLocalScale.z, oldLocalScale.z, 1f, epsilon)
            );

            // Inverse delta used to preserve child world transforms.
            Vector3 inverseDelta = new Vector3(
                SafeDivide(1f, scaleDelta.x, 1f, epsilon),
                SafeDivide(1f, scaleDelta.y, 1f, epsilon),
                SafeDivide(1f, scaleDelta.z, 1f, epsilon)
            );

            // Optional "uniformized" child scale correction.
            // This makes the child's resulting world-scale change uniform rather than per-axis.
            Vector3 childScaleCorrection = inverseDelta;

            if (preserveChildScales && uniformScaleChildren)
            {
                float uniform = GeometricMeanAbs(scaleDelta);
                childScaleCorrection = new Vector3(
                    SafeDivide(uniform, scaleDelta.x, 1f, epsilon),
                    SafeDivide(uniform, scaleDelta.y, 1f, epsilon),
                    SafeDivide(uniform, scaleDelta.z, 1f, epsilon)
                );
            }

            int childCount = tx.childCount;

            // Cache what we need before changing parent scale.
            // No heap allocations here; just a single pass after parent scale change.
            if (preserveChildScales || preserveChildPositions)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = tx.GetChild(i);

                    Vector3 childLocalPosition = child.localPosition;
                    Vector3 childLocalScale = child.localScale;

                    // Apply parent scale.
                    // We do it once, on the first child iteration? No:
                    // better to apply before loop if there are children,
                    // but we still need old child values first? No, child local values do not change
                    // when parent localScale changes, so it's safe to set parent first.
                }
            }

            // Set parent scale.
            tx.localScale = newLocalScale;

            if (preserveChildScales || preserveChildPositions)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Transform child = tx.GetChild(i);

                    if (preserveChildPositions)
                    {
                        child.localPosition = Vector3.Scale(child.localPosition, inverseDelta);
                    }

                    if (preserveChildScales)
                    {
                        child.localScale = Vector3.Scale(child.localScale, childScaleCorrection);
                    }
                }
            }

            return fullyRecoverable;
        }

        /// <summary>
        /// Convenience overload for uniform parent scale.
        /// </summary>
        public static bool SetScale(
            this Transform tx,
            float uniformLocalScale,
            bool preserveChildScales,
            bool preserveChildPositions,
            bool uniformScaleChildren,
            float epsilon = 0.000001f)
        {
            return tx.SetScale(
                new Vector3(uniformLocalScale, uniformLocalScale, uniformLocalScale),
                preserveChildScales,
                preserveChildPositions,
                uniformScaleChildren,
                epsilon
            );
        }

        private static float SafeDivide(float numerator, float denominator, float fallback, float epsilon)
        {
            return Mathf.Abs(denominator) > epsilon ? numerator / denominator : fallback;
        }

        private static bool Approximately(Vector3 a, Vector3 b, float epsilon)
        {
            return Mathf.Abs(a.x - b.x) <= epsilon
                && Mathf.Abs(a.y - b.y) <= epsilon
                && Mathf.Abs(a.z - b.z) <= epsilon;
        }

        private static float GeometricMeanAbs(Vector3 v)
        {
            float ax = Mathf.Max(Mathf.Abs(v.x), 0.000001f);
            float ay = Mathf.Max(Mathf.Abs(v.y), 0.000001f);
            float az = Mathf.Max(Mathf.Abs(v.z), 0.000001f);
            return Mathf.Pow(ax * ay * az, 1f / 3f);
        }

        public static bool TryGetSpan(
            this Transform tx,
            Vector3 refPosition,
            Quaternion refRotation,
            Vector3 refScale,
            Vector3 interpBoundsStartPoint,
            Vector3 direction,
            out Bnds.SpanFromPoint span)
        {
            span = default;

            if (tx == null)
                return false;

            var bounds = tx.ToBounds(refPosition, refRotation, refScale);
            if (!bounds.HasValue)
                return false;

            return Bnds.TryGetSpan(
                bounds: bounds.Value,
                interpBoundsStartPoint: interpBoundsStartPoint,
                direction: direction,
                out span
            );
        }

        public static bool TryGetSpan(
            this Transform tx,
            Matrix4x4 trs,
            Vector3 interpBoundsStartPoint,
            Vector3 direction,
            out Bnds.SpanFromPoint span)
        {
            span = default;

            if (tx == null)
                return false;

            var bounds = tx.ToBounds(trs);
            if (!bounds.HasValue)
                return false;

            return Bnds.TryGetSpan(
                bounds: bounds.Value,
                interpBoundsStartPoint: interpBoundsStartPoint,
                direction: direction,
                out span
            );
        }
    }
}

