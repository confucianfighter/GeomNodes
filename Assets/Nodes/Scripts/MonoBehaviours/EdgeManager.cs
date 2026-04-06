using System.Collections.Generic;
using UnityEngine;

namespace DLN
{
    public enum CRUDResult
    {
        Created,
        Updated,
        Deleted,
        AlreadyExisted,
        NotFound
    }

    public sealed class EdgeManager : MonoBehaviour
    {
        public static EdgeManager Instance { get; private set; }

        [SerializeField] private Edge edgePrefab;

        // Key: (startBinding, endBinding)
        private static readonly Dictionary<(int, int), Edge> edges = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Call this to wipe all cached edges (useful for playmode resets).
        /// </summary>
        public static void ClearAll()
        {
            edges.Clear();
        }

        /// <summary>
        /// Ensures we don't treat "destroyed edge" as existing.
        /// In Unity, a destroyed object can compare equal to null.
        /// </summary>
        private static bool TryGetLiveEdge((int, int) key, out Edge edge)
        {
            if (edges.TryGetValue(key, out edge))
            {
                if (edge == null) // destroyed or missing reference
                {
                    edges.Remove(key);
                    edge = null;
                    return false;
                }
                return true;
            }
            edge = null;
            return false;
        }

        private static Edge CreateEdge((int, int) key)
        {
            if (Instance == null)
            {
                Debug.LogError("EdgeManager.Instance is null. Ensure an EdgeManager exists in the scene.");
                return null;
            }
            if (Instance.edgePrefab == null)
            {
                Debug.LogError("EdgeManager.edgePrefab is not assigned.");
                return null;
            }

            var edge = Instantiate(Instance.edgePrefab, Instance.transform);
            edges[key] = edge;
            return edge;
        }

        public static CRUDResult CreateIfNotExists((int, int) key, out Edge edge, IPortBase port)
        {
            if (port == null)
            {
                edge = null;
                Debug.LogError("CreateIfNotExists called with null port.");
                return CRUDResult.NotFound;
            }

            if (TryGetLiveEdge(key, out edge))
            {
                BindVisibilityEvents(port, edge);
                return CRUDResult.AlreadyExisted;
            }

            edge = CreateEdge(key);
            if (edge == null)
                return CRUDResult.NotFound;

            BindVisibilityEvents(port, edge);
            return CRUDResult.Created;
        }

        private static void BindVisibilityEvents(IPortBase port, Edge edge)
        {
            // Prevent duplicate subscriptions
            port.onEnable.RemoveListener(edge.UpdateVisibility);
            port.onDisable.RemoveListener(edge.UpdateVisibility);

            port.onEnable.AddListener(edge.UpdateVisibility);
            port.onDisable.AddListener(edge.UpdateVisibility);

            // Optional: update once immediately so it reflects current state
            edge.UpdateVisibility();
        }
    }
}
