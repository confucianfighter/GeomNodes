#if false
using System.Collections.Generic;

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
#endif
