using UnityEngine;
using DLN;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace DLN
{
    [RequireComponent(typeof(LazyFollow))]
    public class FollowMenuDock : MonoBehaviour
    {
        [SerializeField] private LazyFollow lazyFollow;
        [SerializeField] private MenuDock menuDock;

        private void Awake()
        {
            lazyFollow = GetComponent<LazyFollow>();
            // get world root components, find teh first one with a MenuDock component
            if (menuDock == null)
            {
                var roots = gameObject.scene.GetRootGameObjects();

                foreach (var root in roots)
                {
                    if (root.TryGetComponent<MenuDock>(out var dock))
                    {
                        menuDock = dock;
                    }
                }

            }
        }

        private void Start()
        {
            if (menuDock != null && menuDock.dockPoints.Count > 0)
            {
                // For simplicity, follow the first dock point
                lazyFollow.target = menuDock.GetDockPoint();
            }
        }
    }
}