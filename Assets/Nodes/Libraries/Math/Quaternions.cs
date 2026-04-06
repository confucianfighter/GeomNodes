using System;
using System.Linq;
using UnityEngine;
using DLN.Extensions;

namespace DLN
{
    public static class Quaternions
    {
        public static Quaternion RotationFromForwardUp(Vector3 forward, Vector3 upHint)
        {
            forward = forward.sqrMagnitude < 0.000001f
                ? Vector3.forward
                : forward.normalized;

            if (upHint.sqrMagnitude < 0.000001f)
                upHint = Vector3.up;

            upHint = upHint.normalized;

            if (Mathf.Abs(Vector3.Dot(forward, upHint)) > 0.999f)
            {
                upHint = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.999f
                    ? Vector3.right
                    : Vector3.up;
            }

            return Quaternion.LookRotation(forward, upHint);
        }
    }
}