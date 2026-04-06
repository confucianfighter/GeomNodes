using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using DLN;

namespace DLN.Tests
{
    public class MathsConversionsTests
    {
        private const float Tol = 0.0001f;

        [Test]
        public void ToNativeFloat3Array_Vector3Array_CopiesValuesInOrder()
        {
            var source = new[]
            {
            new Vector3(1f, 2f, 3f),
            new Vector3(-4f, 5.5f, 6f),
            new Vector3(7f, 8f, -9f)
        };

            using var result = MathsConversions.ToNativeFloat3Array(source, Allocator.Temp);

            Assert.That(result.Length, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(new float3(1f, 2f, 3f)).Using(new Float3Comparer(Tol)));
            Assert.That(result[1], Is.EqualTo(new float3(-4f, 5.5f, 6f)).Using(new Float3Comparer(Tol)));
            Assert.That(result[2], Is.EqualTo(new float3(7f, 8f, -9f)).Using(new Float3Comparer(Tol)));
        }

        [Test]
        public void ToVector3Array_NativeFloat3Array_CopiesValuesInOrder()
        {
            using var source = new NativeArray<float3>(new[]
            {
            new float3(1f, 2f, 3f),
            new float3(-4f, 5.5f, 6f),
            new float3(7f, 8f, -9f)
        }, Allocator.Temp);

            var result = MathsConversions.ToVec(source);

            Assert.That(result.Length, Is.EqualTo(3));
            Assert.That(result[0], Is.EqualTo(new Vector3(1f, 2f, 3f)).Using(new Vector3Comparer(Tol)));
            Assert.That(result[1], Is.EqualTo(new Vector3(-4f, 5.5f, 6f)).Using(new Vector3Comparer(Tol)));
            Assert.That(result[2], Is.EqualTo(new Vector3(7f, 8f, -9f)).Using(new Vector3Comparer(Tol)));
        }

        [Test]
        public void Vector3Array_ToNativeFloat3Array_ToVector3Array_RoundTrips()
        {
            var source = new[]
            {
            new Vector3(0f, 0f, 0f),
            new Vector3(1.25f, -2.5f, 3.75f),
            new Vector3(10f, 20f, 30f)
        };

            using var native = MathsConversions.ToNativeFloat3Array(source, Allocator.Temp);
            var roundTrip = MathsConversions.ToVec(native);

            Assert.That(roundTrip.Length, Is.EqualTo(source.Length));

            for (int i = 0; i < source.Length; i++)
                Assert.That(roundTrip[i], Is.EqualTo(source[i]).Using(new Vector3Comparer(Tol)));
        }

        [Test]
        public void CopyToFloat3_Vector3Array_CopiesIntoExistingDestination()
        {
            var source = new[]
            {
            new Vector3(1f, 2f, 3f),
            new Vector3(4f, 5f, 6f)
        };

            using var destination = new NativeArray<float3>(2, Allocator.Temp);

            MathsConversions.CopyToFloat3(source, destination);

            Assert.That(destination[0], Is.EqualTo(new float3(1f, 2f, 3f)).Using(new Float3Comparer(Tol)));
            Assert.That(destination[1], Is.EqualTo(new float3(4f, 5f, 6f)).Using(new Float3Comparer(Tol)));
        }

        [Test]
        public void CopyToFloat3_LengthMismatch_Throws()
        {
            var source = new[]
            {
            new Vector3(1f, 2f, 3f)
        };

            using var destination = new NativeArray<float3>(2, Allocator.Temp);

            Assert.Throws<System.ArgumentException>(() =>
                MathsConversions.CopyToFloat3(source, destination));
        }
    }
}