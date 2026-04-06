using System.Linq;
using UnityEngine;
namespace DLN
{

    public static partial class Maths
    {
        public static Vector3 ConstrainByUniformFit(Vector3 desiredDimensions, Bounds objectToResizeBounds, IncludeAxis axisToConstrainTo, IncludeAxis axisToConstrain)
        {
            // say preliminarydimensions is 2, 1 in the x and y. and our object is 1,1, scalefactor would be 1
            float[] ratios = Enumerable.Repeat(float.PositiveInfinity, 3).ToArray();
            if (axisToConstrainTo.x)
            {
                ratios[0] = desiredDimensions.x / objectToResizeBounds.size.x; // = 2
            }
            if (axisToConstrainTo.y)
            {
                ratios[1] = desiredDimensions.y / objectToResizeBounds.size.y; // = 1
            }

            if (axisToConstrainTo.z)
            {
                ratios[2] = desiredDimensions.z / objectToResizeBounds.size.z;
            }

            var scaleFactor = ratios.Min();
            Debug.Log(scaleFactor);

            if (axisToConstrain.x)
            {
                desiredDimensions.x = objectToResizeBounds.size.x * scaleFactor;
            }
            if (axisToConstrain.y)
            {
                desiredDimensions.y = objectToResizeBounds.size.y * scaleFactor;
            }
            if (axisToConstrain.z)
            {
                desiredDimensions.z = objectToResizeBounds.size.z * scaleFactor;
            }
            Debug.Log(desiredDimensions);
            return desiredDimensions;
            // take least ratio of included axis
            // scale inclusion axis by ratio
        }


    }
}