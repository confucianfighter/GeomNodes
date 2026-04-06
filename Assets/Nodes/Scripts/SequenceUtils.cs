using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DLN
{
    public enum SequenceMode { RepeatLast, Cycle }

    public static class SequenceUtils
    {
        public static IEnumerable AsSequence(object input)
        {
            if (input is IEnumerable seq && !(input is string))
                return seq;
            else
                return new[] { input };
            throw new ArgumentException($"Cannot convert {input} to sequence of {typeof(object)}");
        }

        public static IEnumerable<(object, object)> ZipWith(object a, object b, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, seq2.Count);

            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count])
                );
            }
        }
        public static IEnumerable<(object, object, object)> ZipWith(object a, object b, object c, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, seq3.Count));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count])
                );
            }
        }
        public static IEnumerable<(object, object, object, object)> ZipWith(object a, object b, object c, object d, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            var seq4 = AsSequence(d).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, Math.Max(seq3.Count, seq4.Count)));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count]),
                    i < seq4.Count ? seq4[i] : (mode == SequenceMode.RepeatLast ? seq4.Last() : seq4[i % seq4.Count])
                );
            }
        }
        public static IEnumerable<(object, object, object, object, object)> ZipWith(object a, object b, object c, object d, object e, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            var seq4 = AsSequence(d).Cast<object>().ToList();
            var seq5 = AsSequence(e).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, Math.Max(seq3.Count, Math.Max(seq4.Count, seq5.Count))));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count]),
                    i < seq4.Count ? seq4[i] : (mode == SequenceMode.RepeatLast ? seq4.Last() : seq4[i % seq4.Count]),
                    i < seq5.Count ? seq5[i] : (mode == SequenceMode.RepeatLast ? seq5.Last() : seq5[i % seq5.Count])
                );
            }
        }
        public static IEnumerable<(object, object, object, object, object, object)> ZipWith(object a, object b, object c, object d, object e, object f, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            var seq4 = AsSequence(d).Cast<object>().ToList();
            var seq5 = AsSequence(e).Cast<object>().ToList();
            var seq6 = AsSequence(f).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, Math.Max(seq3.Count, Math.Max(seq4.Count, Math.Max(seq5.Count, seq6.Count)))));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count]),
                    i < seq4.Count ? seq4[i] : (mode == SequenceMode.RepeatLast ? seq4.Last() : seq4[i % seq4.Count]),
                    i < seq5.Count ? seq5[i] : (mode == SequenceMode.RepeatLast ? seq5.Last() : seq5[i % seq5.Count]),
                    i < seq6.Count ? seq6[i] : (mode == SequenceMode.RepeatLast ? seq6.Last() : seq6[i % seq6.Count])
                );
            }
        }
        // now for 7 items
        public static IEnumerable<(object, object, object, object, object, object, object)> ZipWith(object a, object b, object c, object d, object e, object f, object g, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            var seq4 = AsSequence(d).Cast<object>().ToList();
            var seq5 = AsSequence(e).Cast<object>().ToList();
            var seq6 = AsSequence(f).Cast<object>().ToList();
            var seq7 = AsSequence(g).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, Math.Max(seq3.Count, Math.Max(seq4.Count, Math.Max(seq5.Count, Math.Max(seq6.Count, seq7.Count))))));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count]),
                    i < seq4.Count ? seq4[i] : (mode == SequenceMode.RepeatLast ? seq4.Last() : seq4[i % seq4.Count]),
                    i < seq5.Count ? seq5[i] : (mode == SequenceMode.RepeatLast ? seq5.Last() : seq5[i % seq5.Count]),
                    i < seq6.Count ? seq6[i] : (mode == SequenceMode.RepeatLast ? seq6.Last() : seq6[i % seq6.Count]),
                    i < seq7.Count ? seq7[i] : (mode == SequenceMode.RepeatLast ? seq7.Last() : seq7[i % seq7.Count])
                );
            }
        }
        public static IEnumerable<(object, object, object, object, object, object, object, object)> ZipWith(object a, object b, object c, object d, object e, object f, object g, object h, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            var seq4 = AsSequence(d).Cast<object>().ToList();
            var seq5 = AsSequence(e).Cast<object>().ToList();
            var seq6 = AsSequence(f).Cast<object>().ToList();
            var seq7 = AsSequence(g).Cast<object>().ToList();
            var seq8 = AsSequence(h).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, Math.Max(seq3.Count, Math.Max(seq4.Count, Math.Max(seq5.Count, Math.Max(seq6.Count, Math.Max(seq7.Count, seq8.Count)))))));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count]),
                    i < seq4.Count ? seq4[i] : (mode == SequenceMode.RepeatLast ? seq4.Last() : seq4[i % seq4.Count]),
                    i < seq5.Count ? seq5[i] : (mode == SequenceMode.RepeatLast ? seq5.Last() : seq5[i % seq5.Count]),
                    i < seq6.Count ? seq6[i] : (mode == SequenceMode.RepeatLast ? seq6.Last() : seq6[i % seq6.Count]),
                    i < seq7.Count ? seq7[i] : (mode == SequenceMode.RepeatLast ? seq7.Last() : seq7[i % seq7.Count]),
                    i < seq8.Count ? seq8[i] : (mode == SequenceMode.RepeatLast ? seq8.Last() : seq8[i % seq8.Count])
                );
            }
        }
        public static IEnumerable<(object, object, object, object, object, object, object, object, object)> ZipWith(object a, object b, object c, object d, object e, object f, object g, object h, object ii, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            var seq4 = AsSequence(d).Cast<object>().ToList();
            var seq5 = AsSequence(e).Cast<object>().ToList();
            var seq6 = AsSequence(f).Cast<object>().ToList();
            var seq7 = AsSequence(g).Cast<object>().ToList();
            var seq8 = AsSequence(h).Cast<object>().ToList();
            var seq9 = AsSequence(ii).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, Math.Max(seq3.Count, Math.Max(seq4.Count, Math.Max(seq5.Count, Math.Max(seq6.Count, Math.Max(seq7.Count, Math.Max(seq8.Count, seq9.Count))))))));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count]),
                    i < seq4.Count ? seq4[i] : (mode == SequenceMode.RepeatLast ? seq4.Last() : seq4[i % seq4.Count]),
                    i < seq5.Count ? seq5[i] : (mode == SequenceMode.RepeatLast ? seq5.Last() : seq5[i % seq5.Count]),
                    i < seq6.Count ? seq6[i] : (mode == SequenceMode.RepeatLast ? seq6.Last() : seq6[i % seq6.Count]),
                    i < seq7.Count ? seq7[i] : (mode == SequenceMode.RepeatLast ? seq7.Last() : seq7[i % seq7.Count]),
                    i < seq8.Count ? seq8[i] : (mode == SequenceMode.RepeatLast ? seq8.Last() : seq8[i % seq8.Count]),
                    i < seq9.Count ? seq9[i] : (mode == SequenceMode.RepeatLast ? seq9.Last() : seq9[i % seq9.Count])
                );
            }
        }
        public static IEnumerable<(object, object, object, object, object, object, object, object, object, object)> ZipWith(object a, object b, object c, object d, object e, object f, object g, object h, object ii, object j, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            var seq4 = AsSequence(d).Cast<object>().ToList();
            var seq5 = AsSequence(e).Cast<object>().ToList();
            var seq6 = AsSequence(f).Cast<object>().ToList();
            var seq7 = AsSequence(g).Cast<object>().ToList();
            var seq8 = AsSequence(h).Cast<object>().ToList();
            var seq9 = AsSequence(ii).Cast<object>().ToList();
            var seq10 = AsSequence(j).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, Math.Max(seq3.Count, Math.Max(seq4.Count, Math.Max(seq5.Count, Math.Max(seq6.Count, Math.Max(seq7.Count, Math.Max(seq8.Count, Math.Max(seq9.Count, seq10.Count)))))))));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count]),
                    i < seq4.Count ? seq4[i] : (mode == SequenceMode.RepeatLast ? seq4.Last() : seq4[i % seq4.Count]),
                    i < seq5.Count ? seq5[i] : (mode == SequenceMode.RepeatLast ? seq5.Last() : seq5[i % seq5.Count]),
                    i < seq6.Count ? seq6[i] : (mode == SequenceMode.RepeatLast ? seq6.Last() : seq6[i % seq6.Count]),
                    i < seq7.Count ? seq7[i] : (mode == SequenceMode.RepeatLast ? seq7.Last() : seq7[i % seq7.Count]),
                    i < seq8.Count ? seq8[i] : (mode == SequenceMode.RepeatLast ? seq8.Last() : seq8[i % seq8.Count]),
                    i < seq9.Count ? seq9[i] : (mode == SequenceMode.RepeatLast ? seq9.Last() : seq9[i % seq9.Count]),
                    i < seq10.Count ? seq10[i] : (mode == SequenceMode.RepeatLast ? seq10.Last() : seq10[i % seq10.Count])
                );
            }
        }
        // now for 11 items
        public static IEnumerable<(object, object, object, object, object, object, object, object, object, object, object)> ZipWith(object a, object b, object c, object d, object e, object f, object g, object h, object ii, object j, object k, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            var seq4 = AsSequence(d).Cast<object>().ToList();
            var seq5 = AsSequence(e).Cast<object>().ToList();
            var seq6 = AsSequence(f).Cast<object>().ToList();
            var seq7 = AsSequence(g).Cast<object>().ToList();
            var seq8 = AsSequence(h).Cast<object>().ToList();
            var seq9 = AsSequence(ii).Cast<object>().ToList();
            var seq10 = AsSequence(j).Cast<object>().ToList();
            var seq11 = AsSequence(k).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, Math.Max(seq3.Count, Math.Max(seq4.Count, Math.Max(seq5.Count, Math.Max(seq6.Count, Math.Max(seq7.Count, Math.Max(seq8.Count, Math.Max(seq9.Count, Math.Max(seq10.Count, seq11.Count))))))))));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count]),
                    i < seq4.Count ? seq4[i] : (mode == SequenceMode.RepeatLast ? seq4.Last() : seq4[i % seq4.Count]),
                    i < seq5.Count ? seq5[i] : (mode == SequenceMode.RepeatLast ? seq5.Last() : seq5[i % seq5.Count]),
                    i < seq6.Count ? seq6[i] : (mode == SequenceMode.RepeatLast ? seq6.Last() : seq6[i % seq6.Count]),
                    i < seq7.Count ? seq7[i] : (mode == SequenceMode.RepeatLast ? seq7.Last() : seq7[i % seq7.Count]),
                    i < seq8.Count ? seq8[i] : (mode == SequenceMode.RepeatLast ? seq8.Last() : seq8[i % seq8.Count]),
                    i < seq9.Count ? seq9[i] : (mode == SequenceMode.RepeatLast ? seq9.Last() : seq9[i % seq9.Count]),
                    i < seq10.Count ? seq10[i] : (mode == SequenceMode.RepeatLast ? seq10.Last() : seq10[i % seq10.Count]),
                    i < seq11.Count ? seq11[i] : (mode == SequenceMode.RepeatLast ? seq11.Last() : seq11[i % seq11.Count])
                );
            }
        }
        // now for 12 items
        public static IEnumerable<(object, object, object, object, object, object, object, object, object, object, object, object)> ZipWith(object a, object b, object c, object d, object e, object f, object g, object h, object ii, object j, object k, object l, SequenceMode mode = SequenceMode.RepeatLast)
        {
            var seq1 = AsSequence(a).Cast<object>().ToList();
            var seq2 = AsSequence(b).Cast<object>().ToList();
            var seq3 = AsSequence(c).Cast<object>().ToList();
            var seq4 = AsSequence(d).Cast<object>().ToList();
            var seq5 = AsSequence(e).Cast<object>().ToList();
            var seq6 = AsSequence(f).Cast<object>().ToList();
            var seq7 = AsSequence(g).Cast<object>().ToList();
            var seq8 = AsSequence(h).Cast<object>().ToList();
            var seq9 = AsSequence(ii).Cast<object>().ToList();
            var seq10 = AsSequence(j).Cast<object>().ToList();
            var seq11 = AsSequence(k).Cast<object>().ToList();
            var seq12 = AsSequence(l).Cast<object>().ToList();
            int max = Math.Max(seq1.Count, Math.Max(seq2.Count, Math.Max(seq3.Count, Math.Max(seq4.Count, Math.Max(seq5.Count, Math.Max(seq6.Count, Math.Max(seq7.Count, Math.Max(seq8.Count, Math.Max(seq9.Count, Math.Max(seq10.Count, Math.Max(seq11.Count, seq12.Count)))))))))));
            for (int i = 0; i < max; i++)
            {
                yield return (
                    i < seq1.Count ? seq1[i] : (mode == SequenceMode.RepeatLast ? seq1.Last() : seq1[i % seq1.Count]),
                    i < seq2.Count ? seq2[i] : (mode == SequenceMode.RepeatLast ? seq2.Last() : seq2[i % seq2.Count]),
                    i < seq3.Count ? seq3[i] : (mode == SequenceMode.RepeatLast ? seq3.Last() : seq3[i % seq3.Count]),
                    i < seq4.Count ? seq4[i] : (mode == SequenceMode.RepeatLast ? seq4.Last() : seq4[i % seq4.Count]),
                    i < seq5.Count ? seq5[i] : (mode == SequenceMode.RepeatLast ? seq5.Last() : seq5[i % seq5.Count]),
                    i < seq6.Count ? seq6[i] : (mode == SequenceMode.RepeatLast ? seq6.Last() : seq6[i % seq6.Count]),
                    i < seq7.Count ? seq7[i] : (mode == SequenceMode.RepeatLast ? seq7.Last() : seq7[i % seq7.Count]),
                    i < seq8.Count ? seq8[i] : (mode == SequenceMode.RepeatLast ? seq8.Last() : seq8[i % seq8.Count]),
                    i < seq9.Count ? seq9[i] : (mode == SequenceMode.RepeatLast ? seq9.Last() : seq9[i % seq9.Count]),
                    i < seq10.Count ? seq10[i] : (mode == SequenceMode.RepeatLast ? seq10.Last() : seq10[i % seq10.Count]),
                    i < seq11.Count ? seq11[i] : (mode == SequenceMode.RepeatLast ? seq11.Last() : seq11[i % seq11.Count]),
                    i < seq12.Count ? seq12[i] : (mode == SequenceMode.RepeatLast ? seq12.Last() : seq12[i % seq12.Count])
                );
            }
        }
        public static IEnumerable<object> Flatten(object input, int depth = 1)
        {
            // Step 0: turn anything into an IEnumerable<object>
            var seq = (input is IEnumerable e && !(input is string))
                ? e.Cast<object>()
                : new[] { input };

            return FlattenRec(seq, depth);
        }

        private static IEnumerable<object> FlattenRec(IEnumerable<object> seq, int depth)
        {
            // If we've reached “0”, group whatever’s left as a single IEnumerable<object>
            if (depth == 0)
            {
                yield return seq;
                yield break;
            }

            foreach (var item in seq)
            {
                // If this item is another non-string IEnumerable, recurse one level
                if (item is IEnumerable inner && !(item is string))
                {
                    foreach (var sub in FlattenRec(inner.Cast<object>(), depth - 1))
                        yield return sub;
                }
                else
                {
                    // Otherwise it’s atomic—just yield it
                    yield return item;
                }
            }
        }
        // FLATTEN RANGE
        public static void FlattenRangeNode(
            object input,
            int startDepth,
            int endDepth,
            out IEnumerable<object> result)
        {
            var output = new List<object>();
            var seq = (input is IEnumerable e && !(input is string))
                ? e.Cast<object>()
                : new[] { input };
            FlattenRec(seq, 0, startDepth, endDepth, output);
            result = output;
        }

        private static void FlattenRec(
            IEnumerable<object> seq,
            int currentDepth,
            int startDepth,
            int endDepth,
            List<object> output)
        {
            foreach (var item in seq)
            {
                if (item is IEnumerable inner && !(item is string))
                {
                    if (currentDepth < startDepth)
                    {
                        output.Add(inner.Cast<object>());
                    }
                    else if (currentDepth < endDepth)
                    {
                        FlattenRec(inner.Cast<object>(), currentDepth + 1, startDepth, endDepth, output);
                    }
                    else
                    {
                        output.Add(inner.Cast<object>());
                    }
                }
                else
                {
                    output.Add(item);
                }
            }
        }

        // UNSQUEEZE (ENCAPSULATE) AT DEPTH
        public static void UnsqueezeNode(
            object input,
            int targetDepth,
            out IEnumerable<object> result)
        {
            var output = new List<object>();
            var seq = (input is IEnumerable e && !(input is string))
                ? e.Cast<object>()
                : new[] { input };
            UnsqueezeRec(seq, 0, targetDepth, output);
            result = output;
        }

        private static void UnsqueezeRec(
            IEnumerable<object> seq,
            int currentDepth,
            int targetDepth,
            List<object> output)
        {
            foreach (var item in seq)
            {
                if (item is IEnumerable inner && !(item is string))
                {
                    UnsqueezeRec(inner.Cast<object>(), currentDepth + 1, targetDepth, output);
                }
                else if (currentDepth == targetDepth)
                {
                    output.Add(new List<object> { item });
                }
                else
                {
                    output.Add(item);
                }
            }
        }



    }
}
