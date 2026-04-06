using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DLN
{
    public enum WhichHand
    {
        Left,
        Right,
        Either
    }

    public static class Conversions
    {
        
        public static Material ToMaterial(this UnityEngine.Object obj)
        {
            if (obj is Material mat)
            {
                return mat;
            }
            else if (obj is Renderer renderer)
            {
                return renderer.sharedMaterial;
            }
            else if (obj is GameObject go)
            {
                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                    return rend.sharedMaterial;
            }
            else if (obj is Component comp)
            {
                var rend = comp.GetComponent<Renderer>();
                if (rend != null)
                    return rend.sharedMaterial;
            }

            throw new ArgumentException($"Cannot convert {obj} to Material in ToMaterial");
        }

        public static Transform AsTransform(this UnityEngine.Object obj)
        {
            if (obj is Transform t) return t;
            if (obj is GameObject go) return go.transform;
            if (obj is Component comp) return comp.transform;


            throw new ArgumentException($"Cannot convert {obj} to Transform in AsTransform");
        }

        public static GameObject AsGameObject(this UnityEngine.Object obj)
        {
            if (obj is GameObject go) return go;
            if (obj is Transform t) return t.gameObject;
            if (obj is Component comp) return comp.gameObject;

            throw new ArgumentException($"Cannot convert {obj} to GameObject in AsGameObject");
        }

        public static SmartBounds ToSmartBounds(this UnityEngine.Object obj)
        {
            if (obj is SmartBounds smartBounds)
                return smartBounds;

            var go = obj.AsGameObject();

            if (go.TryGetComponent<SmartBounds>(out var existingSB))
                return existingSB;

            var sb = go.AddComponent<SmartBounds>();
            if (sb == null)
                throw new ArgumentException("AddComponent<SmartBounds>() returned null.");

            return sb;
        }

        public static Bounds ToWorldBounds(this UnityEngine.Object obj, OptionalBoundsSettings overrides = default)
        {

            Bnds.GetBoundsCorners(
                space: Space.World,
                refSpace: null,
                @object: obj,
                corners: out var corners,
                overrides: overrides);

            if (corners == null || corners.Length == 0)
                return new Bounds(Vector3.zero, Vector3.zero);

            var bounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(corners[i]);

            return bounds;
        }

        public static Bounds? ToLocalBounds(this UnityEngine.Object obj, OptionalBoundsSettings overrides = default)
        {
            return obj.ToSmartBounds().GetLocalBoundsRecursive(overrides);
        }

        public static Bounds? ToBounds(this UnityEngine.Object obj, Transform refOrigin, Transform refRotation, Transform refScale, OptionalBoundsSettings overrides = default)
        {
            Bnds.GetOrientedBounds(@object: obj, refOrigin: refOrigin, refRotation: refRotation, refScale: refScale, bounds: out var bounds, overrides: overrides);
            return bounds;
        }
        public static Bounds? ToBounds(this UnityEngine.Object obj, Vector3 refOrigin, Quaternion refRotation, Vector3 refScale, OptionalBoundsSettings overrides = default)
        {
            var trs = Matrix4x4.TRS(
                refOrigin,
                refRotation,
                refScale
            );

            Bnds.GetOrientedBounds(@object: obj, trs: trs, bounds: out var bounds, overrides: overrides);
            return bounds;
        }
        public static Bounds? ToBounds(this UnityEngine.Object obj, Matrix4x4 trs, OptionalBoundsSettings overrides = default)
        {
            Bnds.GetOrientedBounds(@object: obj, trs: trs, bounds: out var bounds, overrides: overrides);
            return bounds;
        }

        public static Quaternion AsWorldRotation(this UnityEngine.Object obj)
        {
            return obj switch
            {
                Transform t => t.rotation,
                GameObject go => go.transform.rotation,
                Component c => c.transform.rotation,
                _ => throw new ArgumentException($"Cannot convert {obj} to Quaternion in AsWorldRotation")
            };
        }

        public static InteractorHandedness ToHandedness(this UnityEngine.Object obj)
        {
            return obj switch
            {
                XRBaseInputInteractor xrController => xrController.handedness,
                XRBaseInteractor xrController => xrController.handedness,
                _ => throw new ArgumentException($"Cannot convert {obj} to Handedness in ToHandedness")
            };
        }



        public static Renderer AsRenderer(this UnityEngine.Object obj)
        {
            return obj switch
            {
                Renderer r => r,
                GameObject go => go.GetComponent<Renderer>(),
                Component c => c.GetComponent<Renderer>(),
                _ => throw new ArgumentException($"Cannot convert {obj} to Renderer in AsRenderer")
            };
        }



        public static Vector3 PosFromLocalTo(this Vector3 vec, Space space, Transform localTransform, Transform refTransform = null)
        {
            if (space == Space.World)
            {
                if (localTransform == null)
                    return vec;

                return localTransform.TransformPoint(vec);
            }
            else if (space == Space.Local)
            {
                return vec;
            }
            else if (space == Space.Reference)
            {
                if (refTransform == null)
                    throw new ArgumentException("Target transform cannot be null when space is Reference.");

                var world = localTransform.TransformPoint(vec);
                return refTransform.InverseTransformPoint(world);
            }
            else if (space == Space.Parent)
            {
                if (localTransform.parent == null)
                {
                    Debug.LogWarning("Local transform has no parent, converting to world space.");
                    return localTransform.TransformPoint(vec);
                }

                var world = localTransform.TransformPoint(vec);
                return localTransform.parent.InverseTransformPoint(world);
            }
            else
            {
                throw new ArgumentException($"Cannot convert {vec} to {space} space in PosFromLocalTo");
            }
        }

        public static Vector3 DirFromLocalTo(this Vector3 vec, Space space, Transform localTransform, Transform refTransform = null)
        {
            if (space == Space.World)
            {
                if (localTransform == null)
                    return vec;

                return localTransform.TransformDirection(vec);
            }
            else if (space == Space.Local)
            {
                return vec;
            }
            else if (space == Space.Reference)
            {
                if (refTransform == null)
                    throw new ArgumentException("Target transform cannot be null when space is Reference.");

                var world = localTransform.TransformDirection(vec);
                return refTransform.InverseTransformDirection(world);
            }
            else if (space == Space.Parent)
            {
                if (localTransform.parent == null)
                {
                    Debug.LogWarning("Local transform has no parent, converting to world space.");
                    return localTransform.TransformDirection(vec);
                }

                var world = localTransform.TransformDirection(vec);
                return localTransform.parent.InverseTransformDirection(world);
            }
            else
            {
                throw new ArgumentException($"Cannot convert {vec} to {space} space in DirFromLocalTo");
            }
        }

        public static Mesh AsMesh(this UnityEngine.Object obj)
        {
            if (obj is Mesh m) return m;
            if (obj is MeshFilter mf) return mf.mesh;
            if (obj is Component comp) return comp.GetComponent<MeshFilter>().mesh;
            if (obj is GameObject go) return go.GetComponent<MeshFilter>().mesh;

            throw new ArgumentException($"Cannot convert {obj} to Mesh in AsMesh");
        }

        public static Collider AsCollider(this UnityEngine.Object obj)
        {
            return obj switch
            {
                Collider c => c,
                GameObject go => go.GetComponent<Collider>(),
                Component comp => comp.GetComponent<Collider>(),
                _ => throw new ArgumentException($"Cannot convert {obj} to Collider in AsCollider")
            };
        }

        public static XRBaseInteractable AsXRBaseInteractable(this UnityEngine.Object obj, bool createIfNotExists = false)
        {
            if (obj is XRBaseInteractable interactable)
                return interactable;

            if (obj is GameObject go)
            {
                var existing = go.GetComponent<XRBaseInteractable>();
                if (existing != null) return existing;

                if (createIfNotExists)
                    return go.AddComponent<XRBaseInteractable>();

                throw new ArgumentException($"Failed to convert object to XRBaseInteractable in AsXRBaseInteractable. Create if not exists is set to {createIfNotExists}.");
            }

            if (obj is Component comp)
            {
                var existing = comp.GetComponent<XRBaseInteractable>();
                if (existing != null) return existing;

                if (createIfNotExists)
                    return comp.gameObject.AddComponent<XRBaseInteractable>();

                throw new ArgumentException($"Failed to convert object to XRBaseInteractable in AsXRBaseInteractable. Create if not exists is set to {createIfNotExists}.");
            }

            throw new ArgumentException($"Cannot convert {obj} to XRBaseInteractable in AsXRBaseInteractable.");
        }

        public static BoxCollider AddBoxCollider(
            UnityEngine.Object obj,
            bool isTrigger = true,
            bool enabled = true,
            List<string> includeLayerNames = null,
            List<string> excludeLayerNames = null,
            OptionalBoundsSettings overrides = default)
        {
            var go = obj.AsGameObject();

            var boxCollider = go.GetComponents<BoxCollider>()
                .Select(c =>
                {
                    c.isTrigger = isTrigger;
                    return c;
                })
                .FirstOrDefault();

            if (boxCollider == null)
                boxCollider = go.AddComponent<BoxCollider>();

            boxCollider.isTrigger = isTrigger;
            boxCollider.enabled = enabled;

            if (includeLayerNames != null && includeLayerNames.Count > 0)
            {
                var layers = includeLayerNames
                    .Select(name => LayerMask.NameToLayer(name))
                    .Where(layer => layer >= 0)
                    .ToList();

                if (layers.Count > 0)
                    boxCollider.includeLayers |= layers.Aggregate((a, b) => a | b);
            }

            if (excludeLayerNames != null && excludeLayerNames.Count > 0)
            {
                var layers = excludeLayerNames
                    .Select(name => LayerMask.NameToLayer(name))
                    .Where(layer => layer >= 0)
                    .ToList();

                if (layers.Count > 0)
                    boxCollider.excludeLayers |= layers.Aggregate((a, b) => a | b);
            }

            Bounds? bounds = go.ToLocalBounds(overrides);

            if (!bounds.HasValue)
            {
                boxCollider.center = Vector3.zero;
                boxCollider.size = Vector3.zero;
                boxCollider.enabled = false;
                return boxCollider;
            }

            boxCollider.center = bounds.Value.center;
            boxCollider.size = bounds.Value.size;
            boxCollider.enabled = enabled;

            return boxCollider;
        }
    }
}