// using System;
// using UnityEngine;

// namespace DLN
// {
//     /// <summary>
//     /// Transform-based layout/scale ops.
//     /// These operate by changing the Transform itself (localScale/localPosition),
//     /// rather than editing mesh points.
//     ///
//     /// All fitting/scaling calculations are performed in refTransform local space.
//     /// </summary>
//     public static class TransformOps
//     {
//         /// <summary>
//         /// Returns the 8 corners of a Bounds.
//         /// </summary>
//         private static Vector3[] GetBoundsCorners(Bounds b)
//         {
//             Vector3 min = b.min;
//             Vector3 max = b.max;

//             return new Vector3[]
//             {
//                 new Vector3(min.x, min.y, min.z),
//                 new Vector3(min.x, min.y, max.z),
//                 new Vector3(min.x, max.y, min.z),
//                 new Vector3(min.x, max.y, max.z),
//                 new Vector3(max.x, min.y, min.z),
//                 new Vector3(max.x, min.y, max.z),
//                 new Vector3(max.x, max.y, min.z),
//                 new Vector3(max.x, max.y, max.z),
//             };
//         }

//         /// <summary>
//         /// Transforms a local-space bounds into another space by transforming all 8 corners,
//         /// then recomputing an axis-aligned bounds in the destination space.
//         /// </summary>
//         private static Bounds TransformBounds(Bounds bounds, Matrix4x4 localToTargetSpace)
//         {
//             Vector3[] corners = GetBoundsCorners(bounds);

//             Vector3 min = localToTargetSpace.MultiplyPoint3x4(corners[0]);
//             Vector3 max = min;

//             for (int i = 1; i < corners.Length; i++)
//             {
//                 Vector3 p = localToTargetSpace.MultiplyPoint3x4(corners[i]);
//                 min = Vector3.Min(min, p);
//                 max = Vector3.Max(max, p);
//             }

//             Bounds result = new Bounds();
//             result.SetMinMax(min, max);
//             return result;
//         }

//         /// <summary>
//         /// Gets smart local bounds from a GameObject via ToLocalBounds(...),
//         /// then converts them into refTransform local space.
//         /// </summary>
//         private static Bounds GetBoundsInRefSpace(
//             GameObject go,
//             Transform refTransform,
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             if (go == null) throw new ArgumentNullException(nameof(go));
//             if (refTransform == null) throw new ArgumentNullException(nameof(refTransform));

//             Bounds? localBoundsNullable = go.ToLocalBounds(
//                 useProxy: useProxy,
//                 boundsModeOverride: boundsModeOverride,
//                 sideSelectionOverride: sideSelectionOverride);

//             if (!localBoundsNullable.HasValue)
//             {
//                 throw new InvalidOperationException(
//                     $"Object '{go.name}' returned no local bounds.");
//             }

//             Bounds localBounds = localBoundsNullable.Value;
//             Matrix4x4 goLocalToRefLocal = refTransform.worldToLocalMatrix * go.transform.localToWorldMatrix;
//             return TransformBounds(localBounds, goLocalToRefLocal);
//         }

//         private static GameObject ResolveTargetForAxis(
//             string axisName,
//             GameObject axisTarget,
//             bool axisIncluded)
//         {
//             if (!axisIncluded)
//                 return axisTarget;

//             if (axisTarget == null)
//             {
//                 throw new ArgumentNullException(
//                     axisName,
//                     $"A target must be provided for included axis {axisName}.");
//             }

//             return axisTarget;
//         }

//         /// <summary>
//         /// Builds a synthetic ref-local bounds using X from targetX, Y from targetY, Z from targetZ.
//         /// Center and size are both taken per-axis from the corresponding target.
//         /// Offsets are applied after composition in ref-local units.
//         /// </summary>
//         private static Bounds BuildCompositeTargetBoundsInRefSpace(
//             Transform refTransform,
//             GameObject targetX,
//             GameObject targetY,
//             GameObject targetZ,
//             BoundsOffsets offsets,
//             bool includeX = true,
//             bool includeY = true,
//             bool includeZ = true,
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             if (refTransform == null) throw new ArgumentNullException(nameof(refTransform));
//             if (!includeX && !includeY && !includeZ)
//                 throw new ArgumentException("At least one axis must be included.");

