using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DLN
{
    public class ToggleInstantiateDisable : MonoBehaviour
    {
        public PosTargetProvider prefab;
        private PosTargetProvider instance;

        public UnityEvent toggle;
        public bool instantiateOnStart = false;
        public PosTargetProvider targetHolder;


        void Start()
        {
            // find unity xr toggle component
            if (TryGetComponent<Toggle>(out Toggle toggleComp))
            {
                toggleComp.onValueChanged.AddListener((value) => Toggle());
                if (instance == null || instance.gameObject.activeSelf == false)
                {
                    toggleComp.isOn = false;
                }
                else
                {
                    toggleComp.isOn = true;
                }
            }
        }

        public void Toggle()
        {
            if (instance == null)
            {
                var targ = targetHolder.GetTarget();
                instance = Instantiate<PosTargetProvider>(prefab, targ.position, targ.rotation);
                if (instance.TryGetComponent<PosTargetProvider>(out PosTargetProvider t))
                {
                    // passing the target along to whatever we are instantiating
                    // e.g. gameObject => ContextMenu => CornerMarkers
                    Debug.Log("Passing target called " + targ.name + " to instantiated prefab called " + instance.name);
                    t.SetTarget(targ);
                }
                else
                {
                    Debug.LogWarning("Prefab does not have a Target component.");
                }
            }
            else
            {
                instance.gameObject.SetActive(!instance.gameObject.activeSelf);
            }
            // prefab should have a target component?

        }
    }
}
