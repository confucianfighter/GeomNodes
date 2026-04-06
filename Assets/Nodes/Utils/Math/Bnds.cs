using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using g4;
using Unity.XR.CoreUtils;
using DLN.Extensions;
namespace DLN
{
    public enum Corner
    {
        FrontBottomLeft,
        FrontTopLeft,
        FrontTopRight,
        FrontBottomRight,
        BackBottomLeft,
        BackTopLeft,
        BackTopRight,
        BackBottomRight
    }

    public enum Side
    {
        Front,
        Back,
        Left,
        Right,
        Top,
        Bottom
    }

    public enum SideZ
    {
        Front,
        Back
    }

    public enum SideY
    {
        Top,
        Bottom
    }

    public enum SideX
    {
        Left,
        Right
    }

    public enum Space
    {
        Local,
        World,
        Parent,
        Reference
    }

    public static class Bnds
    {
        public static void GetG4OBB(
            UnityEngine.Object @object,
            List<Vector3> corners,
            out Vector3 size
        )
        {
            var cobox = new ContOrientedBox3(corners.Select(c => new Vector3d(c.x, c.y, c.z)));
            var ext = cobox.Box.Extent;
            var width = ext.x * 2;
            var height = ext.y * 2;
            var depth = ext.z * 2;
            size = new Vector3((float)width, (float)height, (float)depth);
        }

        public static bool GetLocalRadialBounds(
            UnityEngine.Object @object,
            out float radius,
            out float centroid,
            OptionalBoundsSettings overrides = default
        )
        {
            var bounds = @object.ToLocalBounds(overrides);

            if (!bounds.HasValue)
            {
                radius = 0f;
                centroid = 0f;
                return false;
            }

            radius = bounds.Value.extents.magnitude;
            centroid = bounds.Value.center.magnitude;
            return true;
        }

        public static bool GetWorldRadialBounds(
            UnityEngine.Object @object,
            out float radius,
            out Vector3 centroid
        )
        {
            var bounds = @object.ToWorldBounds();
            radius = bounds.extents.magnitude;
            centroid = bounds.center;
            return true;
        }

        public static bool GetLocalBoundsCorners(
            UnityEngine.Object @object,
            out Vector3[] corners,
            OptionalBoundsSettings overrides = default
        )
        {
            var bounds = @object.ToLocalBounds(overrides);

            if (!bounds.HasValue || bounds.Value.size == Vector3.zero)
            {
                corners = new Vector3[0];
                return false;
            }

            Vector3 c = bounds.Value.center;
            Vector3 e = bounds.Value.extents;

            var frontBottomLeft = c + new Vector3(-e.x, -e.y, e.z);
            var frontTopLeft = c + new Vector3(-e.x, e.y, e.z);
            var frontTopRight = c + new Vector3(e.x, e.y, e.z);
            var frontBottomRight = c + new Vector3(e.x, -e.y, e.z);
            var backBottomLeft = c + new Vector3(-e.x, -e.y, -e.z);
            var backTopLeft = c + new Vector3(-e.x, e.y, -e.z);
            var backTopRight = c + new Vector3(e.x, e.y, -e.z);
            var backBottomRight = c + new Vector3(e.x, -e.y, -e.z);

            corners = new Vector3[]
            {
                frontBottomLeft, frontTopLeft, frontTopRight, frontBottomRight,
                backBottomLeft, backTopLeft, backTopRight, backBottomRight
            };

            return true;
        }

        public static bool GetOrientedBounds(
 UnityEngine.Object refSpace,
    UnityEngine.Object @object,
    out Bounds? bounds,
    Space scaleSpace = Space.World,
    bool log = false,
    OptionalBoundsSettings overrides = default
)
        {
            bounds = null;

            if (!GetOrientedBounds(
                refSpace: refSpace,
                @object: @object,
                center: out Vector3 center,
                extents: out _,
                size: out Vector3 size,
                min: out _,
                max: out _,
                log: log,
                overrides: overrides
            ))
            {
                return false;
            }

            bounds = new Bounds(center, size);
            return true;
        }
        public static bool GetOrientedBounds(
    Transform reference,
    UnityEngine.Object @object,
    out Bounds bounds,
    bool log = false,
    OptionalBoundsSettings overrides = default
)
        {
            return GetOrientedBounds(
                reference,
                reference,
                reference,
                @object,
                out bounds,
                log,
                overrides
            );
        }
        public static bool GetOrientedBounds(
     Matrix4x4 trs,
     UnityEngine.Object @object,
     out Bounds bounds,
     bool log = false,
     OptionalBoundsSettings overrides = default
 )
        {
            bounds = default;

            if (!GetLocalBoundsCorners(@object, out var corners, overrides))
                return false;

            var tx = @object.AsTransform();
            if (tx == null)
                return false;

            Matrix4x4 localToMeasure = trs.inverse * tx.localToWorldMatrix;

            for (int i = 0; i < corners.Length; i++)
                corners[i] = localToMeasure.MultiplyPoint3x4(corners[i]);

            bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(corners[i]);

            if (log)
            {
                Debug.Log(
                    $"Bounds Size: {bounds.size.x}, {bounds.size.y}, {bounds.size.z} " +
                    $"Center: {bounds.center.x}, {bounds.center.y}, {bounds.center.z}"
                );
            }

            return true;
        }

