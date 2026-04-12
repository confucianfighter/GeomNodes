using System;

namespace DLN
{
    /// <summary>
    /// X runs from padding edge to border edge, with content edge as the divider.
    /// Inner = padding-to-content
    /// Outer = content-to-border
    /// </summary>
    [Serializable]
    public enum ProfileXRegion
    {
        Inner = 0,
        Outer = 1,
    }
}
