using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Entities;
using UnityEngine.XR.Interaction.Toolkit;
using Unity.XR.CoreUtils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLN
{
    public class InfoDisplayPointer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform rayOrigin;
        [SerializeField] private Vector3 menuPlacementOffset = new Vector3(0, 0, 0.2f);
        [SerializeField] private InputActionReference buttonAction;

        [Header("Raycast")]
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float maxDistance = 30f;
        [SerializeField] private Transform placementTransform;

        [Header("Info Display")]
        // prefab with ScriptMachine + graph
        [SerializeField] private string toggleEventName = "Toggle"; // custom event in VS graph

        private const string DEBUG_CONTEXT_MENU = "Debug Context Menu";

        private void OnEnable()
        {
            if (buttonAction?.action == null) return;
            buttonAction.action.Enable();
            buttonAction.action.performed += OnButtonPerformed;
        }

        private void OnDisable()
        {
            if (buttonAction?.action == null) return;
            buttonAction.action.performed -= OnButtonPerformed;
        }

        private void OnButtonPerformed(InputAction.CallbackContext _)
        {
            if (rayOrigin == null) return;

            if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, maxDistance, hitMask))
                return;

            var target = hit.collider.attachedRigidbody
                ? hit.collider.attachedRigidbody.gameObject
                : hit.collider.gameObject;

            ToggleInfoDisplay(target, hit);
        }

        private void ToggleInfoDisplay(GameObject target, RaycastHit hit)
        {
            Debug.Log("Toggle Info Display on " + target.name);
            bool alreadyExisted = TreeNodeManager.Instance.Exists(target);
            var menu = TreeNodeManager
                .Instance
                .GetAndCreateIfNotExists(target);
            var headTx = XRRuntime.Origin.Camera.transform;

            if (menu.TryGetComponent<PosTargetProvider>(out var posTargetProvider))
            {
                if (placementTransform == null)
                {
                    Debug.LogError("Menu placement transform should not be null in InfoDisplayPointer. Using head transform as fallback.");
                }

                posTargetProvider.SetTarget(placementTransform);
            }
            if (alreadyExisted)
            {
                Debug.Log("toggle existing Info Display on " + target.name + " to " + !menu.gameObject.activeSelf);
                menu.gameObject.SetActive(!menu.gameObject.activeSelf);
                return;
            }
            else
            {
                Debug.Log("Info display IS NEW, setting active");
                menu.gameObject.SetActive(true);
            }

        }
        [ContextMenu("SimulateClickOnSelected")]
        private void SimulateClickOnSelected()
        {
#if UNITY_EDITOR
            var go = Selection.activeGameObject;
            if (go == null)
            {
                Debug.LogWarning("No GameObject selected.");
                return;
            }

            ToggleInfoDisplay(go, new RaycastHit());
#endif
        }

    }
}
