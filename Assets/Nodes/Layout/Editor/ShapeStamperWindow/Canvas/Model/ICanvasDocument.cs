using System.Collections.Generic;
using UnityEngine;
namespace DLN.EditorTools.ShapeStamper
{

public interface ICanvasDocument
{
    IList<CanvasPoint> Points { get; }
    IList<CanvasEdge> Edges { get; }
    IList<CanvasOffsetConstraint> Offsets { get; }

    bool IsClosed { get; }
    void MarkDirty();
}
}