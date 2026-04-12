using System;
using UnityEngine;

namespace DLN
{
    /// <summary>
    /// Phase 1 region-based shape point.
    /// Stores a 3x3 region selection plus interpolation within that region.
    /// regionLerp is expected to stay in 0..1 on each axis, but is not clamped here.
    /// </summary>
    [Serializable]
    public struct CanvasPoint
    {
        public ShapeRegionX xRegion;
        public ShapeRegionY yRegion;
        public Vector2 regionLerp;

        public CanvasPoint(ShapeRegionX xRegion, ShapeRegionY yRegion, Vector2 regionLerp)
        {
            this.xRegion = xRegion;
            this.yRegion = yRegion;
            this.regionLerp = regionLerp;
        }

        public static CanvasPoint Center =>
            new CanvasPoint(
                ShapeRegionX.Middle,
                ShapeRegionY.Middle,
                new Vector2(0.5f, 0.5f)
            );

        public override readonly string ToString()
        {
            return $"CanvasPoint({xRegion}, {yRegion}, {regionLerp})";
        }
    }
}
