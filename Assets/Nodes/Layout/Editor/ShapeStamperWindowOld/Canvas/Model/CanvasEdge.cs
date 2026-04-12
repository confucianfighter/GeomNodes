#if false
using System;
using UnityEngine;

[Serializable]
public struct CanvasEdge
{
    public int Id;
    public int A;
    public int B;
    public float ProfileXScale;

    public CanvasEdge(int id, int a, int b, float profileXScale = 1f)
    {
        Id = id;
        A = a;
        B = b;
        ProfileXScale = profileXScale;
    }

    public float SanitizedProfileXScale => Mathf.Max(0f, ProfileXScale);
}
#endif
