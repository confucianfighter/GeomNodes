namespace DLN
{
    /// <summary>
    /// Implemented by UI row components that can refresh themselves from their bound getter.
    /// </summary>
    public interface IBoundRow
    {
        void Refresh();
    }
}
