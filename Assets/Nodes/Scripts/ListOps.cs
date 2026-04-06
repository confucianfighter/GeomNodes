using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using System;

namespace DLN
{
    public static class ListOps
    {
        // remove first item from list
        public static void RemoveFirstFromList(object list, out List<object> outList, out object firstItem)
        {
            if (list is IList<object> ilist && ilist.Count > 0)
            {
                outList = new List<object>(ilist);
                firstItem = outList.First();
                outList.RemoveAt(0);
            }
            else if (list is IList ilist2 && ilist2.Count > 0)
            {
                outList = new List<object>(ilist2.Cast<object>());
                firstItem = outList.First();
                outList.RemoveAt(0);
            }
            else
            {
                throw new ArgumentException($"Cannot remove first item from {list.GetType()}");
            }
        }
        public static void ShiftList(object list, int shiftAmount, out List<object> outList)
        {
            outList = new List<object>();
            // negative shiftAmount means shift to the left
            // if shifting left, remove item from the front of the list and add it to the end.
            if (shiftAmount < 0)
            {
                shiftAmount = -shiftAmount;
                if (list is IList<object> ilist && ilist.Count > 0)
                {
                    outList = new List<object>(ilist);
                    for (int i = 0; i < shiftAmount; i++)
                    {
                        if (outList.Count > 0)
                        {
                            var item = outList.Last();
                            outList.RemoveAt(0);
                            outList.Add(item);
                        }
                    }
                }
                else if (list is IList ilist2 && ilist2.Count > 0)
                {
                    outList = new List<object>(ilist2.Cast<object>());
                    for (int i = 0; i < shiftAmount; i++)
                    {
                        if (outList.Count > 0)
                        {
                            var item = outList.Last();
                            outList.RemoveAt(0);
                            outList.Add(item);
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Cannot shift {list.GetType()}");
                }
            }
            else if (shiftAmount > 0)
            {
                // positive shiftAmount means shift to the right
                // if shifting right, remove item from the end of the list and add it to the front.
                if (list is IList<object> ilist && ilist.Count > 0)
                {
                    outList = new List<object>(ilist);
                    for (int i = 0; i < shiftAmount; i++)
                    {
                        if (outList.Count > 0)
                        {
                            var item = outList.First();
                            outList.RemoveAt(outList.Count - 1);
                            outList.Insert(0, item);
                        }
                    }
                }
                else if (list is IList ilist2 && ilist2.Count > 0)
                {
                    outList = new List<object>(ilist2.Cast<object>());
                    for (int i = 0; i < shiftAmount; i++)
                    {
                        if (outList.Count > 0)
                        {
                            var item = outList.First();
                            outList.RemoveAt(outList.Count - 1);
                            outList.Insert(0, item);
                        }
                    }
                }
                else
                {
                    throw new ArgumentException($"Cannot shift {list.GetType()}");
                }
            }

        }
        // remove last item from list
        public static void RemoveLastFromList(object list, out List<object> outList, out object lastItem)
        {
            if (list is IList<object> ilist && ilist.Count > 0)
            {
                outList = new List<object>(ilist);
                lastItem = outList.Last();
                outList.RemoveAt(ilist.Count - 1);
            }
            else if (list is IList ilist2 && ilist2.Count > 0)
            {
                outList = new List<object>(ilist2.Cast<object>());
                lastItem = outList.Last();
                outList.RemoveAt(ilist2.Count - 1);
            }
            else
            {
                throw new ArgumentException($"Cannot remove last item from {list.GetType()}");
            }
        }

        public static void AddToList(object item, object list, out List<object> outList)
        {
            if (list is IList<object> ilist)
            {
                outList = new List<object>(ilist);
                outList.Add(item);
            }
            else if (list is IList ilist2)
            {
                outList = new List<object>(ilist2.Cast<object>());
                outList.Add(item);
            }
            else
            {
                throw new ArgumentException($"Cannot add item to {list.GetType()}");
            }
        }

        public static void RemoveFromList(object item, object list)
        {
            if (list is IList<object> ilist)
            {
                ilist.Remove(item);
            }
            else if (list is IList)
            {
                ((IList)list).Remove(item);
            }
            else
            {
                throw new ArgumentException($"Cannot remove item from {list.GetType()}");
            }
        }
        public static IEnumerable<object> AsDepthFirstSequence(object input)
        {
            if (input is IEnumerable seq && !(input is string))
                foreach (var child in seq.Cast<object>())
                    foreach (var leaf in AsDepthFirstSequence(child))
                        yield return leaf;
            else
                yield return input;
        }

        /// <summary>
        /// template: any nested IEnumerable (your shape)
        /// values: any nested IEnumerable (your values, no need to be flat)
        /// shaped: out nested List<object> mirroring template, filled from values in depth-first order
        /// </summary>
        public static void MatchShapeDepthFirst(
            object template,
            object list,
            out object reshapedList)
        {
            // run the recursive iterator on values here:
            var flatEnum = AsDepthFirstSequence(list).GetEnumerator();
            reshapedList = MatchRec(template, flatEnum);

            if (flatEnum.MoveNext())
                Debug.LogWarning(
                    $"MatchShapeDepthFirst: {CountRemaining(flatEnum)} extra items were ignored.");
        }

        private static object MatchRec(object node, IEnumerator flatEnum)
        {
            if (node is IEnumerable seq && !(node is string))
            {
                var output = new List<object>();
                foreach (var child in seq.Cast<object>())
                    output.Add(MatchRec(child, flatEnum));
                return output;
            }
            else
            {
                if (flatEnum.MoveNext())
                    return flatEnum.Current;
                throw new ArgumentException("Not enough items to match template shape.");
            }
        }

        private static int CountRemaining(IEnumerator flatEnum)
        {
            int count = 0;
            do { count++; }
            while (flatEnum.MoveNext());
            return count;
        }
    }
}
