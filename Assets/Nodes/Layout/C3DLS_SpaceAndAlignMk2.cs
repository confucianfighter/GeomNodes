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

        [Header("Directions In Working Space")]
        [SerializeField] public Vector3 spacingDir = Vector3.forward;
        [SerializeField] public Vector3 alignmentDir = Vector3.forward;

        [Header("Targets")]
        [SerializeField] public Transform parent;
        [SerializeField] public float spacing = 0.2f;
        [SerializeField] public Vector3 startPos = Vector3.zero;

        [Header("Bounds Pivot For Alignment")]
        [SerializeField] public Vector3 pivot01 = new Vector3(0f, 0f, 1f);

        [Header("Bounds Overrides")]
        [SerializeField] public OptionalBoundsSettings boundsOverrides;

        private void Reset()
        {
            if (parent == null)
                parent = transform;
        }
        void OnValidate()
        {
            Execute();
        }

        [ContextMenu("Space By Centers")]
        public void SpaceByCenters()
        {
            Transform[] transforms = GetChildTransforms();
            if (transforms.Length == 0 || parent == null)
                return;

            Align.SpaceWithOBBsAndClearancesAlongDirection(
                workingFrame: parent.ToFrame(),
                transforms: transforms,
                startPos: startPos,
                spacingDir: spacingDir,
                spacing: spacing
            );
        }

        [ContextMenu("Align Pivots To Line")]
        public void AlignPivotsOnly()
        {
            Transform[] transforms = GetChildTransforms();
            if (transforms.Length == 0 || parent == null)
                return;

            Align.AlignPivotsToLine(
                workingFrame: parent.ToFrame(),
                transforms: transforms,
                startPos: startPos,
                lineDir: alignmentDir,
                lockAxis: spacingDir,
                pivot01: pivot01
            );
        }

        [ContextMenu("Space + Align")]
        public override void Execute()
        {
            Transform[] transforms = GetChildTransforms();
            if (transforms.Length == 0 || parent == null)
                return;

            Frame workingFrame = parent.ToFrame();

            Align.SpaceWithOBBsAndClearancesAlongDirection(
                workingFrame: workingFrame,
                transforms: transforms,
                startPos: startPos,
                spacingDir: spacingDir,
                spacing: spacing
            );

            Align.AlignPivotsToLine(
                workingFrame: workingFrame,
                transforms: transforms,
                startPos: startPos,
                lineDir: alignmentDir,
                lockAxis: spacingDir,
                pivot01: pivot01
            );
        }

        private Transform[] GetChildTransforms()
        {
            return parent == null
                ? Array.Empty<Transform>()
                : parent.Cast<Transform>().ToArray();
        }
    }
}