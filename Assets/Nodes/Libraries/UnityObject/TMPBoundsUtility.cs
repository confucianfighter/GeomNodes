using UnityEngine;
using TMPro;

namespace DLN
{
    public static class TMPBoundsUtility
    {
        public static bool TryGetGlyphLocalMinMax(TMP_Text tmp, out Vector3 min, out Vector3 max)
        {
            min = Vector3.zero;
            max = Vector3.zero;

            if (tmp == null)
                return false;

            tmp.ForceMeshUpdate();

            var textInfo = tmp.textInfo;
            int charCount = textInfo.characterCount;

            bool foundAny = false;
            Vector3 currentMin = Vector3.positiveInfinity;
            Vector3 currentMax = Vector3.negativeInfinity;

            for (int i = 0; i < charCount; i++)
            {
                var c = textInfo.characterInfo[i];

                if (!c.isVisible)
                    continue;

                Encapsulate(ref currentMin, ref currentMax, c.bottomLeft);
                Encapsulate(ref currentMin, ref currentMax, c.topLeft);
                Encapsulate(ref currentMin, ref currentMax, c.topRight);
                Encapsulate(ref currentMin, ref currentMax, c.bottomRight);

                foundAny = true;
            }

            if (!foundAny)
                return false;

            min = currentMin;
            max = currentMax;
            return true;
        }

        public static bool TryGetGlyphLocalBounds(TMP_Text tmp, out Bounds bounds)
        {
            bounds = default;

            if (!TryGetGlyphLocalMinMax(tmp, out var min, out var max))
                return false;

            bounds = CreateBounds(min, max);
            return true;
        }

        public static bool TryGetGlyphLocalCorners(TMP_Text tmp, out Vector3[] corners)
        {
            corners = null;

            if (!TryGetGlyphLocalMinMax(tmp, out var min, out var max))
                return false;

            corners = Create2DCorners(min, max);
            return true;
        }

        public static bool TryGetGlyphBoundsInParentSpace(TMP_Text tmp, out Bounds bounds)
        {
            bounds = default;

            if (tmp == null)
                return false;

            Transform t = tmp.transform;
            Transform parent = t.parent;

            if (parent == null)
                return false;

            if (!TryGetGlyphLocalCorners(tmp, out var localCorners))
                return false;

            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            for (int i = 0; i < localCorners.Length; i++)
            {
                Vector3 worldCorner = t.TransformPoint(localCorners[i]);
                Vector3 parentCorner = parent.InverseTransformPoint(worldCorner);
                Encapsulate(ref min, ref max, parentCorner);
            }

            bounds = CreateBounds(min, max);
            return true;
        }

        public static bool TryGetGlyphCornersInParentSpace(TMP_Text tmp, out Vector3[] corners)
        {
            corners = null;

            if (!TryGetGlyphBoundsInParentSpace(tmp, out var bounds))
                return false;

            corners = Create2DCorners(bounds.min, bounds.max);
            return true;
        }

        private static void Encapsulate(ref Vector3 min, ref Vector3 max, Vector3 point)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        private static Bounds CreateBounds(Vector3 min, Vector3 max)
        {
            return new Bounds((min + max) * 0.5f, max - min);
        }

        private static Vector3[] Create2DCorners(Vector3 min, Vector3 max)
        {
            return new Vector3[]
            {
                new Vector3(min.x, min.y, 0f), // bottom-left
                new Vector3(min.x, max.y, 0f), // top-left
                new Vector3(max.x, max.y, 0f), // top-right
                new Vector3(max.x, min.y, 0f), // bottom-right
            };
        }
        private static Bounds GetTMPLocalBounds(TMP_Text tmp)
        {
            if (!TMPBoundsUtility.TryGetGlyphLocalBounds(tmp, out var bounds))
            {
                Debug.LogError("Couldn't get proper tmp_bounds in GetTMPLocalBounds");
            }
            return bounds;

        }
    }
}