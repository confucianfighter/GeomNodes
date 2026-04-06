using UnityEngine;
using Converter = DLN.MathsConversions;
using Unity.Mathematics;
using DLN;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using UnityEngine.Animations;
using Unity.XR.CoreUtils;

namespace DLN
{
    public enum ResizeOrientation
    {
        ObjectToResize,
        ReferenceObject

    }
    public class SixtynineSliceScale : LayoutOp
    {
        [ButtonField(nameof(UpdateTree), "Update Tree")]
        [SerializeField] private bool updateTree = false;
        [ButtonField(nameof(Execute), "Run This Op")]
        [SerializeField] private bool executeNow = false; // dummy field for button attribute.
        [SerializeField] GameObject referenceObject;
        [ShowIfObjectAssigned(nameof(referenceObject), "Override Default Bounds Reporting", indentAmount: 12f)]
        [CheckboxLeft("Override How Reference Object Reports Bounds", 0f)]
        [SerializeField] private bool overrideBoundsReporting;
        [ShowIfBool(nameof(overrideBoundsReporting), true, "Reference Object Bounds Reporting", 12f)]
        public TargetObjectBoundsSettings referenceBoundsSettings = TargetObjectBoundsSettings.Empty;

        [SerializeField, NoFoldout(NoFoldoutHeaderMode.UnderlinedLabel)]
        public ResizeOptions resizeOptions = ResizeOptions.Init();
        [SerializeField] public float3 pivot = new Vector3(.5f, .5f, .5f);
        [SerializeField, HideInInspector] public List<GameObject> objectsToResize = new List<GameObject>();
        [SerializeField] public bool anchorToReferenceObject = false;
        [Serializable]
        private struct AnchorMoveArgs
        {
            public IncludeAxis axisToMove;
            public Vector3 anchor;
        }
        [ShowIfBool(nameof(anchorToReferenceObject), true)]
        [SerializeField]
        private AnchorMoveArgs anchorToTargetParams = new AnchorMoveArgs
        {
            anchor = new Vector3(.5f, .5f, .5f),
            axisToMove = new IncludeAxis { x = true, y = true, z = true }

        };


#if UNITY_EDITOR
#endif
        private Bounds? _referenceBounds;
        void OnValidate()
        {
#if UNITY_EDITOR
#endif
            Execute();
        }
        public void UpdateTree()
        {
            if (!C3DLS_Utils.TryExecuteFirstDepthFirstExecutor(this.transform, includeSelf: true))
            {
                Debug.LogError("Could not find Depth first Executor");
            }
        }
        public Transform GetRefTransform()
        {
            if (referenceBoundsSettings.boundsOrientation == ResizeOrientation.ReferenceObject)
            {
                if (referenceObject != null)
                {
                    return referenceObject.transform;
                }
            }
            return this.transform;
        }


        private IEnumerable<GameObject> GetObjectsToResize()
        {
            if (objectsToResize.Count == 0)
            {
                objectsToResize.Add(this.gameObject);
            }
            foreach (var go in objectsToResize)
            {
                yield return go;
            }
        }

        [ContextMenu("Execute")]
        public override void Execute()
        {

            Vector3 _getAnchor(GameObject target)
            {
                Vector3 result = Vector3.zero;
                if (referenceObject == null) return result;
                Bnds.InterpolateBounds(
                    @object: anchorToReferenceObject ? referenceObject : target,
                    interpVec: anchorToTargetParams.anchor,
                    outputCoordSpace: Space.World,
                    overrides: referenceBoundsSettings.boundsSettings,
                    result: out result
                );
                return result;
            }


            foreach (var objectToResize in GetObjectsToResize())
            {
                if (resizeOptions.resizeMeshOnly)
                {
                    objectToResize.transform.SetScale(Vector3.one,
                        preserveChildPositions: true,
                        preserveChildScales: true,
                        uniformScaleChildren: false);

                }
                var targetSize = resolveDimensionToResizeTo(objectToResize);
                var startAnchorPos = _getAnchor(objectToResize);
                var resizeArgs = new ResizeArgs
                {
                    target = objectToResize,
                    pivot = pivot,
                    size = targetSize

                };

                if (resizeOptions.resizeMeshOnly)
                {
                    resizeArgs.resizeMethod = ResizeMethod.Mesh;

                }
                else
                {
                    resizeArgs.resizeMethod = ResizeMethod.Scale;
                }
                Resizer.Resize(args: resizeArgs);

                if (anchorToReferenceObject && referenceObject != null)
                {
                    AnchorToTarget(
                        objectToMove: objectToResize,
                        objectToAnchorTo: referenceObject,
                        includeAxis: anchorToTargetParams.axisToMove);
                }


            }
        }
        private void Scale(GameObject target, Bounds targetBounds, ResizeArgs args)
        {
            var size = args.size;
            var currentSize = targetBounds.size;
            var currentScale = target.transform.localScale;
            var scaleFactor = size.DivideBy(currentSize);
            target.transform.localScale = scaleFactor.Mul(currentScale);
            Debug.Log($"scaleFactor {scaleFactor}");
            var inverseScale = Vector3.one.DivideBy(scaleFactor);
            Debug.Log($"InverseScale {inverseScale}");
            scaleFactor = scaleFactor.ConstrainUniform().Mul(inverseScale);
            foreach (Transform t in target.transform)
            {
                t.localScale = t.localScale.Mul(scaleFactor);
            }
        }

