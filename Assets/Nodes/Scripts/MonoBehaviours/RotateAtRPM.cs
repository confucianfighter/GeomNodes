using UnityEngine;

namespace DLN
{
    public class RotateAtRPM : MonoBehaviour
    {
        [Tooltip("Rotations per minute.")]
        public float rpm = 10f;

        [Tooltip("Axis in local space (e.g. (0,1,0) for local up).")]
        public Vector3 localAxis = Vector3.up;

        [Tooltip("If true, axis is treated as world-space instead of local-space.")]
        public bool useWorldAxis = false;

        [Tooltip("Rotate in Update (true) or FixedUpdate (false).")]
        public bool useUpdate = true;

        void Update()
        {
            if (useUpdate) Step(Time.deltaTime);
        }

        void FixedUpdate()
        {
            if (!useUpdate) Step(Time.fixedDeltaTime);
        }

        void Step(float dt)
        {
            if (Mathf.Approximately(rpm, 0f)) return;

            Vector3 axis = localAxis;
            if (axis.sqrMagnitude < 1e-8f) return;
            axis.Normalize();

            float degreesPerSecond = rpm * 360f / 60f; // rpm -> deg/sec
            float angle = degreesPerSecond * dt;

            if (useWorldAxis)
            {
                transform.Rotate(axis, angle, UnityEngine.Space.World);
            }
            else
            {
                transform.Rotate(axis, angle, UnityEngine.Space.Self);
            }
        }
    }
}
