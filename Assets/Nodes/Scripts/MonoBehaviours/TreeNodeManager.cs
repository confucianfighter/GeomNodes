using UnityEngine;
using System.Collections.Generic;

namespace DLN
{
    // A singleton class that retrieves and caches ContextMenu instances
    // Context menus are parented under this GameObject
    public class TreeNodeManager : MonoBehaviour
    {
        public static TreeNodeManager Instance { get; private set; }

        [Tooltip("Prefab must include (or allow adding) a DestroyNotifier")]
        public Title defaultPrefab;

        // KV store: target instanceID -> spawned context menu
        [SerializeField] private readonly Dictionary<int, GameObject> _cache = new();

        // Tracks which targets we’ve already subscribed to
        [SerializeField] private readonly HashSet<int> _registered = new();
        public Transform menuSpawnTarget;

        [ContextMenu("Awake")]
        public void Awake()
        {
            SetSingletonInstance();
        }
        public void SetSingletonInstance()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // Convenience overload
        public GameObject GetAndCreateIfNotExists(GameObject target)
        {
            return GetAndCreateIfNotExists(posTarget: target.gameObject, dataTarget: target.gameObject, prefab: defaultPrefab);
        }

        public GameObject GetAndCreateIfNotExists(GameObject posTarget, GameObject dataTarget, Title prefab)
        {
            if (!posTarget)
            {
                Debug.LogError("GetAndCreateIfNotExists: target is null.");
                return null;
            }

            if (!prefab)
            {
                Debug.LogError("GetAndCreateIfNotExists: prefab is null.");
                return null;
            }
            // if it breaks, change this back to posTarget
            int id = dataTarget.GetInstanceID();

            // Return cached if it exists and is still alive
            if (_cache.TryGetValue(id, out var existing) && existing)
                return existing;

            // Clean up stale entry
            _cache.Remove(id);

            // Ensure we are subscribed to target destruction
            EnsureRegistered(dataTarget, id);

            // Instantiate menu under this cache GameObject for organization
            var instance = Instantiate(prefab.gameObject, this.transform);
            if (instance.TryGetComponent(out PosTargetProvider t))
            {
                t.SetTarget(menuSpawnTarget);
            }
            else
            {
                instance.AddComponent<PosTargetProvider>().SetTarget(posTarget.transform);
            }
            if (instance.TryGetComponent<DataTargetProvider>(out var dt))
            {
                dt.SetTarget(dataTarget.transform);
            }
            else
            {
                instance.AddComponent<DataTargetProvider>().SetTarget(dataTarget.transform);
            }
            instance.name = $"{prefab.name} -> {dataTarget.name}::{id}";
            instance.GetComponent<Title>()?.SetTitle(posTarget.name);

            _cache[id] = instance.gameObject;
            return instance.gameObject;
        }

        /// <summary>
        /// Ensures the target has a DestroyNotifier and that we are subscribed
        /// exactly once to its destruction.
        /// </summary>
        private void EnsureRegistered(GameObject target, int id)
        {
            if (_registered.Contains(id))
                return;

            var notifier = target.GetComponent<DestroyNotifier>();
            if (!notifier)
                notifier = target.AddComponent<DestroyNotifier>();

            // Closure captures the id we used as the key
            notifier.Destroyed += () =>
            {
                if (_cache.TryGetValue(id, out var menu) && menu)
                    Destroy(menu);

                _cache.Remove(id);
                _registered.Remove(id);
            };

            _registered.Add(id);
        }
        public bool Exists(GameObject target)
        {
            if (!target)
                return false;

            int id = target.GetInstanceID();
            return _cache.ContainsKey(id) && _cache[id];
        }

        private void OnDestroy()
        {
            // Hygiene: clear static reference
            if (Instance == this)
                Instance = null;

            ClearAll();
        }
        [ContextMenu("Clear All Cached Menus")]
        private void ClearAll()
        {

            foreach (var child in transform)
            {
                DestroyImmediate(((Transform)child).gameObject);
            }
            _cache.Clear();
            _registered.Clear();
        }
    }
}
