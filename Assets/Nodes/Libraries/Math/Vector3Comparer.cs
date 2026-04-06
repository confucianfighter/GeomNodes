using System.Collections;
using NUnit.Framework;
using UnityEngine;

namespace DLN
{
    public sealed class Vector3Comparer : IEqualityComparer
    {
        private readonly float tolerance;

        public Vector3Comparer(float tolerance)
        {
            this.tolerance = tolerance;
        }

        public new bool Equals(object x, object y)
        {
            if (x is not Vector3 a || y is not Vector3 b)
                return false;

            return Mathf.Abs(a.x - b.x) <= tolerance
                && Mathf.Abs(a.y - b.y) <= tolerance
                && Mathf.Abs(a.z - b.z) <= tolerance;
        }

        public int GetHashCode(object obj) => obj?.GetHashCode() ?? 0;
    }
}