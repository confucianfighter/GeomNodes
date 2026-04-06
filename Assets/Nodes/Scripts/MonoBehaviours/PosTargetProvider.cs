using UnityEngine;

namespace DLN
{
    public class PosTargetProvider : MonoBehaviour
    {
        public Transform targetObject;
        public void SetTarget(Transform tx)
        {
            Debug.Log("Setting target to " + tx.name);
            targetObject = tx;
        }
        public Transform GetTarget()
        {
            return targetObject;
        }
        public bool TryGetTarget(out Transform target)
        {
            target = targetObject;
            return target != null;
        }
    }
}