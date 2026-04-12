using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN
{
    public enum AdaptiveShapeSlotKind
    {
        Material = 0,
        Effects = 1,
        Semantic = 2
    }

    [Serializable]
    public struct AdaptiveShapeMaterialSlot
    {
        public string name;
        public AdaptiveShapeSlotKind kind;
        public Material material;
    }

    [Serializable]
    public struct AdaptiveShapeProfileEdge
    {
        public string name;
        [Min(0)] public int materialSlotIndex;
    }

    [Serializable]
    public struct AdaptiveShapeProfile
    {
        public List<AdaptiveShapeProfileEdge> edges;
    }

    [Serializable]
    public struct AdaptiveShapeSizeSettings
    {
        [Tooltip("Temporary bridge mode: treat BordersPadding minContentsSize as the adaptive shape inner size.")]
        public bool useBordersPaddingMinAsInnerSize;

        [Tooltip("Fallback inner size used when not reading from BordersPadding min values.")]
        public Vector2 explicitInnerSize;
    }

    [DisallowMultipleComponent]
    public sealed class AdaptiveShape : MonoBehaviour
    {
        [SerializeField] private SmartBounds smartBounds;
        [SerializeField] private bool preferSmartBoundsBordersPadding = true;
        [SerializeField] private BordersPadding fallbackBordersPadding = BordersPadding.Default;

        [SerializeField] private AdaptiveShapeSizeSettings sizeSettings;

        [SerializeField] private List<AdaptiveShapeMaterialSlot> materialSlots = new();
        [SerializeField] private AdaptiveShapeProfile profile = new AdaptiveShapeProfile
        {
            edges = new List<AdaptiveShapeProfileEdge>()
        };

        public SmartBounds SmartBounds => smartBounds;
        public IReadOnlyList<AdaptiveShapeMaterialSlot> MaterialSlots => materialSlots;
        public AdaptiveShapeProfile Profile => profile;

        public BordersPadding GetEffectiveBordersPadding()
        {
            EnsureReferences();

            BordersPadding result = preferSmartBoundsBordersPadding && smartBounds != null
                ? smartBounds.bordersPadding
                : fallbackBordersPadding;

            result.ClampToValid();
            return result;
        }

        public Vector2 GetCurrentInnerSize()
        {
            if (sizeSettings.useBordersPaddingMinAsInnerSize)
            {
                BordersPadding bp = GetEffectiveBordersPadding();
                return new Vector2(bp.x.minContentsSize, bp.y.minContentsSize);
            }

            return new Vector2(
                Mathf.Max(0f, sizeSettings.explicitInnerSize.x),
                Mathf.Max(0f, sizeSettings.explicitInnerSize.y));
        }

        public bool TryGetSlot(int index, out AdaptiveShapeMaterialSlot slot)
        {
            if (index >= 0 && index < materialSlots.Count)
            {
                slot = materialSlots[index];
                return true;
            }

            slot = default;
            return false;
        }

        public void EnsureReferences()
        {
            if (smartBounds == null)
                TryGetComponent(out smartBounds);
        }

        private void Reset()
        {
            EnsureReferences();
            ValidateData();
        }

        private void OnValidate()
        {
            EnsureReferences();
            ValidateData();
        }

        private void ValidateData()
        {
            fallbackBordersPadding.ClampToValid();
            sizeSettings.explicitInnerSize.x = Mathf.Max(0f, sizeSettings.explicitInnerSize.x);
            sizeSettings.explicitInnerSize.y = Mathf.Max(0f, sizeSettings.explicitInnerSize.y);

            if (materialSlots == null)
                materialSlots = new List<AdaptiveShapeMaterialSlot>();

            if (profile.edges == null)
                profile.edges = new List<AdaptiveShapeProfileEdge>();

            for (int i = 0; i < profile.edges.Count; i++)
            {
                AdaptiveShapeProfileEdge edge = profile.edges[i];
                edge.materialSlotIndex = Mathf.Max(0, edge.materialSlotIndex);

                if (materialSlots.Count > 0)
                    edge.materialSlotIndex = Mathf.Clamp(edge.materialSlotIndex, 0, materialSlots.Count - 1);

                profile.edges[i] = edge;
            }
        }
    }
}
