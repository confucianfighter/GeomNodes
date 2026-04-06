using System;
using UnityEngine;

namespace DLN
{
    public enum TargetIncludeMode
    {
        RootOnly,
        ImmediateChildren,
        ImmediateChildrenWithLabel,
        FirstMatchingDepthWithLabel,
        AllMatchingDescendantsWithLabel,
    }

    [Serializable]
    public struct TargetQuery
    {
        public GameObject root;
        public TargetIncludeMode include;
        public string label;
        public OptionalBoundsSettings boundsOverrides;

        public static TargetQuery Default => new TargetQuery
        {
            root = null,
            include = TargetIncludeMode.RootOnly,
            label = null,
            boundsOverrides = OptionalBoundsSettings.Empty
        };

        public bool ShowsLabel =>
            include == TargetIncludeMode.ImmediateChildrenWithLabel ||
            include == TargetIncludeMode.FirstMatchingDepthWithLabel ||
            include == TargetIncludeMode.AllMatchingDescendantsWithLabel;

        public bool RequiresLabel =>
            include == TargetIncludeMode.FirstMatchingDepthWithLabel ||
            include == TargetIncludeMode.AllMatchingDescendantsWithLabel;

        public bool HasValidLabel => !string.IsNullOrWhiteSpace(label);
    }
}