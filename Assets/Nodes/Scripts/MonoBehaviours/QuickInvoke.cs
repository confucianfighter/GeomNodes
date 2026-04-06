using UnityEngine;
using UnityEngine.Events;
using System;

namespace DLN
{
    public class QuickInvoke : MonoBehaviour
    {
        [Header("Drag functions into this event for quick testing")]
        public UnityEvent targetEvent;
        public UnityEvent<float> dynamicFloatEvent;

        [SerializeField] bool debug = true;

        [ContextMenu("Invoke Now")]
        public void InvokeNow()
        {
            if (debug)
            {
                Debug.Log($"QuickInvoke: about to invoke on '{name}' (activeInHierarchy={gameObject.activeInHierarchy})");

                if (targetEvent == null)
                {
                    Debug.LogWarning("QuickInvoke: targetEvent is NULL.");
                    return;
                }

                int persistentCount = targetEvent.GetPersistentEventCount();
                Debug.Log($"QuickInvoke: persistent listener count = {persistentCount}");

                for (int i = 0; i < persistentCount; i++)
                {
                    var targ = targetEvent.GetPersistentTarget(i);
                    var method = targetEvent.GetPersistentMethodName(i);
                    Debug.Log($"  [{i}] target={(targ ? targ.name : "NULL")} method='{method}'");
                }

                if (persistentCount == 0)
                    Debug.LogWarning("QuickInvoke: No persistent listeners. If you relied on AddListener at runtime, it won't exist when invoking from the inspector in edit mode.");
            }

            try
            {
                targetEvent?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        [ContextMenu("Invoke Dynamic Float with 3.14f")]
        public void InvokeDynamicFloat()
        {
            try
            {
                dynamicFloatEvent?.Invoke(3.14f);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        public void PrintFloat(float value)
        {
            Debug.Log($"QuickInvoke: PrintFloat received value: {value}");
        }
    }
}
