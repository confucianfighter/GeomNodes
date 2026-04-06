using System;
using System.Linq;
using UnityEngine;
using DLN.Extensions;

namespace DLN
{
    public class SpaceAndAlignMK2 : LayoutOp
    {
        [Header("Execution")]
        [ButtonField(nameof(Execute), "Space + Align")]
        [SerializeField] private bool updateThis = false;

        [Header("Directions")]
        [SerializeField] public Vector3 spacingDir = Vector3.right;
        [SerializeField] public Vector3 alignmentDir = Vector3.right;

        [Header("Targets")]
        [SerializeField] public Transform parent;
        [SerializeField] public float spacing = 0.2f;
        [SerializeField] public Vector3 startPos = Vector3.zero;

        [Header("Bounds Interpolation For Alignment")]
        [SerializeField] public Vector3 alignment = new Vector3(0f, 0f, 1f);

        [Header("Bounds Overrides")]
        [SerializeField] public OptionalBoundsSettings boundsOverrides;

        private void Reset()
        {
            if (parent == null)
                parent = transform;
        }

        [ContextMenu("Space By Centers")]
        public void SpaceByCenters()
        {
            var transforms = GetChildTransforms();
            if (transforms.Length == 0 || parent == null)
                return;

            SpaceByCenters(
                refTx: parent,
                transforms: transforms,
                startPos: startPos,
                spacingDir: spacingDir,
                spacing: spacing);
        }

        [ContextMenu("Align Pivots To Line")]
        public void AlignPivotsToLine()
        {
            var transforms = GetChildTransforms();
            if (transforms.Length == 0 || parent == null)
                return;

            AlignPivotsToLine(
                refTx: parent,
                transforms: transforms,
                startPos: startPos,
                spacingDir: spacingDir,
                alignmentDir: alignmentDir,
                alignment: alignment);
        }

        [ContextMenu("Space + Align")]
        public override void Execute()
        {
            var transforms = GetChildTransforms();
            if (transforms.Length == 0 || parent == null)
                return;

            SpaceByCenters(
                refTx: parent,
                transforms: transforms,
                startPos: startPos,
                spacingDir: spacingDir,
                spacing: spacing);

            AlignPivotsToLine(
                refTx: parent,
                transforms: transforms,
                startPos: startPos,
                spacingDir: spacingDir,
                alignmentDir: alignmentDir,
                alignment: alignment);
        }

        private Transform[] GetChildTransforms()
        {
            return parent == null
                ? Array.Empty<Transform>()
                : parent.Cast<Transform>().ToArray();
        }

        public static void SpaceByCenters(
            Transform refTx,
            Transform[] transforms,
            Vector3 startPos,
            Vector3 spacingDir,
            float spacing)
        {
            if (refTx == null || transforms == null || transforms.Length == 0)
                return;

            spacingDir = spacingDir.sqrMagnitude < 0.000001f
                ? Vector3.right
                : spacingDir.normalized;

            Quaternion spacingRot = GetFrameRotation(spacingDir, refTx.up);
            Vector3 spacingOrigin = startPos;

            float cursorZ = 0f;

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform tx = transforms[i];

                var boundsOpt = tx.ToBounds(
                    spacingOrigin,
                    spacingRot,
                    Vector3.one
                );

                if (!boundsOpt.HasValue)
                    continue;

                Bounds b = boundsOpt.Value;
                Vector3 currentCenterInSpacingFrame = b.center;

                float targetCenterZ;
                if (i == 0)
                {
                    targetCenterZ = cursorZ;
                }
                else
                {
                    targetCenterZ = cursorZ + b.extents.z;
                }

                Vector3 targetCenterInSpacingFrame = new Vector3(
                    0f,
                    0f,
                    targetCenterZ
                );

                Vector3 currentWorldCenter = spacingOrigin + (spacingRot * currentCenterInSpacingFrame);
                Vector3 targetWorldCenter = spacingOrigin + (spacingRot * targetCenterInSpacingFrame);

                tx.position += targetWorldCenter - currentWorldCenter;

                // Re-sample after move so the next placement uses actual post-move bounds.
                var movedBoundsOpt = tx.ToBounds(
                    spacingOrigin,
                    spacingRot,
                    Vector3.one
                );

                if (!movedBoundsOpt.HasValue)
                    continue;

                Bounds movedBounds = movedBoundsOpt.Value;
                cursorZ = movedBounds.center.z + movedBounds.extents.z + spacing;
            }
        }

