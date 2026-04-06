using UnityEngine;
using TMPro;
using System;
using Unity.XR.CoreUtils;
using System.Reflection;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;

namespace DLN
{
    public class ShowParentMenuButton : MonoBehaviour
    {
        public GameObject targetGameObject;
        public DataTargetProvider dataTargetProvider;
        [SerializeField] GameObject submenuPrefab;
        [SerializeField] GameObject submenuInstance;
        [SerializeField] bool debug = true;
        [SerializeField] Transform connectorTransform;
        [SerializeField] IPortBase port;

        private void Log(Func<string> messageFunc)
        {
            if (debug) Debug.Log(messageFunc());
        }
        public IEnumerator Initialize()
        {
            yield return new WaitUntil(() => dataTargetProvider.GetTarget() != null);
            targetGameObject = dataTargetProvider.GetTarget().gameObject.transform.parent.gameObject;
            if (targetGameObject == null) gameObject.SetActive(false);
            SetLabel(targetGameObject != null ? targetGameObject.name : "No GameObject");
            if (TryGetComponent<UnityEngine.UI.Button>(out var button))
            {
                button.onClick.AddListener(Click);
            }
            else if (this.TryGetComponentInChildren<UnityEngine.UI.Button>(out var childButton))
            {
                childButton.onClick.AddListener(Click);
            }
            yield return null;

        }
        [ContextMenu("Start")]
        public void Start()
        {
            StartCoroutine(Initialize());
        }
        public void SetLabel(string label)
        {
            if (TryGetComponent<Title>(out var title))
            {
                title.SetTitle(label);
            }
            else
            {
                GetComponentInChildren<TextMeshProUGUI>()!.text = label;
            }

        }
        private void OnDestroy()
        {
            if (submenuInstance != null)
            {
                Destroy(submenuInstance);
            }
        }
        [ContextMenu("OnEnable")]
        private void OnEnable()
        {
            if (submenuInstance != null)
            {
                submenuInstance.SetActive(true);
            }
        }
        // on setactive(false) set the submenuInstance to false as well.
        [ContextMenu("Click")]
        public void Click()
        {
            Debug.Log($"Clicked on gameObject button: {targetGameObject.GetType().Name}");
            if (submenuInstance == null)
            {
                var origin = XRRuntime.Origin;
                if (origin == null)
                {
                    Debug.LogError("XROrigin not found in the scene by XRRuntime.Origin.");
                    return;
                }
                this.submenuInstance = TreeNodeManager.Instance.GetAndCreateIfNotExists(targetGameObject);
                if (submenuInstance.TryGetComponent<Title>(out var title))
                {
                    title.SetTitle(targetGameObject.name);
                }
                // this is a physical target, a physical target is a gameobject
                // I suppose a data target can also be a gameobject.
                // So do I rename TargetProvider to PositioningTargetProvider and create a DataTargetProvider?
                if (submenuInstance.TryGetComponent<PosTargetProvider>(out var targetProvider))
                {
                    if (connectorTransform != null)
                    {
                        targetProvider.SetTarget(connectorTransform);
                    }
                    else
                    {
                        targetProvider.SetTarget(this.transform);
                    }
                }
                if (submenuInstance.TryGetComponent<DataTargetProvider>(out var dtProvider))
                {
                    dtProvider.SetTarget(targetGameObject.transform);
                }

            }
            else
            {
                submenuInstance.SetActive(true);
            }

        }

    }
}