using UnityEngine;
using TMPro;
using DLN.Extensions;
using DLN;

namespace DLN
{
    [RequireComponent(typeof(BoundsVisuals))]
    [DisallowMultipleComponent]
    public class SmartBounds : MonoBehaviour
    {
        public BoundsSettings settings = BoundsSettings.Default;
        public BordersPadding bordersPadding = default;
        [Header("Optional Explicit Bounds Source")]
        [SerializeField] public Object boundsObject = null;

        [Header("Legacy / Special Case")]
        [SerializeField] public Transform proxyTarget = null;

        public Vector3 CachedCenter = Sentinel;
        public Vector3 CachedSize = Sentinel;
        public bool isEmpty = true;

        private static readonly Vector3 Sentinel = new Vector3(-1000f, -1000f, -1000f);

        public ResolvedBoundsSettings ResolveSettings(OptionalBoundsSettings overrides = default)
        {
            return BoundsSettingsResolver.Resolve(settings, overrides);
        }

        private static bool IsBoundsEmpty(Bounds b)
        {
            return b.size == Vector3.zero;
        }

        private void OnValidate()
        {
            GetComponent<BoundsVisuals>().Draw();
        }

        private bool TryResolveBoundsSource(out Object source)
        {
            if (boundsObject != null)
            {
                source = boundsObject;
                return true;
            }

            if (TryGetComponent<BoxCollider>(out var collider))
            {
                source = collider;
                return true;
            }

            if (TryGetComponent<TMP_Text>(out var tmp))
            {
                source = tmp;
                return true;
            }

            if (TryGetComponent<RectTransform>(out var rectTransform))
            {
                source = rectTransform;
                return true;
            }

            if (TryGetComponent<MeshFilter>(out var meshFilter))
            {
                source = meshFilter;
                return true;
            }

            if (TryGetComponent<MeshCollider>(out var meshCollider))
            {
                source = meshCollider;
                return true;
            }

            if (TryGetComponent<Renderer>(out var renderer))
            {
                source = renderer;
                return true;
            }

            if (TryGetComponent<SkinnedMeshRenderer>(out var skinnedMeshRenderer))
            {
                source = skinnedMeshRenderer;
                return true;
            }

            source = null;
            return false;
        }

        private bool TryGetBoundsFromSource(
            Object source,
            ResolvedBoundsSettings resolved,
            out Bounds bounds)
        {
            bounds = default;

            switch (source)
            {
                case BoxCollider collider:
                    bounds = new Bounds(collider.center, collider.size);
                    return resolved.includeSelfEvenIfEmpty || !IsBoundsEmpty(bounds);

                case TMP_Text tmp:
                    if (!TMPBoundsUtility.TryGetGlyphLocalBounds(tmp, out bounds))
                        return false;

                    return resolved.includeSelfEvenIfEmpty || !IsBoundsEmpty(bounds);

                case RectTransform rectTransform:
                    {
                        var pts = new Vector3[4];
                        rectTransform.GetLocalCorners(pts);

                        Bounds rectBounds = new Bounds(pts[0], Vector3.zero);
                        for (int i = 1; i < pts.Length; i++)
                            rectBounds.Encapsulate(pts[i]);

                        bounds = rectBounds;
                        return resolved.includeSelfEvenIfEmpty || !IsBoundsEmpty(bounds);
                    }

                case MeshFilter meshFilter:
                    if (meshFilter.sharedMesh == null)
                        return false;

                    bounds = meshFilter.sharedMesh.bounds;
                    return resolved.includeSelfEvenIfEmpty || !IsBoundsEmpty(bounds);

                case MeshCollider meshCollider:
                    if (meshCollider.sharedMesh == null)
                        return false;

                    bounds = meshCollider.sharedMesh.bounds;
                    return resolved.includeSelfEvenIfEmpty || !IsBoundsEmpty(bounds);

                case SkinnedMeshRenderer skinnedMeshRenderer:
                    if (skinnedMeshRenderer.sharedMesh == null)
                        return false;

                    bounds = skinnedMeshRenderer.sharedMesh.bounds;
                    return resolved.includeSelfEvenIfEmpty || !IsBoundsEmpty(bounds);

                case Renderer renderer:
                    bounds = renderer.localBounds;
                    return resolved.includeSelfEvenIfEmpty || !IsBoundsEmpty(bounds);

                default:
                    return false;
            }
        }

        private bool TryGetRawSelfSourceBounds(
            ResolvedBoundsSettings resolved,
            out Bounds bounds)
        {
            bounds = default;

            if (!TryResolveBoundsSource(out var source) || source == null)
                return false;

            if (boundsObject == null)
                boundsObject = source;

            return TryGetBoundsFromSource(source, resolved, out bounds);
        }