        public static bool GetOrientedBounds(
            Transform refOrigin,
            Transform refRotation,
            Transform refScale,
            UnityEngine.Object @object,
            out Bounds bounds,
            bool log = false,
            OptionalBoundsSettings overrides = default
        )
        {
            bounds = default;

            if (refOrigin == null || refRotation == null || refScale == null)
                return false;

            Matrix4x4 measureWorld = Matrix4x4.TRS(
                refOrigin.position,
                refRotation.rotation,
                refScale.lossyScale
            );

            return GetOrientedBounds(
                measureWorld,
                @object,
                out bounds,
                log,
                overrides
            );
        }

        public static bool GetOrientedBounds(
 UnityEngine.Object refSpace,
            UnityEngine.Object @object,
            out Vector3 center,
            out Vector3 extents,
            out Vector3 size,
            out Vector3 min,
            out Vector3 max,
            bool log = false,
            OptionalBoundsSettings overrides = default
        )
        {
            center = Vector3.zero;
            extents = Vector3.zero;
            size = Vector3.zero;
            min = Vector3.zero;
            max = Vector3.zero;

            if (!GetLocalBoundsCorners(@object, out var corners, overrides))
                return false;

            var tx = @object.AsTransform();
            var refTx = refSpace.AsTransform();

            if (tx == null || refTx == null)
                return false;

            Matrix4x4 localToRef = refTx.worldToLocalMatrix * tx.localToWorldMatrix;


            for (int i = 0; i < corners.Length; i++)
                corners[i] = localToRef.MultiplyPoint3x4(corners[i]);

            var refBounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                refBounds.Encapsulate(corners[i]);


            center = refBounds.center;
            extents = refBounds.extents;
            size = refBounds.size;
            min = refBounds.min;
            max = refBounds.max;

            if (log)
                Debug.Log($"Ref Bounds Size: {size.x}, {size.y}, {size.z} Center: {center.x}, {center.y}, {center.z}");

            return true;
        }

        public static void InterpolateWorldBounds(
            UnityEngine.Object @object,
            float t_x,
            float t_y,
            float t_z,
            out Vector3 position
        )
        {
            var bounds = @object.ToWorldBounds();
            var min = bounds.min;
            var size = bounds.size;

            position = min + new Vector3(
                size.x * t_x,
                size.y * t_y,
                size.z * t_z
            );
        }

        public static void GetBoundsCorners(
            Space space,
 UnityEngine.Object refSpace,
            UnityEngine.Object @object,
            out Vector3[] corners,
            OptionalBoundsSettings overrides = default
        )
        {
            GetLocalBoundsCorners(@object, out corners, overrides);

            if (space == Space.Local)
                return;

            var tx = @object.AsTransform();
            tx.TransformPoints(corners);

            if (space == Space.Reference)
            {
                var refTx = refSpace.AsTransform();
                refTx.InverseTransformPoints(corners);
            }
            else if (space == Space.Parent)
            {
                var parentTx = tx.parent;
                if (parentTx == null)
                    throw new System.ArgumentNullException(nameof(tx), "Transform has no parent when using Space.Parent.");

                parentTx.InverseTransformPoints(corners);
            }
        }

        public static void SetSmartBoundsSettings(
            UnityEngine.Object @object,
            bool includeSelf = true,
            bool includeChildren = true
        )
        {
            var sb = @object.ToSmartBounds();
            sb.SetIncludeSelf(includeSelf);
            sb.SetIncludeChildren(includeChildren);
        }

