using UnityEngine;

namespace DLN
{
    public static class InterfaceFinder
    {
        public static bool TryGetInterface<T>(GameObject go, out T result) where T : class
        {
            // Includes all components (MonoBehaviours + built-ins)
            var comps = go.GetComponents<Component>();

            for (int i = 0; i < comps.Length; i++)
            {
                var c = comps[i];
                if (!c) continue; // handles missing script / destroyed component (Unity "fake null")

                if (c is T t)
                {
                    result = t;
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