//             targetX = ResolveTargetForAxis(nameof(targetX), targetX, includeX);
//             targetY = ResolveTargetForAxis(nameof(targetY), targetY, includeY);
//             targetZ = ResolveTargetForAxis(nameof(targetZ), targetZ, includeZ);

//             Bounds? bx = includeX
//                 ? GetBoundsInRefSpace(targetX, refTransform, useProxy, boundsModeOverride, sideSelectionOverride)
//                 : null;

//             Bounds? by = includeY
//                 ? GetBoundsInRefSpace(targetY, refTransform, useProxy, boundsModeOverride, sideSelectionOverride)
//                 : null;

//             Bounds? bz = includeZ
//                 ? GetBoundsInRefSpace(targetZ, refTransform, useProxy, boundsModeOverride, sideSelectionOverride)
//                 : null;

//             Vector3 center = Vector3.zero;
//             Vector3 size = Vector3.zero;

//             if (includeX)
//             {
//                 center.x = bx.Value.center.x;
//                 size.x = bx.Value.size.x;
//             }

//             if (includeY)
//             {
//                 center.y = by.Value.center.y;
//                 size.y = by.Value.size.y;
//             }

//             if (includeZ)
//             {
//                 center.z = bz.Value.center.z;
//                 size.z = bz.Value.size.z;
//             }

//             Bounds result = new Bounds(center, size);
//             return MeshOps.ApplyOffsets(result, offsets);
//         }

//         /// <summary>
//         /// Converts a delta expressed in refTransform local space into a position change on the transform.
//         /// Only included axes are applied.
//         /// </summary>
//         private static void TranslateTransformInRefSpace(
//             Transform tx,
//             Transform refTransform,
//             Vector3 deltaRef,
//             bool includeX,
//             bool includeY,
//             bool includeZ)
//         {
//             if (tx == null) throw new ArgumentNullException(nameof(tx));
//             if (refTransform == null) throw new ArgumentNullException(nameof(refTransform));

//             if (!includeX) deltaRef.x = 0f;
//             if (!includeY) deltaRef.y = 0f;
//             if (!includeZ) deltaRef.z = 0f;

//             Vector3 worldDelta = refTransform.TransformVector(deltaRef);

//             if (tx.parent != null)
//             {
//                 Vector3 parentLocalDelta = tx.parent.InverseTransformVector(worldDelta);
//                 tx.localPosition += parentLocalDelta;
//             }
//             else
//             {
//                 tx.position += worldDelta;
//             }
//         }

//         /// <summary>
//         /// Multiplies the current localScale by a uniform scalar.
//         /// </summary>
//         private static void ApplyUniformScaleMultiplier(Transform tx, float uniformScaleMultiplier)
//         {
//             if (tx == null) throw new ArgumentNullException(nameof(tx));

//             Vector3 s = tx.localScale;
//             tx.localScale = new Vector3(
//                 s.x * uniformScaleMultiplier,
//                 s.y * uniformScaleMultiplier,
//                 s.z * uniformScaleMultiplier);
//         }

//         /// <summary>
//         /// Uniformly scales a transform so its bounds fit the composite target bounds,
//         /// resolved through a shared reference transform.
//         ///
//         /// Offsets are measured in refTransform local units.
//         /// Positive offsets expand the target bounds; negative offsets shrink them.
//         ///
//         /// After scaling, the object is centered to the target bounds on included axes.
//         /// Returns the uniform scale multiplier that was applied.
//         /// </summary>
//         public static float UniformScaleToEncapsulate(
//             GameObject thingToResize,
//             GameObject targetX,
//             GameObject targetY,
//             GameObject targetZ,
//             Transform refTransform,
//             BoundsOffsets offsets,
//             bool includeX = true,
//             bool includeY = true,
//             bool includeZ = true,
//             string undoLabel = "Uniform Scale Transform To Fit",
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             if (thingToResize == null) throw new ArgumentNullException(nameof(thingToResize));
//             if (refTransform == null) throw new ArgumentNullException(nameof(refTransform));