        private bool TryGetRawProxySelfBounds(
            ResolvedBoundsSettings resolved,
            out Bounds bounds)
        {
            bounds = default;

            if (!TryGetComponent<ProxyBounds>(out var proxy) || proxy == null || !proxy.isActiveAndEnabled)
                return false;

            Bounds? proxyBounds = proxy.GetSelfBounds();
            if (!proxyBounds.HasValue)
                return false;

            bounds = proxyBounds.Value;
            return resolved.includeSelfEvenIfEmpty || !IsBoundsEmpty(bounds);
        }

        private bool TryGetSelectedRawSelfBounds(
            ResolvedBoundsSettings resolved,
            out Bounds bounds)
        {
            bounds = default;

            if (resolved.useProxy && TryGetRawProxySelfBounds(resolved, out bounds))
                return true;

            return TryGetRawSelfSourceBounds(resolved, out bounds);
        }

        private bool TryApplyRegionSelection(
            Bounds sourceBounds,
            ResolvedBoundsSettings resolved,
            out Bounds result)
        {
            return RegionSelection.CalculateRegion(
                sourceBounds,
                resolved.regionSelection,
                bordersPadding,
                out result);
        }

        private static Bounds Encapsulate(Bounds a, Bounds b)
        {
            a.Encapsulate(b.min);
            a.Encapsulate(b.max);
            return a;
        }

        public bool GetSelfBounds(out Bounds bounds, OptionalBoundsSettings overrides = default)
        {
            bounds = default;

            ResolvedBoundsSettings resolved = ResolveSettings(overrides);

            if (!TryGetSelectedRawSelfBounds(resolved, out var rawBounds))
                return false;

            if (!TryApplyRegionSelection(rawBounds, resolved, out bounds))
                return false;

            return resolved.includeSelfEvenIfEmpty || !IsBoundsEmpty(bounds);
        }

        private Bounds? GetLocalBoundsRecursiveCore(ResolvedBoundsSettings resolved)
        {
            Bounds? combinedBounds = null;

            if (proxyTarget != null)
            {
                Bounds? proxyBounds = proxyTarget.ToBounds(refRotation: transform, refOrigin: transform, refScale: transform);

                if (proxyBounds.HasValue)
                {
                    combinedBounds = combinedBounds.HasValue
                        ? Encapsulate(combinedBounds.Value, proxyBounds.Value)
                        : proxyBounds.Value;
                }
            }

            if (resolved.includeSelf && TryGetSelectedRawSelfBounds(resolved, out var selfRawBounds))
            {
                combinedBounds = combinedBounds.HasValue
                    ? Encapsulate(combinedBounds.Value, selfRawBounds)
                    : selfRawBounds;
            }

            if (resolved.includeChildren)
            {
                foreach (Transform child in transform)
                {
                    Bounds? childBounds = child.ToBounds(refRotation: transform, refOrigin: transform, refScale: transform);

                    if (!childBounds.HasValue)
                        continue;

                    combinedBounds = combinedBounds.HasValue
                        ? Encapsulate(combinedBounds.Value, childBounds.Value)
                        : childBounds.Value;
                }
            }

            if (!combinedBounds.HasValue)
            {
                CachedCenter = Sentinel;
                CachedSize = Sentinel;
                isEmpty = true;

                if (resolved.includeSelfEvenIfEmpty)
                    return new Bounds(Vector3.zero, Vector3.zero);

                return null;
            }

            if (!TryApplyRegionSelection(combinedBounds.Value, resolved, out var finalBounds))
            {
                CachedCenter = Sentinel;
                CachedSize = Sentinel;
                isEmpty = true;

                if (resolved.includeSelfEvenIfEmpty)
                    return new Bounds(Vector3.zero, Vector3.zero);

                return null;
            }

            if (!resolved.includeSelfEvenIfEmpty && IsBoundsEmpty(finalBounds))
            {
                CachedCenter = Sentinel;
                CachedSize = Sentinel;
                isEmpty = true;
                return null;
            }

            CachedCenter = finalBounds.center;
            CachedSize = finalBounds.size;
            isEmpty = false;
            finalBounds = finalBounds.ExpandIfZero(Constants.Epsilon);
            return finalBounds;
        }

        public Bounds? GetLocalBoundsRecursive(OptionalBoundsSettings overrides = default)
        {
            ResolvedBoundsSettings resolved = ResolveSettings(overrides);
            return GetLocalBoundsRecursiveCore(resolved);
        }

        public void SetIncludeChildren(bool include)
        {
            settings.includeChildren = include;
        }

        public void SetIncludeSelf(bool include)
        {
            settings.includeSelf = include;
        }

        public void SetRegionSelection(RegionSelection selection)
        {
            settings.regionSelection = selection;
        }

        public static bool TryGetTargetSettings(GameObject go, out BoundsSettings? settings)
        {
            if (go.TryGetComponent<SmartBounds>(out var smartBounds))
            {
                settings = smartBounds.settings;
                return true;
            }

            settings = null;
            return false;
        }
    }
}