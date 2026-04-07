using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public class CanvasInteractionState
    {
        public int HoveredPointId = -1;
        public int HoveredSegmentId = -1;

        public int PressedPointId = -1;
        public int PressedSegmentId = -1;

        public bool IsDraggingSelection;
        public bool IsPanning;
        public bool IsMarqueeSelecting;
        public bool IsDraggingDividerLikeThing; // future-proof if you want local canvas dividers/tools

        public Vector2 MouseDownScreenPosition;
        public Vector2 MouseDownCanvasPosition;

        public Vector2 LastMouseScreenPosition;
        public Vector2 CurrentMouseScreenPosition;

        public Rect MarqueeRectScreen;

        public bool HasMouseDown;
        public int ActiveMouseButton = -1;

        public Vector2 DragDeltaScreen => CurrentMouseScreenPosition - LastMouseScreenPosition;

        public void BeginMouseDown(Vector2 screenPos, Vector2 canvasPos, int mouseButton)
        {
            HasMouseDown = true;
            ActiveMouseButton = mouseButton;

            MouseDownScreenPosition = screenPos;
            MouseDownCanvasPosition = canvasPos;

            LastMouseScreenPosition = screenPos;
            CurrentMouseScreenPosition = screenPos;

            PressedPointId = HoveredPointId;
            PressedSegmentId = HoveredSegmentId;
        }

        public void UpdateMousePosition(Vector2 screenPos)
        {
            LastMouseScreenPosition = CurrentMouseScreenPosition;
            CurrentMouseScreenPosition = screenPos;
        }

        public void BeginDragSelection()
        {
            IsDraggingSelection = true;
            IsPanning = false;
            IsMarqueeSelecting = false;
        }

        public void BeginPan()
        {
            IsPanning = true;
            IsDraggingSelection = false;
            IsMarqueeSelecting = false;
        }

        public void BeginMarquee(Vector2 startScreenPos)
        {
            IsMarqueeSelecting = true;
            IsDraggingSelection = false;
            IsPanning = false;
            MarqueeRectScreen = new Rect(startScreenPos.x, startScreenPos.y, 0f, 0f);
        }

        public void UpdateMarquee(Vector2 currentScreenPos)
        {
            var min = Vector2.Min(MouseDownScreenPosition, currentScreenPos);
            var max = Vector2.Max(MouseDownScreenPosition, currentScreenPos);
            MarqueeRectScreen = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public bool HasMovedPastDragThreshold(float threshold = 4f)
        {
            return Vector2.Distance(MouseDownScreenPosition, CurrentMouseScreenPosition) >= threshold;
        }

        public void ClearHover()
        {
            HoveredPointId = -1;
            HoveredSegmentId = -1;
        }

        public void EndMouseInteraction()
        {
            HasMouseDown = false;
            ActiveMouseButton = -1;

            IsDraggingSelection = false;
            IsPanning = false;
            IsMarqueeSelecting = false;
            IsDraggingDividerLikeThing = false;

            PressedPointId = -1;
            PressedSegmentId = -1;

            MarqueeRectScreen = default;
        }
    }
}