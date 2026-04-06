using UnityEngine;
using System.Collections.Generic;

namespace DLN
{


    public static class RowUtil
    {
        public static void ArrangeInRow(
            List<GameObject> items,
            CartesianAxis direction,
            Vector3 anchor,
            float spacing,
            OptionalBoundsSettings boundsOverrides = default)
        {
            if (items == null || items.Count == 0)
                return;

            anchor = AdjustAnchor(direction, anchor);

            var prev = items[0];
            if (prev == null)
                return;

            Bnds.InterpolateBounds(
                @object: prev,
                t_x: anchor.x,
                t_y: anchor.y,
                t_z: anchor.z,
                result: out var cursor,
                outputCoordSpace: Space.World,
                overrides: boundsOverrides
            );

            for (int i = 1; i < items.Count; i++)
            {
                var current = items[i];
                if (current == null)
                    continue;

                Bnds.InterpolateBounds(
                    @object: current,
                    t_x: anchor.x,
                    t_y: anchor.y,
                    t_z: anchor.z,
                    result: out var anchorOnCurrent,
                    outputCoordSpace: Space.World,
                    overrides: boundsOverrides
                );

                var moveAmount = GetNextDistance(
                    current: prev.transform,
                    direction: direction,
                    spacing: spacing,
                    boundsOverrides: boundsOverrides);

                cursor += moveAmount;

                TransformUtils.MoveWithAnchor(
                    @object: current,
                    anchorPoint: anchorOnCurrent,
                    targetPoint: cursor,
                    outPosition: out _
                );

                prev = current;
            }
        }

        private static Vector3 AdjustAnchor(CartesianAxis direction, Vector3 anchor)
        {
            switch (direction)
            {
                case CartesianAxis.X:
                    return new Vector3(0f, anchor.y, anchor.z);
                case CartesianAxis.Y:
                    return new Vector3(anchor.x, 0f, anchor.z);
                case CartesianAxis.Z:
                    return new Vector3(anchor.x, anchor.y, 0f);
                default:
                    return Vector3.zero;
            }
        }

        private static Vector3 GetNextDistance(
            Transform current,
            CartesianAxis direction,
            float spacing,
            OptionalBoundsSettings boundsOverrides = default)
        {
            var moveAmount = Vector3.zero;

            var bounds = current.ToLocalBounds(overrides: boundsOverrides);

            Vector3 size = Vector3.zero;
            if (bounds.HasValue)
            {
                // This assumes ToWorldBounds() will also accept overrides,
                // or that you add an overload that does.
                var worldBounds = current.ToWorldBounds(overrides: boundsOverrides);
                size = worldBounds.size;
            }

            switch (direction)
            {
                case CartesianAxis.X:
                    moveAmount = new Vector3(size.x + spacing, 0f, 0f);
                    break;
                case CartesianAxis.Y:
                    moveAmount = new Vector3(0f, size.y + spacing, 0f);
                    break;
                case CartesianAxis.Z:
                    moveAmount = new Vector3(0f, 0f, size.z + spacing);
                    break;
            }

            return moveAmount;
        }
    }
}