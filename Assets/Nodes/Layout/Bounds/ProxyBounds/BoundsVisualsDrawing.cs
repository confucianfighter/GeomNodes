using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLN
{
    public enum LabelPlacement
    {
        Center,

        BottomBackLeft,
        BottomBackRight,
        BottomFrontLeft,
        BottomFrontRight,

        TopBackLeft,
        TopBackRight,
        TopFrontLeft,
        TopFrontRight
    }

#if UNITY_EDITOR
    public static class BoundsVisualsDrawer
    {
        private static readonly int[,] EdgePairs =
        {
            {0,1}, {1,3}, {3,2}, {2,0}, // back face
            {4,5}, {5,7}, {7,6}, {6,4}, // front face
            {0,4}, {1,5}, {2,6}, {3,7}  // side connections
        };

        public static void DrawBoundsVisuals(
            bool showSelectedRegion,
            bool showContents,
            bool showBorders,
            bool showPadding,
            GameObject target,
            OptionalBoundsSettings? selectedRegionOverrides = null)
        {
            DrawBoundsVisuals(
                showSelectedRegion,
                showContents,
                showBorders,
                showPadding,
                target,
                selectedRegionOverrides,
                selectedRegionColor: Color.magenta,
                contentsColor: Color.green,
                bordersColor: Color.yellow,
                paddingColor: Color.cyan,
                lineThickness: 5f,
                labelFontSize: 12,
                labelOffsetWorld: 0.05f,
                showLabels: true,
                showDimensions: true,
                dimensionSpace: BoundsDimensionSpace.WorldOriented,
                selectedRegionLabelPlacement: LabelPlacement.TopFrontRight,
                bordersLabelPlacement: LabelPlacement.TopBackRight,
                contentsLabelPlacement: LabelPlacement.TopFrontLeft,
                paddingLabelPlacement: LabelPlacement.TopBackLeft
            );
        }

        public static void DrawBoundsVisuals(
            bool showSelectedRegion,
            bool showContents,
            bool showBorders,
            bool showPadding,
            GameObject target,
            OptionalBoundsSettings? selectedRegionOverrides,
            Color selectedRegionColor,
            Color contentsColor,
            Color bordersColor,
            Color paddingColor,
            float lineThickness,
            int labelFontSize,
            float labelOffsetWorld,
            bool showLabels,
            bool showDimensions,
            BoundsDimensionSpace dimensionSpace,
            LabelPlacement selectedRegionLabelPlacement,
            LabelPlacement bordersLabelPlacement,
            LabelPlacement contentsLabelPlacement,
            LabelPlacement paddingLabelPlacement)
        {
            if (target == null)
                return;

            lineThickness = Mathf.Max(1f, lineThickness);
            labelFontSize = Mathf.Max(1, labelFontSize);
            labelOffsetWorld = Mathf.Max(0f, labelOffsetWorld);

            if (showContents)
            {
                OptionalBoundsSettings overrides = OptionalBoundsSettings.Empty;
                overrides.SetRegionSelection(RegionSelection.Contents);

                Bounds? contentsBounds = target.ToLocalBounds(overrides);
                DrawBoundsVariant(
                    target,
                    contentsBounds,
                    overrides,
                    "Contents",
                    contentsColor,
                    lineThickness,
                    labelFontSize,
                    labelOffsetWorld,
                    showLabels,
                    showDimensions,
                    dimensionSpace,
                    contentsLabelPlacement
                );
            }

            if (showPadding)
            {
                OptionalBoundsSettings overrides = OptionalBoundsSettings.Empty;
                overrides.SetRegionSelection(RegionSelection.Padding);

                Bounds? paddingBounds = target.ToLocalBounds(overrides);
                DrawBoundsVariant(
                    target,
                    paddingBounds,
                    overrides,
                    "Padding",
                    paddingColor,
                    lineThickness,
                    labelFontSize,
                    labelOffsetWorld,
                    showLabels,
                    showDimensions,
                    dimensionSpace,
                    paddingLabelPlacement
                );
            }

            if (showBorders)
            {
                OptionalBoundsSettings overrides = OptionalBoundsSettings.Empty;
                overrides.SetRegionSelection(RegionSelection.Borders);

                Bounds? bordersBounds = target.ToLocalBounds(overrides);
                DrawBoundsVariant(
                    target,
                    bordersBounds,
                    overrides,
                    "Borders",
                    bordersColor,
                    lineThickness,
                    labelFontSize,
                    labelOffsetWorld,
                    showLabels,
                    showDimensions,
                    dimensionSpace,
                    bordersLabelPlacement
                );
            }

            if (showSelectedRegion)
            {
                OptionalBoundsSettings overrides = selectedRegionOverrides ?? default;
                Bounds? selectedBounds = target.ToLocalBounds(overrides);

                DrawBoundsVariant(
                    target,
                    selectedBounds,
                    overrides,
                    "Selected Region",
                    selectedRegionColor,
                    lineThickness,
                    labelFontSize,
                    labelOffsetWorld,
                    showLabels,
                    showDimensions,
                    dimensionSpace,
                    selectedRegionLabelPlacement
                );
            }
        }

        private static void DrawBoundsVariant(
            GameObject target,
            Bounds? bounds,
            OptionalBoundsSettings overrides,
            string label,
            Color color,
            float lineThickness,
            int labelFontSize,
            float labelOffsetWorld,
            bool showLabels,
            bool showDimensions,
            BoundsDimensionSpace dimensionSpace,
            LabelPlacement labelPlacement)
        {
            if (!bounds.HasValue)
                return;

            Bounds localBounds = bounds.Value;
            Vector3[] corners = GetBoxCornerPositions(localBounds);

            using (new Handles.DrawingScope(color, target.transform.localToWorldMatrix))
            {
                for (int i = 0; i < EdgePairs.GetLength(0); i++)
                {
                    int a = EdgePairs[i, 0];
                    int b = EdgePairs[i, 1];
                    Handles.DrawAAPolyLine(lineThickness, new Vector3[] { corners[a], corners[b] });
                }
            }

            if (showLabels)
            {
                string displayText = BuildLabelText(
                    target,
                    label,
                    localBounds,
                    overrides,
                    showDimensions,
                    dimensionSpace
                );

                DrawLabel(
                    target.transform,
                    localBounds,
                    corners,
                    displayText,
                    color,
                    labelFontSize,
                    labelOffsetWorld,
                    labelPlacement
                );
            }
        }

        private static string BuildLabelText(
            GameObject target,
            string label,
            Bounds localBounds,
            OptionalBoundsSettings overrides,
            bool showDimensions,
            BoundsDimensionSpace dimensionSpace)
        {
            if (!showDimensions)
                return label;

            Vector3 dims;

            switch (dimensionSpace)
            {
                case BoundsDimensionSpace.Local:
                    dims = localBounds.size;
                    break;

                case BoundsDimensionSpace.WorldOriented:
                default:
                    Bounds? worldBounds = target.ToBounds(
                        Vector3.zero,
                        target.transform.rotation,
                        Vector3.one,
                        overrides
                    );

                    dims = worldBounds.HasValue
                        ? worldBounds.Value.size
                        : localBounds.size;
                    break;
            }

            return $"{label}\n{dims.x:0.###}, {dims.y:0.###}, {dims.z:0.###}";
        }

        private static void DrawLabel(
            Transform targetTransform,
            Bounds localBounds,
            Vector3[] localCorners,
            string text,
            Color color,
            int fontSize,
            float labelOffsetWorld,
            LabelPlacement placement)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = color;
            style.fontSize = fontSize;

            Vector3 worldPos;

            switch (placement)
            {
                case LabelPlacement.Center:
                    style.alignment = TextAnchor.MiddleCenter;
                    worldPos = targetTransform.TransformPoint(localBounds.center);
                    break;

                case LabelPlacement.BottomBackLeft:
                    style.alignment = TextAnchor.UpperLeft;
                    worldPos = GetCornerLabelWorldPosition(targetTransform, localBounds.center, localCorners[0], labelOffsetWorld);
                    break;

                case LabelPlacement.BottomBackRight:
                    style.alignment = TextAnchor.UpperRight;
                    worldPos = GetCornerLabelWorldPosition(targetTransform, localBounds.center, localCorners[1], labelOffsetWorld);
                    break;

                case LabelPlacement.BottomFrontLeft:
                    style.alignment = TextAnchor.UpperLeft;
                    worldPos = GetCornerLabelWorldPosition(targetTransform, localBounds.center, localCorners[4], labelOffsetWorld);
                    break;

                case LabelPlacement.BottomFrontRight:
                    style.alignment = TextAnchor.UpperRight;
                    worldPos = GetCornerLabelWorldPosition(targetTransform, localBounds.center, localCorners[5], labelOffsetWorld);
                    break;

                case LabelPlacement.TopBackLeft:
                    style.alignment = TextAnchor.LowerLeft;
                    worldPos = GetCornerLabelWorldPosition(targetTransform, localBounds.center, localCorners[2], labelOffsetWorld);
                    break;

                case LabelPlacement.TopBackRight:
                    style.alignment = TextAnchor.LowerRight;
                    worldPos = GetCornerLabelWorldPosition(targetTransform, localBounds.center, localCorners[3], labelOffsetWorld);
                    break;

                case LabelPlacement.TopFrontLeft:
                    style.alignment = TextAnchor.LowerLeft;
                    worldPos = GetCornerLabelWorldPosition(targetTransform, localBounds.center, localCorners[6], labelOffsetWorld);
                    break;

                case LabelPlacement.TopFrontRight:
                    style.alignment = TextAnchor.LowerRight;
                    worldPos = GetCornerLabelWorldPosition(targetTransform, localBounds.center, localCorners[7], labelOffsetWorld);
                    break;

                default:
                    style.alignment = TextAnchor.MiddleCenter;
                    worldPos = targetTransform.TransformPoint(localBounds.center);
                    break;
            }

            Handles.Label(worldPos, text, style);
        }

        private static Vector3 GetCornerLabelWorldPosition(
            Transform targetTransform,
            Vector3 localCenter,
            Vector3 localCorner,
            float labelOffsetWorld)
        {
            Vector3 centerWorld = targetTransform.TransformPoint(localCenter);
            Vector3 cornerWorld = targetTransform.TransformPoint(localCorner);

            Vector3 outward = cornerWorld - centerWorld;
            if (outward.sqrMagnitude < 0.000001f)
                outward = targetTransform.up;

            return cornerWorld + outward.normalized * labelOffsetWorld;
        }

        private static Vector3[] GetBoxCornerPositions(Bounds bounds)
        {
            Vector3 c = bounds.center;
            Vector3 e = bounds.extents;

            return new Vector3[]
            {
                c + new Vector3(-e.x, -e.y, -e.z), // 0 LBB
                c + new Vector3( e.x, -e.y, -e.z), // 1 RBB
                c + new Vector3(-e.x,  e.y, -e.z), // 2 LTB
                c + new Vector3( e.x,  e.y, -e.z), // 3 RTB
                c + new Vector3(-e.x, -e.y,  e.z), // 4 LBF
                c + new Vector3( e.x, -e.y,  e.z), // 5 RBF
                c + new Vector3(-e.x,  e.y,  e.z), // 6 LTF
                c + new Vector3( e.x,  e.y,  e.z)  // 7 RTF
            };
        }
    }
#endif
}