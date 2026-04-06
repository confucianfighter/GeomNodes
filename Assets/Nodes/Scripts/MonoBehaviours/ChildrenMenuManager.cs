using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;

namespace DLN
{

    public class ChildrenMenuManager : MonoBehaviour
    {
        public bool debug = true;
        [SerializeField] private Transform menuSectionTransform;
        [SerializeField] private ComponentMenuButton buttonPrefab;
        [SerializeField] private PosTargetProvider targetProvider;
        [SerializeField] private Transform target;
        [SerializeField] private Title title;
        [SerializeField] private List<ComponentMenuButton> buttons = new List<ComponentMenuButton>();

        public void Log(Func<string> messageFunc)
        {
            if (debug)
            {
                Debug.Log(messageFunc());
            }
        }
        public string[] componentTypes;
        private void Awake()
        {
            StartCoroutine(WaitForTarget());
        }
        public IEnumerator WaitForTarget()
        {
            if (target == null && targetProvider == null)
            {
                if (TryGetComponent<PosTargetProvider>(out var tp))
                {
                    targetProvider = tp;
                }
                else if (this.TryGetComponentInParent<PosTargetProvider>(out var parentTP))
                {
                    targetProvider = parentTP;
                }
                else
                {
                    Debug.LogError($"ComponentMenuManager: No TargetProvider found on {this.name} or its parents.");
                }
            }
            if (targetProvider.GetTarget() == null)
            {
                yield return new WaitUntil(() => targetProvider.GetTarget() != null);
            }
            target = targetProvider.GetTarget();

        }

        private void Start()
        {
            if (this.title == null && TryGetComponent<Title>(out var title))
            {
                this.title = title; // just for view in the inspector.
                title.SetTitle("Components");
            }
            StartCoroutine(UpdateMenu());
        }

        public void ClearMenu()
        {
            foreach (var button in buttons)
            {
                Destroy(button.gameObject);
            }
            buttons.Clear();
        }
        public IEnumerator UpdateMenu()
        {
            ClearMenu();
            yield return new WaitUntil(() => target != null);

            foreach (var component in target.GetComponents<Component>())
            {
                var button = Instantiate(buttonPrefab, menuSectionTransform);
                buttons.Add(button);
                button.component = component;
            }
        }
    }
}