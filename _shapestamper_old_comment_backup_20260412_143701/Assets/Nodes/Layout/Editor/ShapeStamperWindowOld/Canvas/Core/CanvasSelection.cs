using System.Collections.Generic;
using UnityEngine;
namespace DLN.EditorTools.ShapeStamper
{
    [System.Serializable]
    public class CanvasSelection
    {
        private readonly HashSet<CanvasElementRef> _elements = new();

        public IEnumerable<CanvasElementRef> Elements => _elements;
        public int Count => _elements.Count;

        public void Add(CanvasElementRef element)
        {
            if (!element.IsValid)
                return;

            _elements.Add(element);
        }

        public void Remove(CanvasElementRef element)
        {
            _elements.Remove(element);
        }

        public bool Contains(CanvasElementRef element)
        {
            return _elements.Contains(element);
        }

        public void Clear()
        {
            _elements.Clear();
        }

        public HashSet<CanvasElementRef> CloneElements()
        {
            return new HashSet<CanvasElementRef>(_elements);
        }

        public void SetElements(HashSet<CanvasElementRef> elements)
        {
            _elements.Clear();

            if (elements == null)
                return;

            foreach (var element in elements)
                _elements.Add(element);
        }
    }
}