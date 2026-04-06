using System;
using UnityEngine;

namespace DLN
{
    [Serializable]
    public struct OptionalBool
    {
        [SerializeField] private bool hasValue;
        [SerializeField] private bool value;

        public bool HasValue => hasValue;

        public bool Value
        {
            get
            {
                if (!hasValue)
                    throw new InvalidOperationException("OptionalBool has no value.");
                return value;
            }
            set
            {
                this.value = value;
                hasValue = true;
            }
        }

        public void Set(bool value)
        {
            this.value = value;
            hasValue = true;
        }

        public void Clear()
        {
            value = default;
            hasValue = false;
        }

        public bool ValueOr(bool fallback)
        {
            return hasValue ? value : fallback;
        }

        public static OptionalBool WithValue(bool value)
        {
            var result = new OptionalBool();
            result.Set(value);
            return result;
        }
        public static OptionalBool WithOutValue()
        {
            var result = new OptionalBool();
            result.hasValue = false;
            return result;
        }
    }
    [Serializable]
    public struct BoundsSettings
    {
        [SerializeField] public bool includeSelf;
        [SerializeField] public bool includeChildren;
        [SerializeField] public bool includeSelfEvenIfEmpty;
        [SerializeField] public bool useProxy;
        [SerializeField] public RegionSelection regionSelection;

        public static BoundsSettings Default => new BoundsSettings
        {
            includeSelf = true,
            includeChildren = true,
            includeSelfEvenIfEmpty = false,
            useProxy = false,
            regionSelection = RegionSelection.Contents

        };
    }



    [Serializable]
    public struct OptionalBoundsSettings
    {
        [SerializeField] private OptionalBool includeSelf;
        [SerializeField] private OptionalBool includeChildren;
        [SerializeField] private OptionalBool includeSelfEvenIfEmpty;
        [SerializeField] private OptionalBool useProxy;
        [SerializeField] private OptionalRegionSelection regionSelection;

        public OptionalBool IncludeSelf
        {
            get => includeSelf;
            set => includeSelf = value;
        }

        public OptionalBool IncludeChildren
        {
            get => includeChildren;
            set => includeChildren = value;
        }

        public OptionalBool IncludeSelfEvenIfEmpty
        {
            get => includeSelfEvenIfEmpty;
            set => includeSelfEvenIfEmpty = value;
        }

        public OptionalBool UseProxy
        {
            get => useProxy;
            set => useProxy = value;
        }

        public OptionalRegionSelection RegionSelectionOverride
        {
            get => regionSelection;
            set => regionSelection = value;
        }

        public void SetIncludeSelf(bool value) => includeSelf.Set(value);
        public void ClearIncludeSelf() => includeSelf.Clear();

        public void SetIncludeChildren(bool value) => includeChildren.Set(value);
        public void ClearIncludeChildren() => includeChildren.Clear();

        public void SetIncludeSelfEvenIfEmpty(bool value) => includeSelfEvenIfEmpty.Set(value);
        public void ClearIncludeSelfEvenIfEmpty() => includeSelfEvenIfEmpty.Clear();

        public void SetUseProxy(bool value) => useProxy.Set(value);
        public void ClearUseProxy() => useProxy.Clear();

        public void SetRegionSelection(RegionSelection value) => regionSelection.Set(value);
        public void ClearRegionSelection() => regionSelection.Clear();

        public static OptionalBoundsSettings Default => Empty;

        public static OptionalBoundsSettings Empty => new OptionalBoundsSettings
        {
            includeSelf = OptionalBool.WithOutValue(),
            includeChildren = OptionalBool.WithOutValue(),
            includeSelfEvenIfEmpty = OptionalBool.WithOutValue(),
            useProxy = OptionalBool.WithOutValue(),
            regionSelection = OptionalRegionSelection.WithValue(RegionSelection.Contents)
        };

    }

    [Serializable]
    public struct ResolvedBoundsSettings
    {
        [SerializeField] public bool includeSelf;
        [SerializeField] public bool includeChildren;
        [SerializeField] public bool includeSelfEvenIfEmpty;
        [SerializeField] public bool useProxy;
        [SerializeField] public RegionSelection regionSelection;
    }

    public static class BoundsSettingsResolver
    {
        public static ResolvedBoundsSettings Resolve(BoundsSettings defaults, OptionalBoundsSettings overrides)
        {
            var settings = new ResolvedBoundsSettings
            {
                includeSelf = overrides.IncludeSelf.ValueOr(defaults.includeSelf),
                includeChildren = overrides.IncludeChildren.ValueOr(defaults.includeChildren),
                includeSelfEvenIfEmpty = overrides.IncludeSelfEvenIfEmpty.ValueOr(defaults.includeSelfEvenIfEmpty),
                useProxy = overrides.UseProxy.ValueOr(defaults.useProxy),
                regionSelection = overrides.RegionSelectionOverride.ValueOr(defaults.regionSelection)
            };
            return settings;
        }

        public static ResolvedBoundsSettings Resolve(OptionalBoundsSettings settings)
        {
            return Resolve(BoundsSettings.Default, settings);
        }

        public static OptionalBoundsSettings Merge(BoundsSettings defaults, OptionalBoundsSettings overrides)
        {
            var resolved = Resolve(defaults, overrides);

            OptionalBoundsSettings result = OptionalBoundsSettings.Empty;
            result.SetIncludeSelf(resolved.includeSelf);
            result.SetIncludeChildren(resolved.includeChildren);
            result.SetIncludeSelfEvenIfEmpty(resolved.includeSelfEvenIfEmpty);
            result.SetUseProxy(resolved.useProxy);
            result.SetRegionSelection(resolved.regionSelection);

            return result;
        }

    }



}