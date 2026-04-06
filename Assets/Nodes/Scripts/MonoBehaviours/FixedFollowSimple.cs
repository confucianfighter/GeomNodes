using System.Diagnostics;
using UnityEngine;
using UnityEngine.Events;

namespace DLN
{
    [RequireComponent(typeof(PosTargetProvider))]
    [RequireComponent(typeof(LifeCycleEvents))]
    public class FixedFollowEvents : MonoBehaviour
    {
        public PosTargetProvider targetProvider;
        public bool followXRot = true;
        public bool followYRot = true;
        public bool followZRot = true;
        public bool followXPos = true;
        public bool followYPos = true;
        public bool followZPos = true;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;


        public void Follow()
        {
            if (targetProvider.TryGetTarget(out Transform targetTransform))
            {
                if (targetTransform != null)
                {
                    Vector3 position = transform.position;
                    Vector3 rotation = transform.eulerAngles;
                    if (followXPos) position.x = targetTransform.position.x;
                    if (followYPos) position.y = targetTransform.position.y;
                    if (followZPos) position.z = targetTransform.position.z;
                    if (followXRot) rotation.x = targetTransform.eulerAngles.x;
                    if (followYRot) rotation.y = targetTransform.eulerAngles.y;
                    if (followZRot) rotation.z = targetTransform.eulerAngles.z;
                    position += positionOffset;
                    rotation += rotationOffset;

                    transform.position = position;
                    transform.eulerAngles = rotation;
                }
            }
        }
    }
}