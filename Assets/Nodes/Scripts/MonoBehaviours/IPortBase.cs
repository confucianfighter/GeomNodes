using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace DLN
{
    public enum PortType
    {
        Input,
        Output
    }
    public abstract class IPortBase : MonoBehaviour
    {
        public PortType portType;

        public UnityEvent onEnable;
        public UnityEvent onDisable;

        public int selfBinding = -1;
        public int otherBinding = -1;


        private void OnEnable()
        {
            onEnable?.Invoke();
        }
        private void OnDisable()
        {
            onDisable?.Invoke();
        }
        // on awake is when we create the edge, but I forget how we get the reference.
        // is it the button that does this?

        void Awake()
        {
            Initialize();
        }
        [ContextMenu("Initialize")]
        public virtual void Initialize()
        {
            StartCoroutine(InitializeRoutine());

        }
        private IEnumerator InitializeRoutine()
        {
            yield return new WaitUntil(() =>
            selfBinding != -1 &&
            otherBinding != -1
            );
            int startBinding, endBinding;
            if (portType == PortType.Output)
            {
                startBinding = selfBinding;
                endBinding = otherBinding;
                EdgeManager.CreateIfNotExists((startBinding, endBinding), out var edge, this);
                if (edge == null)
                {
                    Debug.LogError($"Port: Could not create or get edge for bindings ({startBinding}, {endBinding})");
                }
                edge.SetStartPoint(this);
                edge.UpdateVisibility();
            }
            else
            {
                startBinding = otherBinding;
                endBinding = selfBinding;
                EdgeManager.CreateIfNotExists((startBinding, endBinding), out var edge, this);
                if (edge == null)
                {
                    Debug.LogError($"Port: Could not create or get edge for bindings ({startBinding}, {endBinding})");
                }
                edge.SetEndPoint(this);
                edge.UpdateVisibility();
            }

        }
    }
}