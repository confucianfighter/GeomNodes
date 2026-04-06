using UnityEngine;
using System;

namespace DLN
{

    public abstract class LayoutOp : MonoBehaviour
    {
        bool isOneShot = true;
        bool debugLog = false;
        public abstract void Execute();

        protected virtual void Reset()
        {
            TryAutoRegisterToNearestList();
        }

        protected void TryAutoRegisterToNearestList()
        {
            // Avoid self-registering if this op IS a list.
            if (this is C3DLS_LayoutOpsList)
                return;

            // Same object first
            var list = GetComponent<C3DLS_LayoutOpsList>();

            // Then search upward
            if (list == null)
                list = GetComponentInParent<C3DLS_LayoutOpsList>();

            if (list != null)
                list.TryAddOp(this);
        }
        public void Log(Func<String> message)
        {
            if (debugLog)
            {
                Debug.Log($"Component called {this.name}, of GameObject named {this.gameObject.name}: {message}");
            }
        }
    }
}