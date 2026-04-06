using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLN
{
    public enum BoundsDimensionSpace
    {
        Local,
        WorldOriented
    }

    [ExecuteAlways]
    [RequireComponent(typeof(SmartBounds))]
    public class BoundsVisuals : MonoBehaviour
    {
        [Header("Target")]
        public GameObject target;

        [Header("Display")]
        public bool showVisuals = true;
        public bool showOnlyWhenSelected = true;

        [Header("Which Bounds To Draw")]
        public bool showSelectedRegion = true;
        public bool showContents = false;
        public bool showBorders = false;
        public bool showPadding = false;

        [Header("Style")]
        public bool showLabels = true;
        public bool showDimensions = true;
        public BoundsDimensionSpace dimensionSpace = BoundsDimensionSpace.WorldOriented;

        [Min(1f)] public float lineThickness = 5f;
        [Min(1)] public int labelFontSize = 11;
        [Min(0f)] public float labelOffsetWorld = 0.05f;

        [Header("Colors")]
        public Color selectedRegionColor = Color.magenta;
        public Color contentsColor = Color.green;
        public Color bordersColor = Color.yellow;
        public Color paddingColor = Color.cyan;

        public LabelPlacement selectedRegionLabelPlacement = LabelPlacement.TopFrontLeft;
        public LabelPlacement bordersLabelPlacement = LabelPlacement.TopBackLeft;
        public LabelPlacement contentsLabelPlacement = LabelPlacement.TopFrontLeft;
        public LabelPlacement paddingLabelPlacement = LabelPlacement.TopFrontLeft;

        private void Reset()
        {
            if (target == null)
                target = gameObject;
        }

        private void OnValidate()
        {
            if (target == null)
                target = gameObject;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            TryDraw();
        }

        private void OnDrawGizmosSelected()
        {
            TryDraw();
        }

        private void TryDraw()
        {
            if (!showVisuals)
                return;

            GameObject drawTarget = target != null ? target : gameObject;
            if (!ShouldDrawNow(drawTarget))
                return;

            Draw();
        }

        public void Draw()
        {
            GameObject drawTarget = target != null ? target : gameObject;
            if (drawTarget == null)
                return;

            BoundsVisualsDrawer.DrawBoundsVisuals(
                showSelectedRegion,
                showContents,
                showBorders,
                showPadding,
                drawTarget,
                null,
                selectedRegionColor,
                contentsColor,
                bordersColor,
                paddingColor,
                lineThickness,
                labelFontSize,
                labelOffsetWorld,
                showLabels,
                showDimensions,
                dimensionSpace,
                selectedRegionLabelPlacement,
                bordersLabelPlacement,
                contentsLabelPlacement,
                paddingLabelPlacement
            );
        }

        private bool ShouldDrawNow(GameObject drawTarget)
        {
            if (Application.isPlaying)
                return true;

            if (!showOnlyWhenSelected)
                return true;

            GameObject selected = Selection.activeGameObject;
            if (selected == null)
                return false;

            return selected == gameObject || selected == drawTarget;
        }
#endif
    }
}