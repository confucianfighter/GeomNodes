using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace DLN
{
    public static class LayoutUtils
    {
        public static void SpaceEvenlyAlongWorldAxis(
            List<UnityEngine.Object> items,
            out Transform alignedTransform,
            float spacing = 0.2f,
            Vector3 axis = default,
            Space getOrientedBoundsSpace = Space.World,
            OptionalBoundsSettings boundsOverrides = default
        )
        {
            axis = axis == Vector3.zero ? Vector3.forward : axis.normalized;
            alignedTransform = new GameObject("TempTransform").transform;

            var itemList = SequenceUtils.AsSequence(items).Cast<UnityEngine.Object>().ToList();
            UnityEngine.Object previousItem = null;

            foreach (var (item, index) in itemList.Select((value, i) => (value, i)))
            {
                if (previousItem == null)
                {
                    previousItem = item;
                    continue;
                }

                Bnds.GetOrientedBounds(
                    refSpace: alignedTransform,
                    @object: previousItem,
                    center: out var prevCenter,
                    extents: out var prevExtents,
                    size: out var prevSize,
                    min: out _,
                    max: out _,
                    overrides: boundsOverrides
                );

                Bnds.GetOrientedBounds(
                    refSpace: alignedTransform,
                    @object: item,
                    center: out var currentCenter,
                    extents: out var currentExtents,
                    size: out var currentSize,
                    min: out _,
                    max: out _,
                    overrides: boundsOverrides
                );

                var distance = prevExtents.z + spacing + currentExtents.z;

                var startMove = alignedTransform.TransformPoint(currentCenter);
                currentCenter.z = prevCenter.z + distance;
                var endMove = alignedTransform.TransformPoint(currentCenter);

                TransformUtils.MoveWithAnchor(
                    @object: item,
                    anchorPoint: startMove,
                    targetPoint: endMove,
                    outPosition: out _
                );

                previousItem = item;
            }
        }
    }
}