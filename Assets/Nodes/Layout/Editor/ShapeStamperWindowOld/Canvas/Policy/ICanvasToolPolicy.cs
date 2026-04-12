#if false
using UnityEditor;
using UnityEngine;


namespace DLN.EditorTools.ShapeStamper
{
    public interface ICanvasToolPolicy
    {
        void DrawOverlay(EditorCanvas canvas, Rect canvasRect);
        void OnMouseDown(EditorCanvas canvas, Event evt);
        void OnDrag(EditorCanvas canvas, Event evt);
        void OnClick(EditorCanvas canvas, Event evt);
        void OnKeyDown(EditorCanvas canvas, Event evt);

        void AddPointAtCanvasPosition(EditorCanvas canvas, Vector2 canvasPos);
        void SplitEdgeAtScreenPosition(EditorCanvas canvas, CanvasElementRef edgeRef, Vector2 screenPos);
        void DeleteSelection(EditorCanvas canvas);
        void ConstrainDraggedPoint(EditorCanvas canvas, int pointId, ref Vector2 position);
    }
}
#endif
