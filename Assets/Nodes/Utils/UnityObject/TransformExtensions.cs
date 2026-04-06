// using UnityEngine;

// namespace DLN
// {
//     public static class TransformExtensions
//     {
//         /// <summary>
//         /// Returns the normalized parameter t (in [0,1]) of
//         /// projecting this Transform’s position onto the line segment AB.
//         /// </summary>
//         public static void GetProjectionT(this Transform self, Transform pointA, Transform pointB, out float t)
//         {
//             t = 0f;
//             Vector3 A = pointA.position;
//             Vector3 B = pointB.position;
//             Vector3 AB = B - A;

//             // If A and B coincide, just return 0
//             float denom = Vector3.Dot(AB, AB);
//             if (denom < Mathf.Epsilon)
//                 return;

//             Vector3 AP = self.position - A;
//             float rawT = Vector3.Dot(AP, AB) / denom;
//             t = Mathf.Clamp01(rawT);
//         }
//         // interpolate between two transforms at t
//         public static void InterpolateBetweenTransforms(this Transform self, Transform pointA, Transform pointB, float t, out Vector3 newPosition)
//         {
//             Vector3 A = pointA.position;
//             Vector3 B = pointB.position;
//             Vector3 AB = B - A;
//             newPosition = A + t * AB;
//             self.position = newPosition;
//         }


//         /// <summary>
//         /// Sets the target Transform’s world‐space position to be the interpolation
//         /// between <paramref name="from"/>.position and <paramref name="to"/>.position at parameter t (0…1).
//         /// </summary>
//         /// <param name="target">The Transform to move.</param>
//         /// <param name="from">Starting Transform.</param>
//         /// <param name="to">Ending Transform.</param>
//         /// <param name="t">Interpolation factor (0…1).</param>
//         /// 
//         // Get transform from either a Transform, a gameobject or a MonoBehaviour
//         public static Transform GetTransform(this UnityEngine.Object target)
//         {
//             if (target == null)
//             {
//                 return null;
//             }
//             if (target is Transform)
//             {
//                 return target as Transform;
//             }
//             if (target is GameObject)
//             {
//                 return (target as GameObject).transform;
//             }
//             if (target is MonoBehaviour)
//             {
//                 return (target as MonoBehaviour).transform;
//             }
//             // error
//             Debug.LogError("Invalid target type: " + target.GetType().Name);
//             return null;
//         }
//         public static void LerpPositionTo(this Transform target, Transform from, Transform to, float t)
//         {
//             if (target == null || from == null || to == null)
//             {
//                 return;
//             }
//             float clampedT = Mathf.Clamp01(t);
//             Vector3 startPos = from.position;
//             Vector3 endPos = to.position;
//             target.position = Vector3.Lerp(startPos, endPos, clampedT);

//         }

//         /// <summary>
//         /// Given two Transforms (as endpoints) and a world‐space point, computes the parameter t (unclamped) 
//         /// for which Vector3.Lerp(from.position, to.position, t) ≈ <paramref name="position"/>.
//         /// Returns 0 if <paramref name="from"/> and <paramref name="to"/> coincide.
//         /// </summary>
//         /// <param name="from">The “start” Transform.</param>
//         /// <param name="to">The “end” Transform.</param>
//         /// <param name="position">A world‐space point.</param>
//         /// <returns>
//         /// t such that Lerp(from.position, to.position, t) projects onto the line segment.
//         /// </returns>
//         public static float GetLerpParameter(this Transform from, Transform to, Vector3 position)
//         {
//             if (from == null || to == null)
//             {
//                 return 0f;
//             }
//             Vector3 a = from.position;
//             Vector3 b = to.position;
//             Vector3 ab = b - a;
//             float denom = Vector3.Dot(ab, ab);
//             if (Mathf.Approximately(denom, 0f))
//             {
//                 return 0f;
//             }
//             // Project (position – a) onto ab:
//             float t = Vector3.Dot(position - a, ab) / denom;
//             return t;
//         }

