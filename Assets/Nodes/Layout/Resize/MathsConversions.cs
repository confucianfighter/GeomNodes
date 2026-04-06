using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace DLN
{
    public static class MathsConversions
    {
        public static void CopyToMeshVertices(NativeArray<float3> source, Mesh mesh)
        {
            if (!source.IsCreated)
                throw new ArgumentException("Source NativeArray is not created.", nameof(source));

            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            var verts = new Vector3[source.Length];
            for (int i = 0; i < source.Length; i++)
                verts[i] = source[i];

            mesh.vertices = verts;
        }
        public static NativeArray<float3> ToNativeFloat3Array(
            this Vector3[] source,
            Allocator allocator = Allocator.Temp)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var result = new NativeArray<float3>(source.Length, allocator);

            for (int i = 0; i < source.Length; i++)
                result[i] = source[i];

            return result;
        }
        public static NativeArray<float> xValues(
            this NativeArray<float3> float3s,
            Allocator allocator = Allocator.Temp)
        {
            var xValues = new NativeArray<float>(float3s.Length, allocator);
            for (int i = 0; i < float3s.Length; i++)
            {
                xValues[i] = float3s[i].x;
            }
            return xValues;
        }
        public static NativeArray<float> yValues(
            this NativeArray<float3> float3s,
            Allocator allocator = Allocator.Temp)
        {
            var yValues = new NativeArray<float>(float3s.Length, allocator);
            for (int i = 0; i < float3s.Length; i++)
            {
                yValues[i] = float3s[i].y;
            }
            return yValues;
        }
        public static NativeArray<float> zValues(
            this NativeArray<float3> float3s,
            Allocator allocator = Allocator.Temp)
        {
            var zValues = new NativeArray<float>(float3s.Length, allocator);
            for (int i = 0; i < float3s.Length; i++)
            {
                zValues[i] = float3s[i].z;
            }
            return zValues;
        }
        public static NativeArray<float3> ToNativeFloat3Array(
            this Mesh mesh,
            Allocator allocator = Allocator.Temp)
        {
            if (mesh == null)
                throw new ArgumentNullException(nameof(mesh));

            return ToNativeFloat3Array(mesh.vertices, allocator);
        }
        public static NativeArray<float3> CombineXYZ(NativeArray<float> xValues, NativeArray<float> yValues, NativeArray<float> zValues, Allocator allocator = Allocator.Temp)
        {
            if (xValues.Length != yValues.Length ||
                xValues.Length != zValues.Length)
            {
                throw new Exception("X, Y, and Z lengths don't match.");
            }
            var float3s = new NativeArray<float3>(xValues.Length, allocator);
            for (int i = 0; i < float3s.Length; i++)
            {
                float3s[i] = new float3(xValues[i], yValues[i], zValues[i]);
            }
            return float3s;

        }
        public static NativeArray<float3> SetXYZ(this ref NativeArray<float3> float3s, NativeArray<float> xValues, NativeArray<float> yValues, NativeArray<float> zValues, Allocator allocator = Allocator.Temp)
        {
            if (xValues.Length != yValues.Length ||
                xValues.Length != zValues.Length)
            {
                throw new Exception("X, Y, and Z lengths don't match.");
            }
            for (int i = 0; i < float3s.Length; i++)
            {
                float3s[i] = new float3(xValues[i], yValues[i], zValues[i]);
            }
            return float3s;
        }
        public static Vector3[] ToVec(this NativeArray<float3> source)
        {
            if (!source.IsCreated)
                throw new ArgumentException("Source NativeArray is not created.", nameof(source));

            var result = new Vector3[source.Length];

            for (int i = 0; i < source.Length; i++)
                result[i] = source[i];

            return result;
        }

        public static Vector3[] ToVector3Array(NativeSlice<float3> source)
        {
            var result = new Vector3[source.Length];

            for (int i = 0; i < source.Length; i++)
                result[i] = source[i];

            return result;
        }

        public static void CopyToFloat3(
            Vector3[] source,
            NativeArray<float3> destination)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (!destination.IsCreated)
                throw new ArgumentException("Destination NativeArray is not created.", nameof(destination));

            if (destination.Length != source.Length)
                throw new ArgumentException("Source and destination lengths must match.");

            for (int i = 0; i < source.Length; i++)
                destination[i] = source[i];
        }

        public static void CopyToVector3(
            NativeArray<float3> source,
            Vector3[] destination)
        {
            if (!source.IsCreated)
                throw new ArgumentException("Source NativeArray is not created.", nameof(source));

            if (destination == null)
                throw new ArgumentNullException(nameof(destination));

            if (destination.Length != source.Length)
                throw new ArgumentException("Source and destination lengths must match.");

            for (int i = 0; i < source.Length; i++)
                destination[i] = source[i];
        }

        public static void CopyToFloat3(
            NativeSlice<Vector3> source,
            NativeArray<float3> destination)
        {
            if (!destination.IsCreated)
                throw new ArgumentException("Destination NativeArray is not created.", nameof(destination));

            if (destination.Length != source.Length)
                throw new ArgumentException("Source and destination lengths must match.");

            for (int i = 0; i < source.Length; i++)
                destination[i] = source[i];
        }
    }
}