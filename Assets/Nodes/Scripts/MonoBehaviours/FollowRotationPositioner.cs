using UnityEngine;
using System;

namespace DLN
{
    public class FollowRotationPositioner : IPositionerBase
    {
        public Vector3 rotationOffsetEuler = Vector3.zero;
        public override void Position(Transform transformToPosition, Transform target)
        {
            transformToPosition.rotation = target.rotation * Quaternion.Euler(rotationOffsetEuler);
        }
    }
}