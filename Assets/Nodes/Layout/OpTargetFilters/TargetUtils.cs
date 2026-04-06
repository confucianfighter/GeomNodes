using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN
{
    public static class TargetUtils
    {
        public static List<GameObject> GetTargets(
            TargetQuery options,
            OptionalBoundsSettings? boundsSettings = null,
            Func<GameObject, bool> additionalValidator = null)
        {
            var results = new List<GameObject>();

            if (options.root == null)
                return results;

            string label = string.IsNullOrWhiteSpace(options.label)
                ? null
                : options.label.Trim();

            if (options.RequiresLabel && string.IsNullOrEmpty(label))
            {
                throw new ArgumentException(
                    $"{options.include} requires a non-empty label.",
                    nameof(options));
            }

            OptionalBoundsSettings? resolvedBoundsSettings = boundsSettings ?? options.boundsOverrides;

            switch (options.include)
            {
                case TargetIncludeMode.RootOnly:
                    TryAddTarget(
                        go: options.root,
                        label: null,
                        boundsSettings: resolvedBoundsSettings,
                        additionalValidator: additionalValidator,
                        results: results);
                    return results;

                case TargetIncludeMode.ImmediateChildren:
                    AddImmediateChildren(
                        parent: options.root.transform,
                        label: null,
                        boundsSettings: resolvedBoundsSettings,
                        additionalValidator: additionalValidator,
                        results: results);
                    return results;

                case TargetIncludeMode.ImmediateChildrenWithLabel:
                    AddImmediateChildren(
                        parent: options.root.transform,
                        label: label,
                        boundsSettings: resolvedBoundsSettings,
                        additionalValidator: additionalValidator,
                        results: results);
                    return results;

                case TargetIncludeMode.FirstMatchingDepthWithLabel:
                    AddFirstMatchingDepth(
                        root: options.root.transform,
                        label: label,
                        boundsSettings: resolvedBoundsSettings,
                        additionalValidator: additionalValidator,
                        results: results);
                    return results;

                case TargetIncludeMode.AllMatchingDescendantsWithLabel:
                    AddAllMatchingDescendants(
                        parent: options.root.transform,
                        label: label,
                        boundsSettings: resolvedBoundsSettings,
                        additionalValidator: additionalValidator,
                        results: results);
                    return results;

                default:
                    return results;
            }
        }

        private static void AddImmediateChildren(
            Transform parent,
            string label,
            OptionalBoundsSettings? boundsSettings,
            Func<GameObject, bool> additionalValidator,
            List<GameObject> results)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                TryAddTarget(
                    go: parent.GetChild(i).gameObject,
                    label: label,
                    boundsSettings: boundsSettings,
                    additionalValidator: additionalValidator,
                    results: results);
            }
        }

        private static void AddAllMatchingDescendants(
            Transform parent,
            string label,
            OptionalBoundsSettings? boundsSettings,
            Func<GameObject, bool> additionalValidator,
            List<GameObject> results)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                TryAddTarget(
                    go: child.gameObject,
                    label: label,
                    boundsSettings: boundsSettings,
                    additionalValidator: additionalValidator,
                    results: results);

                AddAllMatchingDescendants(
                    parent: child,
                    label: label,
                    boundsSettings: boundsSettings,
                    additionalValidator: additionalValidator,
                    results: results);
            }
        }

        private static void AddFirstMatchingDepth(
            Transform root,
            string label,
            OptionalBoundsSettings? boundsSettings,
            Func<GameObject, bool> additionalValidator,
            List<GameObject> results)
        {
            var currentDepth = new List<Transform> { root };

            while (currentDepth.Count > 0)
            {
                var nextDepth = new List<Transform>();
                var matches = new List<GameObject>();

                for (int i = 0; i < currentDepth.Count; i++)
                {
                    Transform parent = currentDepth[i];

                    for (int j = 0; j < parent.childCount; j++)
                    {
                        Transform child = parent.GetChild(j);
                        GameObject go = child.gameObject;

                        if (MatchesLabel(go, label) &&
                            IsValidTarget(go, boundsSettings) &&
                            PassesAdditionalValidator(go, additionalValidator))
                        {
                            matches.Add(go);
                        }

                        nextDepth.Add(child);
                    }
                }

                if (matches.Count > 0)
                {
                    results.AddRange(matches);
                    return;
                }

                currentDepth = nextDepth;
            }
        }

        private static void TryAddTarget(
            GameObject go,
            string label,
            OptionalBoundsSettings? boundsSettings,
            Func<GameObject, bool> additionalValidator,
            List<GameObject> results)
        {
            if (go == null)
                return;

            if (label != null && !MatchesLabel(go, label))
                return;

            if (!IsValidTarget(go, boundsSettings))
                return;

            if (!PassesAdditionalValidator(go, additionalValidator))
                return;

            results.Add(go);
        }

        private static bool MatchesLabel(GameObject go, string label)
        {
            if (label == null)
                return true;

            return go.TryGetComponent<TargetMetadata>(out var metadata) &&
                   metadata.HasLabel(label);
        }

        private static bool IsValidTarget(GameObject go, OptionalBoundsSettings? boundsSettings)
        {
            if (go == null)
                return false;

            Bounds? bounds = boundsSettings.HasValue
                ? go.ToLocalBounds(boundsSettings.Value)
                : go.ToLocalBounds();

            return bounds.HasValue;
        }

        private static bool PassesAdditionalValidator(
            GameObject go,
            Func<GameObject, bool> additionalValidator)
        {
            return additionalValidator == null || additionalValidator(go);
        }
    }
}