        private void AnchorToTarget(GameObject objectToMove, GameObject objectToAnchorTo, IncludeAxis includeAxis)
        {
            Vector3 finalPosition = objectToMove.transform.position;
            Vector3 targetPosition;

            Bnds.InterpolateBounds(
                @object: objectToAnchorTo,
                interpVec: anchorToTargetParams.anchor,
                overrides: referenceBoundsSettings.boundsSettings,
                result: out targetPosition
            );

            finalPosition = finalPosition.MixTrue(trueValues: targetPosition, includeAxis: includeAxis);
            objectToMove.transform.position = finalPosition;
        }


        public Vector3 resolveDimensionToResizeTo(GameObject target)
        {
            Vector3 dimensions = Vector3.zero;
            Bounds? referenceBounds = null;
            if (referenceObject != null)
            {
                referenceBounds = referenceObject.ToBounds(refOrigin: target.transform, refRotation: GetRefTransform(), refScale: target.transform, overrides: referenceBoundsSettings.boundsSettings);
            }
            var targetBounds = target.ToBounds(refOrigin: target.transform, refRotation: GetRefTransform(), refScale: target.transform);

            if (targetBounds == null)
            {
                Debug.Log($"Object to resize is reporting no bounds, check the {typeof(SmartBounds)} settings have includeSelf, and includeChildren set to false, or if basing bounds off of empty transforms, make sure includeselfEvenIfEmpty is set to true on those gameObjects");
            }

            float? targetX = targetBounds.HasValue ? targetBounds.Value.size.x : null;
            float? targetY = targetBounds.HasValue ? targetBounds.Value.size.y : null;
            float? targetZ = targetBounds.HasValue ? targetBounds.Value.size.z : null;


            float? refX = referenceBounds.HasValue ? referenceBounds.Value.size.x : null;
            float? refY = referenceBounds.HasValue ? referenceBounds.Value.size.y : null;
            float? refZ = referenceBounds.HasValue ? referenceBounds.Value.size.z : null;


            var xDim = resolveAxis(resizeOptions.finalSizeOptions.x,
                referenceBoundsSize: refX,
                thingToResizeBoundsSize: targetX);
            var yDim = resolveAxis(resizeOptions.finalSizeOptions.y,
                referenceBoundsSize: refY,
                thingToResizeBoundsSize: targetY);

            var zDim = resolveAxis(resizeOptions.finalSizeOptions.z,
                referenceBoundsSize: refZ,
                thingToResizeBoundsSize: targetZ);
            dimensions.x = xDim;
            dimensions.y = yDim;
            dimensions.z = zDim;
            if (resizeOptions.fitUniformly)
            {
                dimensions = Maths.ConstrainByUniformFit(
                    desiredDimensions: dimensions,
                    objectToResizeBounds: targetBounds.Value,
                    axisToConstrainTo: resizeOptions.uniformFitOptions.axisToConstrainTo,
                    axisToConstrain: resizeOptions.uniformFitOptions.axisToConstrain

                );

            }

            Debug.Log($"final dimensions to resize to after calculations: {dimensions}");
            var finalDimensions = targetBounds.Value.size.MixTrue(dimensions, resizeOptions.axisToActuallyResize);
            if (finalDimensions.x < Constants.Epsilon) finalDimensions.x = Constants.Epsilon;
            if (finalDimensions.y < Constants.Epsilon) finalDimensions.y = Constants.Epsilon;
            if (finalDimensions.z < Constants.Epsilon) finalDimensions.z = Constants.Epsilon;


            return finalDimensions;
        }
        private float resolveAxis(MeasureOptions options, float? referenceBoundsSize, float? thingToResizeBoundsSize)
        {
            float size = Constants.Epsilon;
            switch (options.measureSource)
            {
                case MeasureSource.Fixed:

                    size = options.fixedSize;
                    break;
                case MeasureSource.ReferenceObject:
                    size = referenceBoundsSize.HasValue ? referenceBoundsSize.Value : options.fixedSize;
                    break;
                case MeasureSource.ObjectToResize:
                    size = thingToResizeBoundsSize.Value;
                    break;
            }
            return size;
        }
    }
}