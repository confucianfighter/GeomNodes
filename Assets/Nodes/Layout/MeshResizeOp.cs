namespace DLN
{
    public abstract class MeshResizeOp : LayoutOp
    {
        public sealed override void Execute()
        {
            UpdateDeferredBounds();
            C3DLS_ExecuteDepthFirst.RegisterDeferredMeshResizeOp(this);
        }

        protected abstract void UpdateDeferredBounds();
        public abstract void ExecuteResize();
    }
}