        public static void AlignPivotsToLine(
    Transform refTx,
    Transform[] transforms,
    Vector3 startPos,
    Vector3 spacingDir,
    Vector3 alignmentDir,
    Vector3 alignment)
        {
            if (refTx == null || transforms == null || transforms.Length == 0)
                return;

            spacingDir = spacingDir.sqrMagnitude < 0.000001f
                ? Vector3.right
                : spacingDir.normalized;

            alignmentDir = alignmentDir.sqrMagnitude < 0.000001f
                ? spacingDir
                : alignmentDir.normalized;

            Quaternion spacingRot = GetFrameRotation(spacingDir, refTx.up);
            Vector3 spacingOrigin = startPos;

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform tx = transforms[i];

                // 1) Read the alignment pivot in refTx space.
                var refBoundsOpt = tx.ToBounds(
                    refTx.position,
                    refTx.rotation,
                    Vector3.one
                );

                if (!refBoundsOpt.HasValue)
                    continue;

                Bounds refBounds = refBoundsOpt.Value;
                Vector3 pivotInRefSpace = refBounds.Interpolate(alignment);
                Vector3 currentPivotWorld = refTx.position + (refTx.rotation * pivotInRefSpace);

                // 2) Convert that world pivot into the spacing frame so we can preserve spacing-frame Z.
                Vector3 pivotInSpacingFrame =
                    Quaternion.Inverse(spacingRot) * (currentPivotWorld - spacingOrigin);

                // 3) Solve the point on the alignment line whose spacing-frame Z matches this pivot.
                if (!TryPointOnLineWithSpacingFrameZ(
                        lineOrigin: startPos,
                        lineDir: alignmentDir,
                        spacingFrameOrigin: spacingOrigin,
                        spacingFrameRotation: spacingRot,
                        desiredZ: pivotInSpacingFrame.z,
                        pointOnLine: out Vector3 targetPivotWorld))
                {
                    continue;
                }

                // 4) Move object so its actual alignment pivot lands there.
                tx.position += targetPivotWorld - currentPivotWorld;
            }
        }

        private static Quaternion GetFrameRotation(Vector3 forward, Vector3 upHint)
        {
            forward = forward.sqrMagnitude < 0.000001f
                ? Vector3.forward
                : forward.normalized;

            if (upHint.sqrMagnitude < 0.000001f)
                upHint = Vector3.up;

            if (Mathf.Abs(Vector3.Dot(forward, upHint.normalized)) > 0.999f)
                upHint = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.999f
                    ? Vector3.right
                    : Vector3.up;

            return Quaternion.LookRotation(forward, upHint);
        }

        /// <summary>
        /// Finds the point on a world-space line whose coordinate in the spacing frame
        /// has the requested Z value.
        /// </summary>
        private static bool TryPointOnLineWithSpacingFrameZ(
            Vector3 lineOrigin,
            Vector3 lineDir,
            Vector3 spacingFrameOrigin,
            Quaternion spacingFrameRotation,
            float desiredZ,
            out Vector3 pointOnLine)
        {
            pointOnLine = default;

            if (lineDir.sqrMagnitude < 0.000001f)
                return false;

            Vector3 dirWorld = lineDir.normalized;

            Vector3 originInSpacingFrame =
                Quaternion.Inverse(spacingFrameRotation) * (lineOrigin - spacingFrameOrigin);

            Vector3 dirInSpacingFrame =
                Quaternion.Inverse(spacingFrameRotation) * dirWorld;

            float dz = dirInSpacingFrame.z;
            if (Mathf.Abs(dz) < 0.000001f)
                return false;

            float t = (desiredZ - originInSpacingFrame.z) / dz;
            pointOnLine = lineOrigin + dirWorld * t;
            return true;
        }
    }
}