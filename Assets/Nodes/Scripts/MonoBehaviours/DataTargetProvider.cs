using UnityEngine;

namespace DLN
{
    public class DataTargetProvider : MonoBehaviour
    {
        [SerializeField] private Transform target;

        public Transform GetTarget()
        {
            return target;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}