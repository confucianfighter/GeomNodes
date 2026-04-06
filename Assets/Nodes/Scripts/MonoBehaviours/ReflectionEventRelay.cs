using UnityEngine;
using UnityEngine.Events;
using System.Reflection;


namespace DLN
{
    public class ReflectionEventRelay : MonoBehaviour
    {
        public UnityEvent<float> dynamicFloatEvent;
        public Component targetComponent;
        [ContextMenu("Attach To First Float Event Found in targetComponent")]
        public void AttachToFirstFloatEvent()
        {
            if (targetComponent == null)
            {
                Debug.LogError("ReflectionEventRelay: targetComponent is NULL.");
                return;
            }

            var floatEvents = ComponentUtils.GetUnityEventsOfType<float>(targetComponent);
            if (floatEvents.Count == 0)
            {
                Debug.LogError($"ReflectionEventRelay: No UnityEvent<float> found on component of type {targetComponent.GetType().Name}.");
                return;
            }

            var firstFloatEvent = floatEvents[0];
            firstFloatEvent.AddListener(OnFloatEventInvoked);
            Debug.Log($"ReflectionEventRelay: Attached to UnityEvent<float> '{firstFloatEvent.GetType().Name}' on component '{targetComponent.GetType().Name}'.");
        }
        public void OnFloatEventInvoked(float value)
        {
            Debug.Log($"ReflectionEventRelay: Received float event with value: {value}");

        }
        [ContextMenu("Add First Float Function To dynamicFloatEvent")]
        public void AddFirstFloatFunctionToDynamicFloatEvent()
        {
            
            if (targetComponent == null)
            {
                Debug.LogError("ReflectionEventRelay: targetComponent is NULL.");
                return;
            }

            if (dynamicFloatEvent == null)
                dynamicFloatEvent = new UnityEvent<float>();

            // Find a public instance method: void Method(float)
            MethodInfo method = ComponentUtils.GetFirstPublicMethodWithSingleArg<float>(targetComponent);

            if (method == null)
            {
                Debug.LogError($"ReflectionEventRelay: No public instance method 'void X(float)' found on {targetComponent.GetType().Name}.");
                return;
            }

            UnityAction<float> action;
            try
            {
                action = ComponentUtils.CreateUnityAction<float>(targetComponent, method);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"ReflectionEventRelay: Failed to bind {targetComponent.GetType().Name}.{method.Name}(float). {ex}");
                return;
            }

            dynamicFloatEvent.AddListener(action);

            Debug.Log($"ReflectionEventRelay: Added listener -> {targetComponent.GetType().Name}.{method.Name}(float) to dynamicFloatEvent.");
        }
        [ContextMenu("Invoke Dynamic Float Event with 7.77f")]
        public void InvokeDynamicFloatEvent()
        {
            if (dynamicFloatEvent == null)
            {
                Debug.LogWarning("ReflectionEventRelay: dynamicFloatEvent is NULL.");
                return;
            }

            Debug.Log($"ReflectionEventRelay: Invoking dynamicFloatEvent with value: 7.77f");
            dynamicFloatEvent.Invoke(7.77f);
        }


    }

}