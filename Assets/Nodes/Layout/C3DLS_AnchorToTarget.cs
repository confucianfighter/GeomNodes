using UnityEngine;
using System;

namespace DLN
{
    public class AnchorToTarget : LayoutOp
    {
        [SerializeField] public GameObject moveTarget;
        [SerializeField] public GameObject anchorTarget;
        [SerializeField] public Vector3 pivot = new Vector3(.5f, .5f, .5f);
        [SerializeField] public Vector3 anchor = new Vector3(.5f, .5f, .5f);
        [SerializeField] public bool pinChildren = false;
        [SerializeField] public IncludeAxis includeAxis = IncludeAxis.AllTrue;

        public OptionalBoundsSettings anchorTargetBoundsSettings = OptionalBoundsSettings.Empty;


        public OptionalBoundsSettings moveTargetBoundsSettings = OptionalBoundsSettings.Empty;

        [ContextMenu("Execute")]

        public override void Execute()
        {
            Bnds.InterpolateBounds(
                @object: moveTarget,
                interpVec: pivot,
                outputCoordSpace: Space.World,
                overrides: anchorTargetBoundsSettings,
                result: out var _pivot
            );
            Bnds.InterpolateBounds(
                @object: anchorTarget,
                interpVec: anchor,
                outputCoordSpace: Space.World,
                overrides: moveTargetBoundsSettings,
                result: out var _anchor
            );
            var moveAmount = _anchor - _pivot;
            moveAmount = includeAxis.Mask(moveAmount);

            moveTarget.transform.position += moveAmount;
            if (pinChildren)
            {
                foreach (Transform child in moveTarget.transform)
                {
                    child.position -= moveAmount;
                }
            }


        }
        void OnValidate()
        {
            Execute();
        }
    }

}