        public static void GetWorldBounds(
            UnityEngine.Object @object,
            out Vector3 center,
            out Vector3 min,
            out Vector3 max,
            out Vector3 size
        )
        {
            var bounds = @object.ToWorldBounds();
            center = bounds.center;
            min = bounds.min;
            max = bounds.max;
            size = bounds.size;
        }


        public static bool OBB(
 UnityEngine.Object refSpace,
            UnityEngine.Object @object,
            out Vector3 center,
            out Vector3 extents,
            out Vector3 size,
            out Vector3 min,
            out Vector3 max,
            bool log = false
        )
        {
            if (!(@object is Component objComp) || !(refSpace is Component refComp))
            {
                center = extents = size = min = max = Vector3.zero;
                Debug.LogWarning("Invalid refSpace or object. Must be a Component.");
                return false;
            }

            var objTx = objComp.transform;
            var refTx = refComp.transform;

            Vector3 localCenter;
            Vector3 localSize;

            if (objComp.TryGetComponent<BoxCollider>(out var box))
            {
                localCenter = box.center;
                localSize = box.size;
            }
            else if (objComp.TryGetComponent<MeshRenderer>(out var mr))
            {
                localCenter = mr.localBounds.center;
                localSize = mr.localBounds.size;
            }
            else
            {
                center = extents = size = min = max = Vector3.zero;
                Debug.LogWarning("Object has no BoxCollider or MeshRenderer for bounds.");
                return false;
            }

            Vector3 worldCenter = objTx.TransformPoint(localCenter);
            center = refTx.InverseTransformPoint(worldCenter);

            Matrix4x4 localToWorld = objTx.localToWorldMatrix;
            Matrix4x4 worldToRef = refTx.worldToLocalMatrix;
            Matrix4x4 composite = worldToRef * localToWorld;

            Matrix4x4 m = composite;
            m.m00 = Mathf.Abs(m.m00); m.m01 = Mathf.Abs(m.m01); m.m02 = Mathf.Abs(m.m02);
            m.m10 = Mathf.Abs(m.m10); m.m11 = Mathf.Abs(m.m11); m.m12 = Mathf.Abs(m.m12);
            m.m20 = Mathf.Abs(m.m20); m.m21 = Mathf.Abs(m.m21); m.m22 = Mathf.Abs(m.m22);

            Vector3 halfLocal = localSize * 0.5f;
            Vector3 halfRef = new Vector3(
                m.m00 * halfLocal.x + m.m01 * halfLocal.y + m.m02 * halfLocal.z,
                m.m10 * halfLocal.x + m.m11 * halfLocal.y + m.m12 * halfLocal.z,
                m.m20 * halfLocal.x + m.m21 * halfLocal.y + m.m22 * halfLocal.z
            );

            extents = halfRef;
            size = halfRef * 2f;
            min = center - halfRef;
            max = center + halfRef;

            if (log)
                Debug.Log($"Oriented Bounds Size: {size.x}, {size.y}, {size.z} Center: {center.x}, {center.y}, {center.z}");

            return true;
        }

        public static void GetLocalOrientedWorldBoundsSize(
 UnityEngine.Object transform,
            out Vector3 sizeXVector,
            out Vector3 sizeYVector,
            out Vector3 sizeZVector,
            OptionalBoundsSettings overrides = default
        )
        {
            var tx = transform.AsTransform();
            var bounds = tx.ToLocalBounds(overrides);

            if (!bounds.HasValue)
            {
                sizeXVector = Vector3.zero;
                sizeYVector = Vector3.zero;
                sizeZVector = Vector3.zero;
                return;
            }

            Vector3 size = bounds.Value.size;

            sizeXVector = tx.TransformVector(new Vector3(size.x, 0, 0));
            sizeYVector = tx.TransformVector(new Vector3(0, size.y, 0));
            sizeZVector = tx.TransformVector(new Vector3(0, 0, size.z));
        }
        public static void InterpolateBounds(
            UnityEngine.Object @object,
            Vector3 interpVec,
            out Vector3 result,
            Space outputCoordSpace = Space.World,
            OptionalBoundsSettings overrides = default)
        {
            Bnds.InterpolateBounds(
                @object: @object,
                t_x: interpVec.x,
                t_y: interpVec.y,
                t_z: interpVec.z,
                result: out result,
                outputCoordSpace: outputCoordSpace,
                overrides: overrides
            );
        }

