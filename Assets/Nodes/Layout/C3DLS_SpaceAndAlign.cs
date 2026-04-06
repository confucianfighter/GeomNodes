using UnityEngine;

namespace DLN
{
    public class C3DLS_SpaceAndAlign : LayoutOp
    {
        public Transform rowParent;
        public float spacing = 0.001f;
        public CartesianAxis direction = CartesianAxis.X;
        public Vector3 anchor = Vector3.zero;

        [Header("Bounds Overrides")]
        public OptionalBoundsSettings boundsOverrides = OptionalBoundsSettings.Empty;

        void Awake()
        {
            if (rowParent == null)
                rowParent = transform;
        }

        void Reset()
        {
            boundsOverrides.SetRegionSelection(RegionSelection.Contents);
        }

        [ContextMenu("SpaceAndAlign()")]
        public override void Execute()
        {
            if (rowParent == null)
            {
                Debug.LogError($"Missing rowParent in {name} => {gameObject.name}");
                return;
            }

            var items = GameObjectUtils.GetAllChildren(rowParent);

            RowUtil.ArrangeInRow(
                items: items,
                direction: direction,
                anchor: anchor,
                spacing: spacing,
                boundsOverrides: boundsOverrides);
        }
    }
}