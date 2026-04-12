using System;
using UnityEngine;

namespace DLN
{
    /// <summary>
    /// Phase 1 region-based profile point.
    /// Stores a 2x5 region selection plus interpolation within that region.
    /// regionLerp.x is local interpolation within the X region.
    /// regionLerp.y is local interpolation within the Z region.
    /// </summary>
    [Serializable]
    public struct ProfilePoint
    {
        public ProfileXRegion xRegion;
        public ProfileZRegion zRegion;
        public Vector2 regionLerp;

        public ProfilePoint(ProfileXRegion xRegion, ProfileZRegion zRegion, Vector2 regionLerp)
        {
            this.xRegion = xRegion;
            this.zRegion = zRegion;
            this.regionLerp = regionLerp;
        }

        public static ProfilePoint Center =>
            new ProfilePoint(
                ProfileXRegion.Inner,
                ProfileZRegion.Center,
                new Vector2(0.5f, 0.5f)
            );

        public override readonly string ToString()
        {
            return $"ProfilePoint({xRegion}, {zRegion}, {regionLerp})";
        }
    }
}