//         /// <summary>
//         /// Overload: Given two Transforms (as endpoints) and a target Transform, computes the parameter t (unclamped) 
//         /// for which Vector3.Lerp(from.position, to.position, t) ≈ target.position.
//         /// </summary>
//         /// <param name="from">The “start” Transform.</param>
//         /// <param name="to">The “end” Transform.</param>
//         /// <param name="target">The Transform whose position is used.</param>
//         /// <returns>
//         /// t such that Lerp(from.position, to.position, t) ≈ target.position.
//         /// </returns>
//         public static float GetLerpParameter(this Transform from, Transform to, Transform target)
//         {
//             if (target == null)
//             {
//                 return 0f;
//             }
//             return GetLerpParameter(from, to, target.position);
//         }

//         /// <summary>
//         /// Moves the target Transform to the nearest point on the line between <paramref name="from"/> and <paramref name="to"/>,
//         /// snapping the interpolation parameter to the nearest multiple of <paramref name="interval"/>. 
//         /// Returns that snapped t.
//         /// </summary>
//         /// <param name="target">The Transform to move.</param>
//         /// <param name="from">Start Transform.</param>
//         /// <param name="to">End Transform.</param>
//         /// <param name="interval">
//         /// The step interval for snapping t. For example, interval = 0.25f snaps t to 0.00, 0.25, 0.50, 0.75, 1.00, etc.
//         /// </param>
//         /// <returns>
//         /// The snapped t such that the new target.position = Lerp(from.position, to.position, snappedT).
//         /// </returns>
//         public static float SnapToNearestLerp(this Transform target, Transform from, Transform to, float interval)
//         {
//             if (target == null || from == null || to == null || Mathf.Approximately(interval, 0f))
//             {
//                 return 0f;
//             }
//             // Compute raw t based on current target.position
//             float tRaw = GetLerpParameter(from, to, target.position);

//             // Snap tRaw to nearest multiple of interval
//             float tSnapped = Mathf.Round(tRaw / interval) * interval;

//             // Optionally clamp between 0 and 1 if you want to stay on the segment
//             tSnapped = Mathf.Clamp01(tSnapped);

//             // Compute new position and assign
//             Vector3 newPos = Vector3.Lerp(from.position, to.position, tSnapped);
//             target.position = newPos;

//             return tSnapped;
//         }
//         // uniform scale by factor
//         public static void UniformScaleByFactor(this Transform target, float scaleFactor, out Transform targetOut)
//         {
//             Vector3 newScale = Vector3.one;
//             if (target == null)
//             {
//                 targetOut = null;
//                 return;
//             }
//             // get current scale
//             Vector3 currentScale = target.localScale;
//             // scale by factor
//             newScale = currentScale * scaleFactor;
//             // assign new scale
//             target.localScale = newScale;
//             // return target and new scale
//             targetOut = target;
//         }
//         public static void ConstrainLocalXPosition(this UnityEngine.Object target, float min, float max)
//         {
//             Transform targetTransform = target.AsTransform();
//             if (targetTransform == null)
//             {
//                 return;
//             }
//             // u
//             Vector3 localPosition = targetTransform.localPosition;
//             localPosition.x = Mathf.Clamp(localPosition.x, min, max);
//             targetTransform.localPosition = localPosition;
//         }
//         public static void ConstrainLocalYPosition(this UnityEngine.Object target, float min, float max)
//         {
//             Transform targetTransform = target.AsTransform();
//             if (targetTransform == null)
//             {
//                 return;
//             }
//             // u
//             Vector3 localPosition = targetTransform.localPosition;
//             localPosition.y = Mathf.Clamp(localPosition.y, min, max);
//             targetTransform.localPosition = localPosition;
//         }
//         public static void ConstrainLocalZPosition(this UnityEngine.Object target, object relativeTo, float min, float max)
//         {
//             Transform targetTransform = target.AsTransform();
//             Transform relativeToTransform = relativeTo.AsTransform();
//             if (targetTransform == null || relativeToTransform == null)
//             {
//                 return;
//             }
//             var localPosition = relativeToTransform.InverseTransformPoint(targetTransform.position);
//             localPosition.z = Mathf.Clamp(localPosition.z, min, max);
//             var worldPosition = relativeToTransform.TransformPoint(localPosition);
//             targetTransform.position = worldPosition;
//         }
//         /// <summary>
//         /// Constrains this transform’s position to lie on the line segment from pointA to pointB.
//         /// </summary>
//         /// <param name="self">The Transform to be constrained.</param>
//         /// <param name="pointA">One end of the segment.</param>
//         /// <param name="pointB">The other end of the segment.</param>
//         public static void ConstrainToLine(this Transform self, Transform pointA, Transform pointB)
//         {
//             Vector3 A = pointA.position;
//             Vector3 B = pointB.position;
//             Vector3 AB = B - A;

