using UnityEngine;
using Unity.XR.CoreUtils;

public static class XRRuntime
{
    private static XROrigin _cachedOrigin;

    public static XROrigin Origin
    {
        get
        {
            if (_cachedOrigin == null)
                _cachedOrigin = Object.FindObjectOfType<XROrigin>();
            return _cachedOrigin;
        }
    }
}
