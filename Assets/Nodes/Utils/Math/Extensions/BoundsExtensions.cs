using UnityEngine;
using Unity.Mathematics;

namespace DLN.Extensions
{
    public static class BoundsExtensions
    {
        public static SphereData ToSphere(this Bounds bounds, bool fitCorners = false)
        {
            return new SphereData
            {
                center = bounds.center,
                // extents are always positive, (max - min) / 2
                // this is a tighter fit than extents.magnitude / 2.
                radius = fitCorners ? bounds.extents.Max() : bounds.extents.magnitude
            };

        }
        public static CapsuleData ToCapsule(this Bounds bounds)
        {
            var maxAxis = bounds.extents.MaxAxis();
            return bounds.ToCapsule(maxAxis);
        }
        public static CapsuleData ToCapsule(this Bounds bounds, CartesianAxis axis)
        {
            var capsule = new CapsuleData();
            var maxExtent = bounds.extents.Max();

            switch (axis)
            {

                case CartesianAxis.X:
                    capsule.radius = Mathf.Max(bounds.extents.y, bounds.extents.z);
                    break;
                case CartesianAxis.Y:
                    capsule.radius = Mathf.Max(bounds.extents.x, bounds.extents.z);

                    break;
                case CartesianAxis.Z:
                    capsule.radius = Mathf.Max(bounds.extents.x, bounds.extents.y);

                    break;

            }

            capsule.direction = (int)axis;
            capsule.center = bounds.center;
            capsule.height = Mathf.Max(maxExtent, capsule.radius * 2f);
            return capsule;
        }
        public static Bounds ExpandIfZero(this Bounds bounds, float epsilon)
        {
            var min = bounds.min;
            var max = bounds.max;
            var center = bounds.center;
            var half = epsilon * 0.5f;

            if (bounds.size.x < epsilon) { min.x = center.x - half; max.x = center.x + half; }
            if (bounds.size.y < epsilon) { min.y = center.y - half; max.y = center.y + half; }
            if (bounds.size.z < epsilon) { min.z = center.z - half; max.z = center.z + half; }

            bounds.SetMinMax(min, max);
            return bounds;
        }
        public static Vector3 Interpolate(this Bounds b, Vector3 v)
        {

            v.x = math.remap(0f, 1f, b.min.x, b.max.x, v.x);
            v.y = math.remap(0f, 1f, b.min.y, b.max.y, v.y);
            v.z = math.remap(0f, 1f, b.min.z, b.max.z, v.z);
            return v;

        }

    }

}