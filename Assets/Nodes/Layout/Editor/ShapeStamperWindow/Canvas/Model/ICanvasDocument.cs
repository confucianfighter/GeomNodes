public interface ICanvasDocument
{
    IList<CanvasPoint> Points { get; }
    IList<CanvasEdge> Edges { get; }
    IList<CanvasOffsetConstraint> Offsets { get; }

    bool IsClosed { get; }
    void MarkDirty();
}