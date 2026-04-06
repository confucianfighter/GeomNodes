using UnityEngine;
using DLN.Extensions;

namespace DLN
{
    public static class Align
    {
        public static void SpaceWithOBBsAndClearancesAlongDirection(
            Frame workingFrame,
            Transform[] transforms,
            Vector3 startPos,
            Vector3 spacingDir,
            float spacing)
        {
            if (transforms == null || transforms.Length == 0)
                return;

            spacingDir = spacingDir.sqrMagnitude < 0.000001f
                ? Vector3.forward
                : spacingDir.normalized;

            Vector3 startPosWorld = workingFrame.LocalToWorldPosition(startPos);
            Vector3 spacingDirWorld = workingFrame.LocalToWorldDirection(spacingDir).normalized;
            Vector3 upHintWorld = workingFrame.LocalToWorldDirection(Vector3.up).normalized;

            Frame spacingFrame = Frame.FromForwardUp(
                position: startPosWorld,
                forward: spacingDirWorld,
                upHint: upHintWorld,
                scale: workingFrame.Scale
            );

            float cursorZ = 0f;

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform tx = transforms[i];
                if (tx == null)
                    continue;

                var boundsOpt = tx.ToBounds(
                    spacingFrame.Position,
                    spacingFrame.Rotation,
                    spacingFrame.Scale
                );

                if (!boundsOpt.HasValue)
                    continue;

                Bounds boundsInSpacingFrame = boundsOpt.Value;

                float targetCenterZ = (i == 0)
                    ? cursorZ
                    : cursorZ + boundsInSpacingFrame.extents.z;

                Vector3 targetCenterInSpacingFrame = new Vector3(
                    0f,
                    0f,
                    targetCenterZ
                );

                Vector3 currentWorldCenter = spacingFrame.LocalToWorldPosition(boundsInSpacingFrame.center);
                Vector3 targetWorldCenter = spacingFrame.LocalToWorldPosition(targetCenterInSpacingFrame);

                tx.position += targetWorldCenter - currentWorldCenter;

                // Re-sample after the move so the next placement uses actual post-move bounds.
                var movedBoundsOpt = tx.ToBounds(
                    spacingFrame.Position,
                    spacingFrame.Rotation,
                    spacingFrame.Scale
                );

                if (!movedBoundsOpt.HasValue)
                    continue;

                Bounds movedBoundsInSpacingFrame = movedBoundsOpt.Value;
                cursorZ = movedBoundsInSpacingFrame.center.z + movedBoundsInSpacingFrame.extents.z + spacing;
            }
        }

        /// <summary>
        /// Slides items along the plane perpendicular to lockAxis so that the chosen
        /// pivot on each item's working-space OBB lands on the target line.
        /// All public geometric inputs are in workingFrame local space.
        /// </summary>
        public static void AlignPivotsToLine(
            Frame workingFrame,
            Transform[] transforms,
            Vector3 startPos,
            Vector3 lineDir,
            Vector3 lockAxis,
            Vector3 pivot01)
        {
            if (transforms == null || transforms.Length == 0)
                return;

            lockAxis = lockAxis.sqrMagnitude < 0.000001f
                ? Vector3.right
                : lockAxis.normalized;

            lineDir = lineDir.sqrMagnitude < 0.000001f
                ? lockAxis
                : lineDir.normalized;

            Vector3 startPosWorld = workingFrame.LocalToWorldPosition(startPos);
            Vector3 lockAxisWorld = workingFrame.LocalToWorldDirection(lockAxis).normalized;
            Vector3 lineDirWorld = workingFrame.LocalToWorldDirection(lineDir).normalized;
            Vector3 upHintWorld = workingFrame.LocalToWorldDirection(Vector3.up).normalized;

            Frame lockFrame = Frame.FromForwardUp(
                position: startPosWorld,
                forward: lockAxisWorld,
                upHint: upHintWorld,
                scale: workingFrame.Scale
            );

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform tx = transforms[i];
                if (tx == null)
                    continue;

                var boundsOpt = tx.ToBounds(
                    workingFrame.Position,
                    workingFrame.Rotation,
                    workingFrame.Scale
                );

                if (!boundsOpt.HasValue)
                    continue;

                Bounds boundsInWorkingFrame = boundsOpt.Value;
                Vector3 pivotInWorkingFrame = boundsInWorkingFrame.Interpolate(pivot01);
                Vector3 currentPivotWorld = workingFrame.LocalToWorldPosition(pivotInWorkingFrame);

                Vector3 pivotInLockFrame = lockFrame.WorldToLocalPosition(currentPivotWorld);

                if (!TryPointOnLineWithFrameZ(
                        lineOriginWorld: startPosWorld,
                        lineDirWorld: lineDirWorld,
                        frame: lockFrame,
                        desiredZ: pivotInLockFrame.z,
                        pointOnLineWorld: out Vector3 targetPivotWorld))
                {
                    continue;
                }

                tx.position += targetPivotWorld - currentPivotWorld;
            }
        }

        /// <summary>
        /// Finds the point on a world-space line whose coordinate in the given frame
        /// has the requested local Z value.
        /// </summary>
        private static bool TryPointOnLineWithFrameZ(
            Vector3 lineOriginWorld,
            Vector3 lineDirWorld,
            Frame frame,
            float desiredZ,
            out Vector3 pointOnLineWorld)
        {
            pointOnLineWorld = default;

            if (lineDirWorld.sqrMagnitude < 0.000001f)
                return false;

            Vector3 dirWorld = lineDirWorld.normalized;

            Vector3 originInFrame = frame.WorldToLocalPosition(lineOriginWorld);
            Vector3 dirInFrame = frame.WorldToLocalVector(dirWorld);

            float dz = dirInFrame.z;
            if (Mathf.Abs(dz) < 0.000001f)
                return false;

            float t = (desiredZ - originInFrame.z) / dz;
            pointOnLineWorld = lineOriginWorld + dirWorld * t;
            return true;
        }
    }
}