//             // Avoid division by zero if A and B are at the same spot
//             float denom = Vector3.Dot(AB, AB);
//             if (denom < Mathf.Epsilon)
//             {
//                 // Just snap to A (which is essentially equal to B)
//                 self.position = A;
//                 return;
//             }

//             // Project self.position onto the infinite line AB, then clamp to [0,1]
//             Vector3 AP = self.position - A;
//             float t = Vector3.Dot(AP, AB) / denom;
//             t = Mathf.Clamp01(t);

//             // Reconstruct the clamped point on segment AB
//             self.position = A + t * AB;
//         }
//         public static void ConstrainMinMaxLocalPosition(this UnityEngine.Object target, object relativeTo, Transform min, Transform max, out Vector3 newPosition)
//         {
//             newPosition = Vector3.zero;
//             Transform targetTransform = target.AsTransform();
//             Transform relativeToTransform = relativeTo.AsTransform();
//             if (targetTransform == null || relativeToTransform == null)
//             {
//                 return;
//             }
//             var localPosition = relativeToTransform.InverseTransformPoint(targetTransform.position);
//             localPosition.x = Mathf.Clamp(localPosition.x, min.position.x, max.position.x);
//             localPosition.y = Mathf.Clamp(localPosition.y, min.position.y, max.position.y);
//             localPosition.z = Mathf.Clamp(localPosition.z, min.position.z, max.position.z);
//             var worldPosition = relativeToTransform.TransformPoint(localPosition);
//             targetTransform.position = worldPosition;
//             newPosition = worldPosition;


//         }


//         /// <summary>
//         /// Converts a local-space rotation (quaternion) into world-space,
//         /// based on the given reference transform’s rotation. Result is passed via out.
//         /// </summary>
//         /// <param name="referenceTransform">
//         /// The transform whose world rotation is the basis.</param>
//         /// <param name="localRotation">
//         /// The quaternion representing a rotation in that transform’s local space.</param>
//         /// <param name="worldRotation">
//         /// The resulting world-space quaternion (output via out parameter).</param>
//         public static void ToWorldRotation(
//             this Transform referenceTransform,
//             Quaternion localRotation,
//             out Quaternion worldRotation
//         )
//         {
//             // Parent’s world rotation multiplied by local rotation gives world rotation.
//             worldRotation = referenceTransform.rotation * localRotation;
//         }

//         /// <summary>
//         /// Converts a local-space rotation into world-space (using referenceTransform),
//         /// and immediately applies it to the target transform.
//         /// </summary>
//         /// <param name="referenceTransform">
//         /// The transform whose world rotation is the basis.</param>
//         /// <param name="localRotation">
//         /// The quaternion representing a rotation in that transform’s local space.</param>
//         /// <param name="targetTransform">
//         /// The Transform that will be set to the computed world-space rotation.</param>
//         public static void SetWorldRotation(
//             this Transform referenceTransform,
//             Quaternion localRotation,
//             Transform targetTransform
//         )
//         {
//             // Compute world-space rotation
//             Quaternion worldRot = referenceTransform.rotation * localRotation;
//             // Apply it directly to the target
//             targetTransform.rotation = worldRot;
//         }



//     }
// }