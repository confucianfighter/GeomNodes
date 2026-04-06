// using System;
// using System.Collections.Generic;
// using System.Drawing;
// using NUnit.Framework;
// using Unity.Collections;
// using UnityEngine;
// using UnityEngine.XR.Interaction.Toolkit;
// using Casters = UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;


// namespace DLN
// {
//     [AddComponentMenu("XR/Interaction/Plane Far Caster (ICurveInteractionCaster) with OBB")]

//     public class PlaneFarCaster : MonoBehaviour, Casters.ICurveInteractionCaster
//     {
//         private const float k_MinHitDistance = 0.001f;

//         public float neutralLineLength = 0.2f;
//         public float snapBorders = 0.15f;
//         public float maxAssertionNumber = 200f;
//         public Transform castOrigin { get => transform; set { } }
//         public float castDistance = 10f;
//         public Plane constraintPlane = new Plane(Vector3.up, Vector3.zero);
//         public Transform hitPointDebugMarker;
//         public Transform effectiveCastOrigin => castOrigin;
//         public bool isInitialized { get; private set; }

//         public NativeArray<Vector3> samplePoints { get; private set; }
//         public Vector3 lastSamplePoint => samplePoints[samplePoints.Length - 1];
//         public RaycastHit? lastHit;


//         void Awake()
//         {
//             samplePoints = new NativeArray<Vector3>(2, Allocator.Persistent);
//             isInitialized = true;
//         }

//         void OnDestroy()
//         {
//             if (samplePoints.IsCreated)
//                 samplePoints.Dispose();
//             isInitialized = false;
//         }

//         public bool TryGetColliderTargets(
//             XRInteractionManager interactionManager,
//             List<Collider> colliders)
//         {
//             return TryGetColliderTargets(interactionManager, colliders, new List<RaycastHit>());
//         }

//         public bool TryGetColliderTargets(
//             XRInteractionManager interactionManager,
//             List<Collider> colliders,
//             List<RaycastHit> hits)
//         {
//             // todo: keep reference to a previous collider.
//             // first do a line raycast.
//             // if there is no hit on last target, do a box cast to see if we hit last target.
//             // if there is no hit on the box cast, we return the result of the line raycast.
//             colliders.Clear();
//             hits.Clear();

//             Vector3 origin = effectiveCastOrigin.position;
//             Vector3 direction = effectiveCastOrigin.forward;

//             RaycastHit hit = new RaycastHit();

//             bool hitThePlane = false;
//             var hitDetected = false;
//             // do a normal cast.
//             var lineHitDetected = Physics.Raycast(
//                 origin: origin,
//                 direction: direction,
//                 out var lineHit,
//                 maxDistance: castDistance,
//                 layerMask: Physics.DefaultRaycastLayers,
//                 queryTriggerInteraction: QueryTriggerInteraction.Ignore);

//             hitDetected = lineHitDetected;
//             if (lineHitDetected)
//             {
//                 hit = lineHit;
//                 hitDetected = true;
//                 Debug.Assert(lineHit.collider != null);
//                 Debug.Assert(lineHit.collider.bounds.size.magnitude < maxAssertionNumber);
//             }
//             // do an extra cast against last collider
//             if (lastHit.HasValue)
//             {
//                 // get last hit distance
//                 var lastHitDistance = Vector3.Distance(origin, lastHit.Value.point);


//                 if
//                 (
//                     (lineHitDetected && lineHit.collider != lastHit.Value.collider && lineHit.distance > lastHitDistance)
//                     || !lineHitDetected
//                     || lastHit.Value.normal != lineHit.normal

//                 )
//                 {
//                     var planeHitPoint = lastHit.Value.point;
//                     hitThePlane = MathUtils.CastToPlane(
//                         planeOrigin: lastHit.Value.point,
//                         planeNormal: lastHit.Value.normal,
//                         castOrigin: origin,
//                         castDirection: direction,
//                         maxDistance: castDistance,
//                         hitPoint: out planeHitPoint);

//                     if (hitThePlane)
//                     {
//                         // convert the hit point to the local space of the last collider.

//                         var tx = new GameObject("tx").transform;
//                         tx.position = lastHit.Value.collider.bounds.center;
//                         tx.rotation = Quaternion.LookRotation(lastHit.Value.normal, lastHit.Value.collider.transform.up);
//                         tx.localScale = Vector3.one;
//                         if (Bnds.GetOrientedBounds(
//                                 refSpace: tx,
//                                 @object: lastHit.Value.collider,
//                                 out var center,
//                                 out var extents,
//                                 out var size,
//                                 out var min,
//                                 out var max
//                         ))
//                         {
//                             var bounds = new Bounds(center, size);
//                             var localHitPoint = tx.InverseTransformPoint(planeHitPoint);

//                             bounds.Expand(new Vector3(snapBorders, snapBorders, snapBorders));

//                             if (bounds.Contains(localHitPoint))

//                             {
//                                 // only way to access the last collider from it.
//                                 var planeHit = lastHit.Value;


//                                 // clamp to the local bounds of the last collider.
//                                 planeHit.point = new Vector3(
//                                     Mathf.Clamp(localHitPoint.x, -extents.x, extents.x),
//                                     Mathf.Clamp(localHitPoint.y, -extents.y, extents.y),
//                                     Mathf.Clamp(localHitPoint.z, -extents.z, extents.z)

//                                 );
//                                 planeHit.point = tx.TransformPoint(planeHit.point);
//                                 GameObject.DestroyImmediate(tx.gameObject);
//                                 hit = planeHit;
//                                 hitDetected = true;
//                                 Debug.Log(




//                                     $"Hit the plane at length of {hit.distance}, Hit point: {hit.point}, " +
//                                     $"hit normal: {hit.normal}, origin: {origin}, direction: {direction}, castDistance: {castDistance}, " +
//                                     $"hit.collider has name: {hit.collider.name} last hit collider has name: {lastHit.Value.collider.name}");

//                                 // collider is read only, so this is why we needed the box cast.

//                             }
//                             // }

//                         }
//                     }
//                 }
//             }
//             // Update samplePoints
//             if (!samplePoints.IsCreated || samplePoints.Length != 2)
//             {
//                 if (samplePoints.IsCreated)
//                     samplePoints.Dispose();
//                 samplePoints = new NativeArray<Vector3>(2, Allocator.Persistent);
//             }
//             Vector3 endPoint = origin + direction * neutralLineLength;

//             if (hitDetected)
//             {
//                 endPoint = hit.point;

//             }
//             samplePoints.CopyFrom(new[] { origin, endPoint });

//             // On hit, cache new OBB
//             if (hitDetected)
//             {
//                 this.lastHit = hit;
//                 colliders.Add(hit.collider);
//                 hits.Add(hit);

//                 if (hitPointDebugMarker != null)
//                 {
//                     hitPointDebugMarker.position = hit.point;
//                     hitPointDebugMarker.rotation = Quaternion.LookRotation(hit.normal, hit.collider.transform.up);

//                 }

//             }
//             else
//             {
//                 lastHit = null;
//             }
//             return hitDetected;
//         }
//     }
// }