        public static void InterpolateBounds(
            UnityEngine.Object @object,
            float t_x,
            float t_y,
            float t_z,
            out Vector3 result,
            Space outputCoordSpace = Space.World,
            OptionalBoundsSettings overrides = default
        )
        {
            var tx = @object.AsTransform();
            var bounds = @object.ToLocalBounds(overrides);

            if (!bounds.HasValue)
                bounds = new Bounds(center: @object.ToLocalPosition(), Vector3.zero);

            Vector3 min = bounds.Value.min;
            Vector3 size = bounds.Value.size;

            result = min + new Vector3(
                size.x * t_x,
                size.y * t_y,
                size.z * t_z
            );

            result = result.PosFromLocalTo(
                space: outputCoordSpace,
                localTransform: tx,
                refTransform: null);
        }



        public static void GetBoundsCorner(
            Space space,
 UnityEngine.Object obj,
            out Vector3 corner,
            out Vector3 norm,
            SideX sideX = SideX.Left,
            SideY sideY = SideY.Bottom,
            SideZ sideZ = SideZ.Front,
 UnityEngine.Object refTransform = null,
            OptionalBoundsSettings overrides = default
        )
        {
            var tx = obj.AsTransform();
            var bounds = tx.ToLocalBounds(overrides);

            if (!bounds.HasValue)
            {
                corner = Vector3.zero;
                norm = Vector3.zero;
                return;
            }

            Vector3 c = bounds.Value.center;
            Vector3 e = bounds.Value.extents;
            corner = c;

            switch (sideX)
            {
                case SideX.Left:
                    corner.x -= e.x;
                    c.x -= e.x;
                    break;
                case SideX.Right:
                    corner.x += e.x;
                    c.x += e.x;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(sideX), sideX, null);
            }

            switch (sideY)
            {
                case SideY.Top:
                    corner.y += e.y;
                    c.y += e.y;
                    break;
                case SideY.Bottom:
                    corner.y -= e.y;
                    c.y -= e.y;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(sideY), sideY, null);
            }

