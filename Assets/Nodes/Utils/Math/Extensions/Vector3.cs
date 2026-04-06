using UnityEngine;
using System.Linq;

namespace DLN
{
    public static partial class Maths
    {
        public static Vector3 Mul(this Vector3 a, Vector3 b)
        {
            return new Vector3(
                a.x * b.x,
                a.y * b.y,
                a.z * b.z
            );
        }
        public static CartesianAxis MaxAxis(this Vector3 v)
        {
            v = v.Abs();
            var max = v.x;
            var axis = CartesianAxis.X;
            if (v.y > max)
            {
                max = v.y;
                axis = CartesianAxis.Y;
            }
            if (v.z > max)
            {
                axis = CartesianAxis.Z;
            }
            return axis;

        }
        public static CartesianAxis GetDominantCartesianAxis(this Vector3 v)
        {
            var dominantAxis = CartesianAxis.X;
            var vAbs = v.Abs();
            if (v.x < v.y) dominantAxis = CartesianAxis.Y;
            if (v.y < v.z) dominantAxis = CartesianAxis.Z;

            switch (dominantAxis)
            {
                case CartesianAxis.X:
                    if (v.x < 1) dominantAxis = CartesianAxis.minusY;
                    break;
                case CartesianAxis.Y:
                    if (v.y < 1) dominantAxis = CartesianAxis.minusY;
                    break;
                case CartesianAxis.Z:
                    if (v.z < 1) dominantAxis = CartesianAxis.minusZ;
                    break;
                default:
                    break;

            }
            return dominantAxis;

        }
        public static Vector3 Abs(this Vector3 vector)
        {
            vector.x = Mathf.Abs(vector.x);
            vector.y = Mathf.Abs(vector.y);
            vector.z = Mathf.Abs(vector.z);
            return vector;
        }
        public static float Max(this Vector3 v)
        {
            v = v.Abs();
            var max = v.x;
            if (v.y > max) max = v.y;
            if (v.z > max) max = v.z;
            return max;
        }
        public static Vector3 DivideBy(this Vector3 target, Vector3 divisor)
        {
            return new Vector3(
                target.x / divisor.x,
                target.y / divisor.y,
                target.z / divisor.z
            );
        }
        public static Vector3 ConstrainUniform(this Vector3 target)
        {
            var includeAxis = new IncludeAxis { x = true, y = true, z = true };
            return Maths.ConstrainUniform(
                target,
                includeAxis,
                includeAxis
            );

        }
        public static Vector3 ConstrainUniform(
            this Vector3 target,
            IncludeAxis axisToConstrainTo,
            IncludeAxis axisToConstrain)
        {
            var newScale = Vector3.one;
            float[] ratios = Enumerable.Repeat(float.PositiveInfinity, 3).ToArray();
            if (axisToConstrainTo.x)
            {
                ratios[0] = target.x;
            }
            if (axisToConstrainTo.y)
            {
                ratios[1] = target.y;
            }

            if (axisToConstrainTo.z)
            {
                ratios[2] = target.z;
            }

            var scaleFactor = ratios.Min();
            Debug.Log(scaleFactor);

            if (axisToConstrain.x)
            {
                newScale.x = scaleFactor;
            }
            if (axisToConstrain.y)
            {
                newScale.y = scaleFactor;
            }
            if (axisToConstrain.z)
            {
                newScale.z = scaleFactor;
            }
            return newScale;
        }
        public static Vector3 MixTrue(this Vector3 v, Vector3 trueValues, IncludeAxis includeAxis)
        {
            if (includeAxis.x) v.x = trueValues.x;
            if (includeAxis.y) v.y = trueValues.y;
            if (includeAxis.z) v.z = trueValues.z;
            return v;

        }



    }

}