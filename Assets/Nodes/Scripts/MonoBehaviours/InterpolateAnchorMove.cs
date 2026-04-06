using UnityEngine;
using DLN;
using System;
using System.Collections;
namespace DLN
{
    [RequireComponent(typeof(FollowWithPositioners))]
    public class InterpolateAnchorMove : IPositionerBase
    {
        [SerializeField] public Vector3 PointOnThingBeingMoved;
        [SerializeField] public Vector3 PointOnTarget;
        [SerializeField] public Vector3 Offset;
        [SerializeField] public float delay = 0.0f;
        [SerializeField] private float dummyFloat;
        public override void Position(Transform thingToPosition, Transform target)
        {
            if (thingToPosition == null)
            {
                Debug.LogError("InterpolateAnchorMove: thingToPosition is null.");
                return;

            }
            if (target == null)
            {
                Debug.LogError("InterpolateAnchorMove: target is null.");
                return;
            }

            Bnds.InterpolateBounds(
                @object: thingToPosition,
                t_x: PointOnThingBeingMoved.x,
                t_y: PointOnThingBeingMoved.y,
                t_z: PointOnThingBeingMoved.z,
                out var position,
                Space.World
            );
            if(position == null)
            {
                Debug.LogError("InterpolateAnchorMove: Failed to calculate position on thingToPosition. Setting to Vector3.zero");
                position = Vector3.zero;
            }

            Bnds.InterpolateBounds(
                @object: target,
                t_x: PointOnTarget.x,
                t_y: PointOnTarget.y,
                t_z: PointOnTarget.z,
                out var targetPosition,
                Space.World
            );
            if(targetPosition == null)
            {
                    Debug.LogError("InterpolateAnchorMove: Failed to calculate position on target. Setting to Vector3.zero");
                    targetPosition = Vector3.zero;
            } 
            TransformUtils.MoveWithAnchor(
                @object: thingToPosition,
                anchorPoint: position,
                targetPoint: targetPosition + Offset,
                outPosition: out _
            );
        }
    }
}
