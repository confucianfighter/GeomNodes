using System.Collections.Generic;
using System.Linq;
using System;

namespace DLN
{
    public static class Iterators
    {
        public static IEnumerable<(T current, T next)> CurrentAndNext<T>(this IReadOnlyList<T> items)
        {
            for (int i = 0; i < items.Count - 1; i++)
                yield return (items[i], items[i + 1]);
        }
        public static IEnumerable<((T current, T next), (U current, U next))> CurrentAndNextZip<T, U>(
    IReadOnlyList<T> first,
    IReadOnlyList<U> second)
        {
            return CurrentAndNext(first).Zip(CurrentAndNext(second), (a, b) => (a, b));
        }
        public static IEnumerable<(T current, T other)> OffsetPairs<T>(
    this IReadOnlyList<T> items,
    int offset)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));

            if (offset <= 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be greater than zero.");

            for (int i = 0; i < items.Count - offset; i++)
                yield return (items[i], items[i + offset]);
        }

    }
}
