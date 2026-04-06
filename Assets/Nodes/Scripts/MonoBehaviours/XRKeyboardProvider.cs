using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

namespace DLN
{
    public class XRKeyboardProvider : MonoBehaviour
    {
        public static XRKeyboardProvider Instance { get; private set; }
        public XRKeyboard currentDisplay;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public static IEnumerator GetKeyboard(
            Action<XRKeyboard> onFound,
            Action onTimeout = null,
            float timeoutSeconds = 1f,
            bool includeInactive = true
        )
        {
            if (Instance.currentDisplay != null)
            {
                onFound?.Invoke(Instance.currentDisplay);
                yield break;
            }
            XRKeyboard found = null; // <- captured "wait variable"

            yield return Coroutines.WaitUntilWithTimeout(
                condition: () =>
                {
                    // condition runs every frame until true (or timeout)
                    var roots = SceneManager.GetActiveScene().GetRootGameObjects();

                    for (int i = 0; i < roots.Length; i++)
                    {
                        found = roots[i].GetComponentInChildren<XRKeyboard>(includeInactive);
                        if (found != null)
                            return true;
                    }

                    return false;
                },
                onSuccess: () =>
                {
                    if (Instance != null)
                        Instance.currentDisplay = found;

                    onFound?.Invoke(found);
                },
                onTimeout: onTimeout,
                timeoutSeconds: timeoutSeconds
            );
        }
    }
}