//             if (!includeX && !includeY && !includeZ)
//                 throw new ArgumentException("At least one axis must be included.");

// #if UNITY_EDITOR
//             UnityEditor.Undo.RecordObject(thingToResize.transform, undoLabel);
// #endif

//             Bounds sourceBoundsRef = GetBoundsInRefSpace(
//                 thingToResize,
//                 refTransform,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);

//             Bounds targetBoundsRef = BuildCompositeTargetBoundsInRefSpace(
//                 refTransform,
//                 targetX,
//                 targetY,
//                 targetZ,
//                 offsets,
//                 includeX,
//                 includeY,
//                 includeZ,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);

//             float uniformScaleMultiplier = MeshOps.ComputeUniformScaleToEncapsulateBounds(
//                 sourceBoundsRef,
//                 targetBoundsRef,
//                 includeX,
//                 includeY,
//                 includeZ);

//             ApplyUniformScaleMultiplier(thingToResize.transform, uniformScaleMultiplier);

//             Bounds scaledBoundsRef = GetBoundsInRefSpace(
//                 thingToResize,
//                 refTransform,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);

//             Vector3 deltaRef = targetBoundsRef.center - scaledBoundsRef.center;

//             TranslateTransformInRefSpace(
//                 thingToResize.transform,
//                 refTransform,
//                 deltaRef,
//                 includeX,
//                 includeY,
//                 includeZ);

//             return uniformScaleMultiplier;
//         }

