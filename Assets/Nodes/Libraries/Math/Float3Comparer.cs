using System.Collections;
using NUnit.Framework;
using Unity.Mathematics;

public sealed class Float3Comparer : IEqualityComparer
{
    private readonly float tolerance;

    public Float3Comparer(float tolerance)
    {
        this.tolerance = tolerance;
    }

    public new bool Equals(object x, object y)
    {
        if (x is not float3 a || y is not float3 b)
            return false;

        return math.abs(a.x - b.x) <= tolerance
            && math.abs(a.y - b.y) <= tolerance
            && math.abs(a.z - b.z) <= tolerance;
    }

    public int GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
}