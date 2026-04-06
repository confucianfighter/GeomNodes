using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DLN.Conversions;
using static DLN.Bnds;

// Todo: do not rely on order of args, but instead use named args
namespace DLN
{
    public static class TransformUtils
    {
        // --- World Position ---
        public static void FromTo(Vector3 from, Vector3 to, out Vector3 vec)
        {
            vec = to - from;
        }


        public static void RollTowards(
           UnityEngine.Object obj,
           UnityEngine.Object target,
            Vector3 axis = default,
            Vector3 localPoint = default,
            bool targetIsRelative = false)
        {
            // 1) Resolve your transform and target position
            var t = GetTransform(obj);
            Vector3 dest = target.AsTransform().position
                          + (targetIsRelative ? t.position : Vector3.zero);

            // 2) Compute the world-space position of your local point
            Vector3 worldP = t.TransformPoint(localPoint);

            // 3) Project both the current vector and the desired vector into the plane orthogonal to 'axis'
            Vector3 fromDir = Vector3.ProjectOnPlane(worldP - t.position, axis).normalized;
            Vector3 toDir = Vector3.ProjectOnPlane(dest - t.position, axis).normalized;

            // If either vector is zero-length, bail early
            if (fromDir.sqrMagnitude < Mathf.Epsilon || toDir.sqrMagnitude < Mathf.Epsilon)
                return;

            // 4) Compute the signed angle between them around 'axis'
            float angle = Vector3.SignedAngle(fromDir, toDir, axis);

            // 5) Apply that rotation around 'axis' at the object’s pivot
            //    This only rolls the object around your chosen axis.
            t.Rotate(axis, angle);
        }

        public static Vector3 ToVector3(this UnityEngine.Object obj, out Vector3 pos)
        {
            if (obj is Transform t) { pos = t.position; return pos; }
            if (obj is GameObject go) { pos = go.transform.position; return pos; }
            if (obj is Component c) { pos = c.transform.position; return pos; }
            throw new ArgumentException($"Cannot convert {obj} to Vector3 in ToVector3");
        }
        public static Vector3 ToPosition(this UnityEngine.Object obj)
        {
            if (obj is Transform t) return t.position;
            if (obj is GameObject go) return go.transform.position;
            if (obj is Component c) return c.transform.position;
            throw new ArgumentException($"Cannot convert {obj} to Vector3 in ToPosition");
        }
        public static Vector3 ToLocalPosition(this UnityEngine.Object obj)
        {
            if (obj is Transform t) return t.localPosition;
            if (obj is GameObject go) return go.transform.localPosition;
            if (obj is Component c) return c.transform.localPosition;
            throw new ArgumentException($"Cannot convert {obj} to Vector3 in ToLocalPosition");
        }
        public static void LookAt(UnityEngine.Object @object, Vector3 target = default, Vector3 up = default, bool targetIsRelative = false)
        {
            var t = GetTransform(@object);
            if (targetIsRelative)
            {
                target = t.position + target;
            }
            if (t != null) t.LookAt(target, up);
        }
        public static Quaternion ToRotation(this UnityEngine.Object obj)
        {
            if (obj is Transform t) return t.rotation;
            if (obj is GameObject go) return go.transform.rotation;
            if (obj is Component c) return c.transform.rotation;
            throw new ArgumentException($"Cannot convert {obj} to Vector3 in ToRotation");
        }
        public static Quaternion ToLocalRotation(this UnityEngine.Object obj)
        {
            if (obj is Transform t) return t.localRotation;
            if (obj is GameObject go) return go.transform.localRotation;
            if (obj is Component c) return c.transform.localRotation;
            throw new ArgumentException($"Cannot convert {obj} to Vector3 in ToLocalRotation");
        }

        public static void SetParent(Transform target, Transform parent, out Transform outChild, out Transform outParent, bool worldPositionStays = false)
        {
            target.SetParent(parent, worldPositionStays);
            outChild = target;
            outParent = parent;
        }


        // --- Local Scale ---

        // --- GetTransform utility ---
        public static Transform GetTransform(UnityEngine.Object obj)
        {
            return obj.AsTransform();
        }

        public static void MoveWithAnchor(
           UnityEngine.Object @object,
           Vector3 anchorPoint,
           Vector3 targetPoint,
            out Vector3 outPosition,
            bool pinChildren = false)
        {
            var t = GetTransform(@object);
            var anchor = anchorPoint;
            var target = targetPoint;
            var moveAmount = target - anchor;
            t.position += moveAmount;
            outPosition = t.position;
            if (pinChildren)
            {
                // Pin children to the new position
                foreach (Transform child in t)
                {
                    child.position -= moveAmount;
                }
            }
        }
        public static void MoveByVector
        (
           UnityEngine.Object @object,
            Vector3 vector,
            out Vector3 outPosition,
            bool pinChildren = false)
        {
            var moveAmount = vector;
            outPosition = @object.ToPosition() + moveAmount;
            var t = GetTransform(@object);
            if (t != null)
            {
                t.position = outPosition;
            }
            if (pinChildren)
            {
                // Pin children to the new position
                foreach (Transform child in t)
                {
                    child.position -= moveAmount;
                }
            }
        }
    }
}