            switch (sideZ)
            {
                case SideZ.Front:
                    corner.z += e.z;
                    c.z += e.z;
                    break;
                case SideZ.Back:
                    corner.z -= e.z;
                    c.z -= e.z;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(sideZ), sideZ, null);
            }

            norm = corner - c;
            norm.Normalize();

            corner = corner.PosFromLocalTo(
                space: space,
                refTransform: refTransform.AsTransform(),
                localTransform: tx
            );

            norm = norm.DirFromLocalTo(
                space: space,
                refTransform: refTransform.AsTransform(),
                localTransform: tx
            );
        }

        public static void GetBoundsPanel(
            out Vector3 centroid,
            out Vector3 normal,
            out List<Vector3> corners,
            UnityEngine.Object @object,
            Space space = Space.World,
            Side side = Side.Front,
 UnityEngine.Object refTransform = null,
            OptionalBoundsSettings overrides = default
        )
        {
            var objTx = @object.AsTransform();
            var bounds = objTx.ToLocalBounds(overrides);

            if (!bounds.HasValue)
            {
                centroid = Vector3.zero;
                normal = Vector3.forward;
                corners = new List<Vector3>();
                return;
            }

            Vector3 c = bounds.Value.center;
            Vector3 e = bounds.Value.extents;

            switch (side)
            {
                case Side.Front:
                    centroid = c + new Vector3(0, 0, e.z);
                    normal = Vector3.forward;
                    corners = new List<Vector3>
                    {
                        c + new Vector3(-e.x, -e.y, e.z),
                        c + new Vector3(-e.x,  e.y, e.z),
                        c + new Vector3( e.x,  e.y, e.z),
                        c + new Vector3( e.x, -e.y, e.z)
                    };
                    break;

                case Side.Back:
                    centroid = c + new Vector3(0, 0, -e.z);
                    normal = Vector3.back;
                    corners = new List<Vector3>
                    {
                        c + new Vector3(-e.x, -e.y, -e.z),
                        c + new Vector3(-e.x,  e.y, -e.z),
                        c + new Vector3( e.x,  e.y, -e.z),
                        c + new Vector3( e.x, -e.y, -e.z)
                    };
                    break;

                case Side.Left:
                    centroid = c + new Vector3(-e.x, 0, 0);
                    normal = Vector3.left;
                    corners = new List<Vector3>
                    {
                        c + new Vector3(-e.x, -e.y,  e.z),
                        c + new Vector3(-e.x,  e.y,  e.z),
                        c + new Vector3(-e.x,  e.y, -e.z),
                        c + new Vector3(-e.x, -e.y, -e.z)
                    };
                    break;

                case Side.Right:
                    centroid = c + new Vector3(e.x, 0, 0);
                    normal = Vector3.right;
                    corners = new List<Vector3>
                    {
                        c + new Vector3(e.x, -e.y,  e.z),
                        c + new Vector3(e.x,  e.y,  e.z),
                        c + new Vector3(e.x,  e.y, -e.z),
                        c + new Vector3(e.x, -e.y, -e.z)
                    };
                    break;

                case Side.Top:
                    centroid = c + new Vector3(0, e.y, 0);
                    normal = Vector3.up;
                    corners = new List<Vector3>
                    {
                        c + new Vector3(-e.x, e.y,  e.z),
                        c + new Vector3(-e.x, e.y, -e.z),
                        c + new Vector3( e.x, e.y, -e.z),
                        c + new Vector3( e.x, e.y,  e.z)
                    };
                    break;

                case Side.Bottom:
                    centroid = c + new Vector3(0, -e.y, 0);
                    normal = Vector3.down;
                    corners = new List<Vector3>
                    {
                        c + new Vector3(-e.x, -e.y,  e.z),
                        c + new Vector3(-e.x, -e.y, -e.z),
                        c + new Vector3( e.x, -e.y, -e.z),
                        c + new Vector3( e.x, -e.y,  e.z)
                    };
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(side), side, null);
            }

            switch (space)
            {
                case Space.Local:
                    break;

                case Space.World:
                    for (int i = 0; i < corners.Count; i++)
                        corners[i] = objTx.TransformPoint(corners[i]);

                    centroid = objTx.TransformPoint(centroid);
                    normal = objTx.TransformDirection(normal);
                    break;

                case Space.Parent:
                    var parentTx = objTx.parent;
                    if (parentTx == null)
                        throw new System.ArgumentNullException(nameof(@object), "Parent transform cannot be null when using Space.Parent.");

                    for (int i = 0; i < corners.Count; i++)
                    {
                        var corner = objTx.TransformPoint(corners[i]);
                        corners[i] = parentTx.InverseTransformPoint(corner);
                    }

                    centroid = parentTx.InverseTransformPoint(objTx.TransformPoint(centroid));
                    normal = parentTx.InverseTransformDirection(objTx.TransformDirection(normal));
                    break;

                case Space.Reference:
                    if (refTransform == null)
                        throw new System.ArgumentNullException(nameof(refTransform), "Reference transform cannot be null when using Space.Reference.");

                    var referenceTx = refTransform.AsTransform();
                    for (int i = 0; i < corners.Count; i++)
                    {
                        var corner = objTx.TransformPoint(corners[i]);
                        corners[i] = referenceTx.InverseTransformPoint(corner);
                    }

                    centroid = referenceTx.InverseTransformPoint(objTx.TransformPoint(centroid));
                    normal = referenceTx.InverseTransformDirection(objTx.TransformDirection(normal));
                    break;

                default:
                    throw new System.ArgumentOutOfRangeException(nameof(space), space, null);
            }
        }


        public static bool RayToTarget(
            UnityEngine.Object rayTransfrom,
            UnityEngine.Object targetTransform,
            out Vector3 hitPoint,
            out Vector3 hitNormal,
            out float hitDistance,
            float maxDistance = 100
        )
        {
            var _targetTx = targetTransform.AsTransform();
            var _targetCollider = _targetTx.GetComponent<Collider>();

            hitPoint = Vector3.zero;
            hitNormal = Vector3.zero;
            hitDistance = 0f;

            var rayTx = rayTransfrom.AsTransform();
            var ray = new Ray(rayTx.position, rayTx.forward);

            if (_targetCollider.Raycast(ray: ray, hitInfo: out var hit, maxDistance: maxDistance))
            {
                hitPoint = hit.point;
                hitNormal = hit.normal;
                hitDistance = hit.distance;
                return true;
            }

            return false;
        }

        public static bool FitToBox(
 UnityEngine.Object boxObject,
 UnityEngine.Object targetObject,
            bool setParent = true,
            bool resetLocalRotation = true,
            bool resetLocalScaleToOne = false,
            float padding = 0f,
            float postScaleMultiplier = 1f,
            bool log = false,
            OptionalBoundsSettings overrides = default
        )
        {
            var boxTx = boxObject.AsTransform();
            var targetTx = targetObject.AsTransform();

            if (boxTx == null) throw new System.ArgumentNullException(nameof(boxObject), "boxObject has no Transform.");
            if (targetTx == null) throw new System.ArgumentNullException(nameof(targetObject), "targetObject has no Transform.");

            var boxCol = boxTx.GetComponent<BoxCollider>();
            if (boxCol == null)
                throw new System.ArgumentNullException(nameof(boxObject), "boxObject must have a BoxCollider.");

            if (setParent)
                targetTx.SetParent(boxTx, worldPositionStays: false);

            if (resetLocalRotation)
                targetTx.localRotation = Quaternion.identity;

            if (resetLocalScaleToOne)
                targetTx.localScale = Vector3.one;

            if (!GetOrientedBounds(
                refSpace: boxTx,
                @object: targetTx,
                center: out var objCenterRef,
                extents: out var objExtentsRef,
                size: out var objSizeRef,
                min: out _,
                max: out _,
                log: false,
                overrides: overrides
            ))
            {
                if (log) Debug.LogWarning("FitToBox: target has no bounds to fit.");
                return false;
            }

            var avail = boxCol.size - Vector3.one * (padding * 2f);
            avail.x = Mathf.Max(avail.x, 0.0001f);
            avail.y = Mathf.Max(avail.y, 0.0001f);
            avail.z = Mathf.Max(avail.z, 0.0001f);

            objSizeRef.x = Mathf.Max(objSizeRef.x, 0.0001f);
            objSizeRef.y = Mathf.Max(objSizeRef.y, 0.0001f);
            objSizeRef.z = Mathf.Max(objSizeRef.z, 0.0001f);

            float s = 1f;
            {
                float rx = avail.x / objSizeRef.x;
                float ry = avail.y / objSizeRef.y;
                float rz = avail.z / objSizeRef.z;
                s = Mathf.Min(rx, ry, rz) * postScaleMultiplier;
                targetTx.localScale = targetTx.localScale * s;
            }

            if (!GetOrientedBounds(
                refSpace: boxTx,
                @object: targetTx,
                center: out var newCenterRef,
                extents: out _,
                size: out _,
                min: out _,
                max: out _,
                log: false,
                overrides: overrides
            ))
            {
                return false;
            }

            Vector3 delta = boxCol.center - newCenterRef;

            if (targetTx.parent == boxTx)
            {
                targetTx.localPosition += delta;
            }
            else
            {
                var parentTx = targetTx.parent;
                if (parentTx == null)
                {
                    targetTx.position += boxTx.TransformVector(delta);
                }
                else
                {
                    Vector3 deltaWorld = boxTx.TransformVector(delta);
                    Vector3 deltaParentLocal = parentTx.InverseTransformVector(deltaWorld);
                    targetTx.localPosition += deltaParentLocal;
                }
            }

            if (log)
                Debug.Log($"FitToBox: scale={s} padding={padding} avail={avail}");

            return true;
        }

        public struct SpanFromPoint
        {
            public float Backward;
            public float Forward;
            public float Total => Backward + Forward;

            public SpanFromPoint(float backward, float forward)
            {
                Backward = backward;
                Forward = forward;
            }
        }

        public static bool TryGetSpan(
            Bounds bounds,
            Vector3 interpBoundsStartPoint,
            Vector3 direction,
            out SpanFromPoint span)
        {
            span = default;

            if (direction.sqrMagnitude < 0.000001f)
                return false;

            Vector3 start = bounds.Interpolate(interpBoundsStartPoint);
            Vector3 dir = direction.normalized;

            // Since bounds are axis-aligned in the reference frame,
            // the furthest extent along any direction can be found
            // by selecting min/max on each axis according to the sign.
            Vector3 forwardCorner = new Vector3(
                dir.x >= 0f ? bounds.max.x : bounds.min.x,
                dir.y >= 0f ? bounds.max.y : bounds.min.y,
                dir.z >= 0f ? bounds.max.z : bounds.min.z
            );

            Vector3 backwardCorner = new Vector3(
                dir.x >= 0f ? bounds.min.x : bounds.max.x,
                dir.y >= 0f ? bounds.min.y : bounds.max.y,
                dir.z >= 0f ? bounds.min.z : bounds.max.z
            );

            float forward = Vector3.Dot(forwardCorner - start, dir);
            float backward = Vector3.Dot(start - backwardCorner, dir);

            if (forward < 0f) forward = 0f;
            if (backward < 0f) backward = 0f;

            span = new SpanFromPoint(backward, forward);
            return true;
        }
    }
}
