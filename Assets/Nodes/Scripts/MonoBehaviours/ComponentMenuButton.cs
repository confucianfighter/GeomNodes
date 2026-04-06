using UnityEngine;
using TMPro;
using System;
using Unity.XR.CoreUtils;
using System.Reflection;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace DLN
{
    public class ComponentMenuButton : MonoBehaviour
    {
        public Component component;
        [SerializeField] GameObject submenuPrefab;
        [SerializeField] GameObject submenuInstance;
        [SerializeField] bool debug = true;
        [SerializeField] Transform connectorTransform;
        [SerializeField]
        List<BindingFlags> propertiesBindingFlags = new List<BindingFlags> { BindingFlags.Instance, BindingFlags.Public };

        [SerializeField]
        List<BindingFlags> fieldsBindingFlags = new List<BindingFlags> { BindingFlags.Instance, BindingFlags.Public };

        private void Log(Func<string> messageFunc)
        {
            if (debug) Debug.Log(messageFunc());
        }
        public void Initialize()
        {
            SetLabel(component != null ? component.GetType().Name : "No Component");
            if (TryGetComponent<UnityEngine.UI.Button>(out var button))
            {
                button.onClick.AddListener(Click);
            }
            else if (this.TryGetComponentInChildren<UnityEngine.UI.Button>(out var childButton))
            {
                childButton.onClick.AddListener(Click);
            }

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
        private void OnDestroy()
        {
            if (submenuInstance != null)
            {
                Destroy(submenuInstance);
            }
        }
        private void OnEnable()
        {
            if (submenuInstance != null)
            {
                submenuInstance.SetActive(true);
            }
        }
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
            Debug.Log($"Clicked on component button: {component.GetType().Name}");
            if (submenuInstance == null)
            {
                var origin = XRRuntime.Origin;
                if (origin == null)
                {
                    Debug.LogError("XROrigin not found in the scene by XRRuntime.Origin.");
                    return;
                }
                this.submenuInstance = Instantiate(submenuPrefab, XRRuntime.Origin.transform);
                if (submenuInstance.TryGetComponent<Title>(out var title))
                {
                    title.SetTitle(component.GetType().Name);
                }
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
                if (submenuInstance.TryGetComponent<DataTargetProvider>(out var dataTargetProvider))
                {
                    dataTargetProvider.SetTarget(component.transform);
                }
                if (submenuInstance.TryGetComponent<ComponentMenu>(out var componentMenu))
                {
                    componentMenu.SetTargetComponent(component);
                }

            }
            else
            {
                submenuInstance.SetActive(!submenuInstance.activeSelf);
            }
        }
        [ContextMenu("Print Properties")]
        public void PrintProperties()
        {
            Log(() => $"Component Button: Printing properties of component: {component.GetType().Name}");
            try
            {
                Debug.Log(ComponentUtils.PrintProperties(component, propertiesBindingFlags));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        [ContextMenu("Print Fields")]
        public void PrintFields()
        {
            Log(() => $"Component Button: Printing fields of component: {component.GetType().Name}");
            try
            {
                Debug.Log(ComponentUtils.PrintFields(component, fieldsBindingFlags));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}