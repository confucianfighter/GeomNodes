using UnityEngine;

namespace DLN.EditorTools.ShapeStamper
{
    public interface ICanvasBoundsProvider
    {
        Rect GetCanvasFrameRect();
    }
}