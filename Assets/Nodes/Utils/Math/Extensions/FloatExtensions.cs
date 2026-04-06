using UnityEngine;

namespace DLN
{
    public static class FloatExtensions
    {
        /// <summary>
        /// Remaps the input <paramref name="value"/> from the range [fromStart, fromEnd]
        /// into the range [toStart, toEnd]. 
        /// </summary>
        /// <param name="value">The float to remap.</param>
        /// <param name="fromStart">Lower bound of the input range.</param>
        /// <param name="fromEnd">Upper bound of the input range.</param>
        /// <param name="toStart">Lower bound of the output range.</param>
        /// <param name="toEnd">Upper bound of the output range.</param>
        /// <returns>
        /// The remapped float. If fromEnd == fromStart, returns toStart (to avoid divide-by-zero).
        /// </returns>
        public static void Remap(this float value, float fromStart, float fromEnd, float toStart, float toEnd, out float newValue)
        {
            float fromDelta = fromEnd - fromStart;
            float toDelta = toEnd - toStart;

            float normalized = (value - fromStart) / fromDelta;
            newValue = toStart + normalized * toDelta;
        }
    }
}