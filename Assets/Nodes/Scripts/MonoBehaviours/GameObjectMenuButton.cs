using UnityEngine;
using TMPro;
using System;
using System.Collections;
using Unity.XR.CoreUtils;
using System.Reflection;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace DLN
{
    public enum TreeButtonType
    {
        ChildToParent,
        ParentToChild
    }
    public class GameObjectMenuButton : MonoBehaviour
    {
        public GameObject targetChildGameObject;
        public GameObject targetParentGameObject;
        [SerializeField] GameObject submenuPrefab;
        [SerializeField] GameObject submenuInstance;
        [SerializeField] bool debug = true;
        [SerializeField] Port port;
        public TreeButtonType buttonType = TreeButtonType.ParentToChild;

        private void Log(Func<string> messageFunc)
        {
            if (debug) Debug.Log(messageFunc());
        }
        [ContextMenu("Initialize")]
        public void Initialize()
        {
            StartCoroutine(InitializeCoroutine());
        }
        IEnumerator InitializeCoroutine()
        {
            yield return Coroutines.WaitUntilWithTimeout(
                condition: () => GetComponentInParent<DataTargetProvider>() != null,
                onSuccess: () => Log(() => "DataTargetProvider found in parents."),
                onTimeout: () => Debug.LogError("GameObjectMenuButton: InitializeCoroutine timed out waiting for DataTargetProvider in parents."),
                timeoutSeconds: 5f
            );
            var dataTarget = GetComponentInParent<DataTargetProvider>().GetTarget();
            if (dataTarget == null)
            {
                Debug.LogError($"Need to set dataTargetAtTreeNodeHead.");
            }
            switch (buttonType)
            {
                case TreeButtonType.ParentToChild:
                    this.targetParentGameObject = dataTarget.gameObject;
                    SetLabel(targetChildGameObject != null ? targetChildGameObject.name : "No GameObject");
                    if (TryGetComponent<UnityEngine.UI.Button>(out var button))
                    {
                        button.onClick.AddListener(Click);
                    }
                    else if (this.TryGetComponentInChildren<UnityEngine.UI.Button>(out var childButton))
                    {
                        childButton.onClick.AddListener(Click);
                    }
                    if (targetParentGameObject != null)
                    {
                        port.selfBinding = targetParentGameObject.GetInstanceID();
                        port.otherBinding = targetChildGameObject.GetInstanceID();
                    }
                    break;
                case TreeButtonType.ChildToParent:
                    this.targetChildGameObject = dataTarget.gameObject;
                    // if this doesn't have a parent, then maybe we could provide a dummy world gameobject?
                    if (dataTarget.parent == null)
                    {
                        UnityEngine.UI.Button btn;
                        if (!TryGetComponent<UnityEngine.UI.Button>(out btn))
                        {
                            this.TryGetComponentInChildren<UnityEngine.UI.Button>(out btn);
                        }
                        if (btn != null)
                        {
                            btn.interactable = false;

                        }
                        break;
                    }
                    this.targetParentGameObject = dataTarget.transform.parent.gameObject;
                    if (TryGetComponent<UnityEngine.UI.Button>(out var button2))
                    {
                        button2.onClick.AddListener(Click);
                    }
                    else if (this.TryGetComponentInChildren<UnityEngine.UI.Button>(out var childButton2))
                    {
                        childButton2.onClick.AddListener(Click);
                    }
                    if (targetChildGameObject != null)
                    {
                        port.selfBinding = targetChildGameObject.GetInstanceID();
                        port.otherBinding = targetParentGameObject.GetInstanceID();
                    }
                    break;
            }
            yield return null;

        }
        public void Start()
        {
            Initialize();
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
        private void Destroy()
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
        [ContextMenu("OnDisable")]
        // on setactive(false) set the submenuInstance to false as well.
        private void OnDisable()
        {
            if (submenuInstance != null)
            {
                submenuInstance.SetActive(false);
            }
        }
        [ContextMenu("Click")]
        public void Click()
        {
            Log(() => $"Clicked on gameObject button: {targetChildGameObject.GetType().Name}");
            if (buttonType == TreeButtonType.ParentToChild)
            {
                if (submenuInstance == null)
                {
                    var origin = XRRuntime.Origin;
                    if (origin == null)
                    {
                        Debug.LogError("XROrigin not found in the scene by XRRuntime.Origin.");
                        return;
                    }

                    this.submenuInstance = TreeNodeManager.Instance.GetAndCreateIfNotExists(targetChildGameObject);
                    if (submenuInstance.TryGetComponent<Title>(out var title))
                    {
                        title.SetTitle(targetChildGameObject.name);
                    }
                    // I don't think this function is necessary.
                    if (submenuInstance.TryGetComponent<PosTargetProvider>(out var targetProvider))
                    {
                        if (port != null)
                        {
                            targetProvider.SetTarget(port.transform);
                        }
                        else
                        {
                            targetProvider.SetTarget(this.transform);
                        }
                    }

                }
                else
                {
                    submenuInstance.SetActive(!submenuInstance.activeSelf);
                }
            }
            else if (buttonType == TreeButtonType.ChildToParent)
            {
                if (submenuInstance == null)
                {
                    var origin = XRRuntime.Origin;
                    if (origin == null)
                    {
                        Debug.LogError("XROrigin not found in the scene by XRRuntime.Origin.");
                        return;
                    }

                    this.submenuInstance = TreeNodeManager.Instance.GetAndCreateIfNotExists(targetParentGameObject);
                    if (submenuInstance.TryGetComponent<Title>(out var title))
                    {
                        title.SetTitle(targetParentGameObject.name);
                    }
                    if (submenuInstance.TryGetComponent<PosTargetProvider>(out var targetProvider))
                    {
                        if (port != null)
                        {
                            targetProvider.SetTarget(port.transform);
                        }
                        else
                        {
                            targetProvider.SetTarget(this.transform);
                        }
                    }

                }
                else
                {
                    submenuInstance.SetActive(true);
                }
            }

        }
    }
}