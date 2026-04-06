using UnityEngine;
using DLN;
using System.Collections;
using UnityEngine.Events;

namespace DLN
{
    public class TransformWatcher : MonoBehaviour
    {
        public DataTargetProvider targetProvider;
        [SerializeField] public UnityEvent<float> OnWorldXPosChanged;
        [SerializeField] public UnityEvent<float> OnWorldYPosChanged;
        [SerializeField] public UnityEvent<float> OnWorldZPosChanged;

        [SerializeField] private float lastYValue = float.NaN;
        [SerializeField] private float lastZValue = float.NaN;
        [SerializeField] private float lastXValue = float.NaN;



        protected void Start()
        {
            if (targetProvider == null)
            {
                targetProvider = GetComponentInParent<DataTargetProvider>();
                if (targetProvider == null)
                {
                    Debug.LogError("XWorldPositionField: No TargetProvider found in parents.");
                }
                StartCoroutine(InitializeValues());
            }
        }
        public IEnumerator InitializeValues()
        {
            yield return Coroutines.WaitUntilWithTimeout(
                condition: () => targetProvider != null && targetProvider.GetTarget() != null,
                onSuccess: () =>
                {
                    var pos = targetProvider.GetTarget().transform.position;
                    lastXValue = pos.x;
                    lastYValue = pos.y;
                    lastZValue = pos.z;
                    OnWorldXPosChanged?.Invoke(lastXValue);
                    OnWorldYPosChanged?.Invoke(lastYValue);
                    OnWorldZPosChanged?.Invoke(lastZValue);
                },
                onTimeout: () =>
                {
                    Debug.LogError("TransformWatcher: Timeout waiting for TargetProvider target to be assigned.");
                },
                timeoutSeconds: 5f
            );

        }
        public void SetWorldXPos(float x)
        {

            lastXValue = x;
            Vector3 pos = targetProvider.GetTarget().transform.position;
            pos.x = x;
            targetProvider.GetTarget().transform.position = pos;
        }
        public void SetWorldYPos(float y)
        {
            lastYValue = y;
            Vector3 pos = targetProvider.GetTarget().transform.position;
            pos.y = y;
            targetProvider.GetTarget().transform.position = pos;
        }
        public void SetWorldZPos(float z)
        {
            lastZValue = z;
            Vector3 pos = targetProvider.GetTarget().transform.position;
            pos.z = z;
            targetProvider.GetTarget().transform.position = pos;
        }

        protected void Update()
        {
            var pos = targetProvider.GetTarget().transform.position;
            if (pos.x != lastXValue)
            {
                lastXValue = targetProvider.GetTarget().transform.position.x;
                OnWorldXPosChanged?.Invoke(lastXValue);
            }
            if (pos.y != lastYValue)
            {
                lastYValue = targetProvider.GetTarget().transform.position.y;
                OnWorldYPosChanged?.Invoke(lastYValue);
            }
            if (pos.z != lastZValue)
            {
                lastZValue = targetProvider.GetTarget().transform.position.z;
                OnWorldZPosChanged?.Invoke(lastZValue);
            }


        }

    }
}