#if false
using System.Collections.Generic;
using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public class CanvasInteractionState
    {
        public CanvasElementRef Hovered;
        public CanvasElementRef Pressed;
        public CanvasElementRef Dragging;
        public CanvasElementRef MouseDownElement;

        public bool IsDragging;
        public bool IsDraggingPoints;
        public bool IsPanning;
        public bool IsMarqueeSelecting;

        public Vector2 MouseDownScreen;
        public Vector2 MouseDownCanvas;
        public Vector2 LastMouseScreen;
        public Vector2 LastMouseCanvas;
        public Vector2 DragStartCanvas;

        public Vector2 MarqueeStartScreen;
        public Vector2 MarqueeEndScreen;

        public HashSet<CanvasElementRef> MarqueeSelectionSnapshot = new();
        public Dictionary<int, Vector2> DragPointStartPositions = new();

        public void Clear()
        {
            Hovered = default;
            Pressed = default;
            Dragging = default;
            MouseDownElement = default;

            IsDragging = false;
            IsDraggingPoints = false;
            IsPanning = false;
            IsMarqueeSelecting = false;

            MouseDownScreen = default;
            MouseDownCanvas = default;
            LastMouseScreen = default;
            LastMouseCanvas = default;
            DragStartCanvas = default;

            MarqueeStartScreen = default;
            MarqueeEndScreen = default;

            MarqueeSelectionSnapshot.Clear();
            DragPointStartPositions.Clear();
        }
    }
}
#endif
