using UnityEngine;
namespace DLN
{
    [System.Serializable]
    // applied to all axis
    public struct IncludeAxis
    {
        public bool x;
        public bool y;
        public bool z;

        public Vector3 Mask(Vector3 target)
        {
            Vector3 mask = target;
            if (!x) mask.x = 0;
            if (!y) mask.y = 0;
            if (!z) mask.z = 0;
            return mask;
        }
        public static IncludeAxis AllTrue => new IncludeAxis
        {
            x = true,
            y = true,
            z = true
        };
        public static IncludeAxis AllFalse => new IncludeAxis
        {
            x = false,
            y = false,
            z = false
        };
    }
}