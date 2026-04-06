using UnityEngine;

namespace DLN
{
    public static class TRS
    {
        public static Matrix4x4 FromAxes(Vector3 right, Vector3 up, Vector3 forward, Vector3 position)
        {
            Matrix4x4 m = new Matrix4x4();
            m.SetColumn(0, new Vector4(right.x, right.y, right.z, 0f));
            m.SetColumn(1, new Vector4(up.x, up.y, up.z, 0f));
            m.SetColumn(2, new Vector4(forward.x, forward.y, forward.z, 0f));
            m.SetColumn(3, new Vector4(position.x, position.y, position.z, 1f));
            return m;
        }

        public static Matrix4x4 FromForward(Vector3 forward, Vector3 position, Vector3? upHint = null)
        {
            forward = forward.sqrMagnitude < 0.000001f
                ? Vector3.forward
                : forward.normalized;

            Vector3 up = upHint ?? Vector3.up;

            if (Mathf.Abs(Vector3.Dot(forward, up.normalized)) > 0.999f)
                up = Vector3.forward;

            Vector3 right = Vector3.Cross(up, forward).normalized;
            up = Vector3.Cross(forward, right).normalized;

            return FromAxes(right, up, forward, position);
        }

        public static void GetAxesFromForward(
            Vector3 forward,
            out Vector3 right,
            out Vector3 up,
            Vector3? upHint = null)
        {
            forward = forward.sqrMagnitude < 0.000001f
                ? Vector3.forward
                : forward.normalized;

            up = upHint ?? Vector3.up;

            if (Mathf.Abs(Vector3.Dot(forward, up.normalized)) > 0.999f)
                up = Vector3.forward;

            right = Vector3.Cross(up, forward).normalized;
            up = Vector3.Cross(forward, right).normalized;
        }
    }
}