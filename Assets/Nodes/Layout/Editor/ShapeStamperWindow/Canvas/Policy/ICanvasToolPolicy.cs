public interface ICanvasToolPolicy
{
    bool CanCloseLoop { get; }
    bool AllowEdgeSplit { get; }
    bool AllowOffsets { get; }

    void OnBeginDrag(EditorCanvas canvas);
    void OnSelectionChanged(EditorCanvas canvas);
    void DrawOverlay(EditorCanvas canvas);
}