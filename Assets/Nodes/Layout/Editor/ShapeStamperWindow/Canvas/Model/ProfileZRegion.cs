using System;

namespace DLN
{
    /// <summary>
    /// Vertical profile bands from top to bottom:
    /// 1) +border to +content
    /// 2) +content to +padding
    /// 3) +padding to -padding
    /// 4) -padding to -content
    /// 5) -content to -border
    /// </summary>
    [Serializable]
    public enum ProfileZRegion
    {
        PositiveOuter = 0,
        PositiveInner = 1,
        Center = 2,
        NegativeInner = 3,
        NegativeOuter = 4,
    }
}