//         /// <summary>
//         /// Convenience overload with zero offsets.
//         /// </summary>
//         public static float UniformScaleToEncapsulate(
//             GameObject thingToResize,
//             GameObject targetX,
//             GameObject targetY,
//             GameObject targetZ,
//             Transform refTransform,
//             bool includeX = true,
//             bool includeY = true,
//             bool includeZ = true,
//             string undoLabel = "Uniform Scale Transform To Fit",
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             return UniformScaleToEncapsulate(
//                 thingToResize,
//                 targetX,
//                 targetY,
//                 targetZ,
//                 refTransform,
//                 BoundsOffsets.Zero,
//                 includeX,
//                 includeY,
//                 includeZ,
//                 undoLabel,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);
//         }

//         /// <summary>
//         /// Uniformly scales a transform so its bounds fit inside the composite target bounds
//         /// while leaving the given clearances on each side.
//         ///
//         /// Clearances are measured in refTransform local units.
//         /// Positive clearances reduce usable target size.
//         ///
//         /// After scaling, the object is centered in the usable target bounds on included axes.
//         /// Returns the uniform scale multiplier that was applied.
//         /// </summary>
//         public static float FitInsideBounds(
//             GameObject thingToResize,
//             GameObject targetX,
//             GameObject targetY,
//             GameObject targetZ,
//             Transform refTransform,
//             BoundsOffsets clearances,
//             bool includeX = true,
//             bool includeY = true,
//             bool includeZ = true,
//             string undoLabel = "Fit Transform Inside Bounds",
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             if (thingToResize == null) throw new ArgumentNullException(nameof(thingToResize));
//             if (refTransform == null) throw new ArgumentNullException(nameof(refTransform));

//             if (!includeX && !includeY && !includeZ)
//                 throw new ArgumentException("At least one axis must be included.");

// #if UNITY_EDITOR
//             UnityEditor.Undo.RecordObject(thingToResize.transform, undoLabel);
// #endif

//             Bounds sourceBoundsRef = GetBoundsInRefSpace(
//                 thingToResize,
//                 refTransform,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);

//             Bounds targetBoundsRef = BuildCompositeTargetBoundsInRefSpace(
//                 refTransform,
//                 targetX,
//                 targetY,
//                 targetZ,
//                 BoundsOffsets.Zero,
//                 includeX,
//                 includeY,
//                 includeZ,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);

//             Bounds usableTargetBoundsRef = MeshOps.ApplyOffsets(
//                 targetBoundsRef,
//                 MeshOps.NegateOffsets(clearances));

//             Vector3 usableSize = usableTargetBoundsRef.size;
//             if ((includeX && usableSize.x < 0f) ||
//                 (includeY && usableSize.y < 0f) ||
//                 (includeZ && usableSize.z < 0f))
//             {
//                 throw new InvalidOperationException(
//                     "FitInsideBounds clearances collapse the usable target bounds.");
//             }

//             float uniformScaleMultiplier = MeshOps.ComputeUniformScaleToEncapsulateBounds(
//                 sourceBoundsRef,
//                 usableTargetBoundsRef,
//                 includeX,
//                 includeY,
//                 includeZ);

//             ApplyUniformScaleMultiplier(thingToResize.transform, uniformScaleMultiplier);

//             Bounds scaledBoundsRef = GetBoundsInRefSpace(
//                 thingToResize,
//                 refTransform,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);

//             Vector3 deltaRef = usableTargetBoundsRef.center - scaledBoundsRef.center;

//             TranslateTransformInRefSpace(
//                 thingToResize.transform,
//                 refTransform,
//                 deltaRef,
//                 includeX,
//                 includeY,
//                 includeZ);

//             return uniformScaleMultiplier;
//         }

//         /// <summary>
//         /// Convenience overload with zero clearances.
//         /// </summary>
//         public static float FitInsideBounds(
//             GameObject thingToResize,
//             GameObject targetX,
//             GameObject targetY,
//             GameObject targetZ,
//             Transform refTransform,
//             bool includeX = true,
//             bool includeY = true,
//             bool includeZ = true,
//             string undoLabel = "Fit Transform Inside Bounds",
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             return FitInsideBounds(
//                 thingToResize,
//                 targetX,
//                 targetY,
//                 targetZ,
//                 refTransform,
//                 BoundsOffsets.Zero,
//                 includeX,
//                 includeY,
//                 includeZ,
//                 undoLabel,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);
//         }

//         /// <summary>
//         /// Uniformly scales a transform so its ref-space bounds match the requested dimensions
//         /// on the included axes.
//         ///
//         /// By default the current bounds center is preserved.
//         /// If targetCenterRefLocal is provided, the object is centered there after scaling
//         /// on the included axes.
//         ///
//         /// Returns the uniform scale multiplier that was applied.
//         /// </summary>
//         public static float UniformScaleToDimensions(
//             GameObject thingToResize,
//             Vector3 targetDimensionsRef,
//             Transform refTransform,
//             bool includeX = true,
//             bool includeY = true,
//             bool includeZ = true,
//             Vector3? targetCenterRefLocal = null,
//             string undoLabel = "Uniform Scale Transform To Dimensions",
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             if (thingToResize == null) throw new ArgumentNullException(nameof(thingToResize));
//             if (refTransform == null) throw new ArgumentNullException(nameof(refTransform));

//             if (!includeX && !includeY && !includeZ)
//                 throw new ArgumentException("At least one axis must be included.");

//             if ((includeX && targetDimensionsRef.x < 0f) ||
//                 (includeY && targetDimensionsRef.y < 0f) ||
//                 (includeZ && targetDimensionsRef.z < 0f))
//             {
//                 throw new ArgumentException("Included target dimensions must be non-negative.");
//             }

// #if UNITY_EDITOR
//             UnityEditor.Undo.RecordObject(thingToResize.transform, undoLabel);
// #endif

//             Bounds sourceBoundsRef = GetBoundsInRefSpace(
//                 thingToResize,
//                 refTransform,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);

//             Bounds targetBoundsRef = new Bounds(
//                 targetCenterRefLocal ?? sourceBoundsRef.center,
//                 targetDimensionsRef);

//             float uniformScaleMultiplier = MeshOps.ComputeUniformScaleToEncapsulateBounds(
//                 sourceBoundsRef,
//                 targetBoundsRef,
//                 includeX,
//                 includeY,
//                 includeZ);

//             ApplyUniformScaleMultiplier(thingToResize.transform, uniformScaleMultiplier);

//             Bounds scaledBoundsRef = GetBoundsInRefSpace(
//                 thingToResize,
//                 refTransform,
//                 useProxy,
//                 boundsModeOverride,
//                 sideSelectionOverride);

//             Vector3 desiredCenterRef = targetCenterRefLocal ?? sourceBoundsRef.center;
//             Vector3 deltaRef = desiredCenterRef - scaledBoundsRef.center;

//             TranslateTransformInRefSpace(
//                 thingToResize.transform,
//                 refTransform,
//                 deltaRef,
//                 includeX,
//                 includeY,
//                 includeZ);

//             return uniformScaleMultiplier;
//         }

//         /// <summary>
//         /// Convenience helper for the very common case:
//         /// uniform scale to a specific height in ref space.
//         /// Center is preserved unless targetCenterRefLocal is supplied.
//         /// </summary>
//         public static float UniformScaleToHeight(
//             GameObject thingToResize,
//             float targetHeightRef,
//             Transform refTransform,
//             Vector3? targetCenterRefLocal = null,
//             string undoLabel = "Uniform Scale Transform To Height",
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             return UniformScaleToDimensions(
//                 thingToResize,
//                 new Vector3(0f, targetHeightRef, 0f),
//                 refTransform,
//                 includeX: false,
//                 includeY: true,
//                 includeZ: false,
//                 targetCenterRefLocal: targetCenterRefLocal,
//                 undoLabel: undoLabel,
//                 useProxy: useProxy,
//                 boundsModeOverride: boundsModeOverride,
//                 sideSelectionOverride: sideSelectionOverride);
//         }

//         /// <summary>
//         /// Convenience helper for width in ref space.
//         /// </summary>
//         public static float UniformScaleToWidth(
//             GameObject thingToResize,
//             float targetWidthRef,
//             Transform refTransform,
//             Vector3? targetCenterRefLocal = null,
//             string undoLabel = "Uniform Scale Transform To Width",
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             return UniformScaleToDimensions(
//                 thingToResize,
//                 new Vector3(targetWidthRef, 0f, 0f),
//                 refTransform,
//                 includeX: true,
//                 includeY: false,
//                 includeZ: false,
//                 targetCenterRefLocal: targetCenterRefLocal,
//                 undoLabel: undoLabel,
//                 useProxy: useProxy,
//                 boundsModeOverride: boundsModeOverride,
//                 sideSelectionOverride: sideSelectionOverride);
//         }

//         /// <summary>
//         /// Convenience helper for depth in ref space.
//         /// </summary>
//         public static float UniformScaleToDepth(
//             GameObject thingToResize,
//             float targetDepthRef,
//             Transform refTransform,
//             Vector3? targetCenterRefLocal = null,
//             string undoLabel = "Uniform Scale Transform To Depth",
//             bool useProxy = false,
//             BoundsMode? boundsModeOverride = null,
//             SideBoundsSelection? sideSelectionOverride = null)
//         {
//             return UniformScaleToDimensions(
//                 thingToResize,
//                 new Vector3(0f, 0f, targetDepthRef),
//                 refTransform,
//                 includeX: false,
//                 includeY: false,
//                 includeZ: true,
//                 targetCenterRefLocal: targetCenterRefLocal,
//                 undoLabel: undoLabel,
//                 useProxy: useProxy,
//                 boundsModeOverride: boundsModeOverride,
//                 sideSelectionOverride: sideSelectionOverride);
//         }
//     }
// }