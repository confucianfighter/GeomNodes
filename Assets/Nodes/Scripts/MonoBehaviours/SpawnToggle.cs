using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DLN
{
    public enum ToggleType
    {
        InstantiateDestroy,
        InstantiateDisable
    }
    public class SpawnToggle : MonoBehaviour
    {
        public PosTargetProvider prefab;
        private PosTargetProvider instance;

        public UnityEvent toggle;
        public bool instantiateOnStart = false;
        public PosTargetProvider targetHolder;
        public ToggleType toggleType = ToggleType.InstantiateDestroy;


        public void OnEnable()
        {
            if (TryGetComponent<Toggle>(out Toggle toggleComp))
            {
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
                Debug.Log("Instantiating instance called " + instance.name + " from ToggleInstantiateDestroy.");
                Debug.Log("instance.selfActive = " + instance.gameObject.activeSelf);
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
                if (toggleType == ToggleType.InstantiateDisable)
                {
                    instance.gameObject.SetActive(!instance.gameObject.activeSelf);
                }
                else if (toggleType == ToggleType.InstantiateDestroy)
                {
                    Debug.Log("Destroying instance called " + instance.name + " from ToggleInstantiateDestroy.");
                    Destroy(instance.gameObject);
                    instance = null;
                }
            }
            // prefab should have a target component?

        }
    }
}
