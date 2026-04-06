using UnityEngine;
using DLN;
using System;
using System.Collections;
using System.Collections.Generic;
namespace DLN
{
    public class FollowWithPositioners : MonoBehaviour
    {
        [SerializeField] public float delay = 0.0f;
        public Transform target;
        public PosTargetProvider targetProvider;
        [SerializeField] List<IPositionerBase> positioners = new List<IPositionerBase>();
        [SerializeField] bool constantUpdate = true;
        [SerializeField] bool RepositionOnEnable = false;
        [ContextMenu("Awake")]
        void Awake()
        {
            if (positioners.Count == 0)
            {
                if (TryGetComponent<IPositionerBase>(out var foundPositioners))
                {
                    positioners.Add(foundPositioners);
                }
            }
            if (target == null)
            {
                StartCoroutine(WaitForTargetAndSetInitialPosition());

            }


        }

        public IEnumerator OnEnableRoutine()
        {
            if (RepositionOnEnable)
            {
                if (delay > 0.0f)
                {
                    yield return new WaitForSeconds(delay);
                }
                yield return WaitForTargetAndSetInitialPosition();
            }

        }
        private IEnumerator WaitForTargetAndSetInitialPosition()
        {
            yield return new WaitUntil(() => targetProvider != null);
            yield return new WaitUntil(() => targetProvider.GetTarget() != null);
            target = targetProvider.GetTarget();
            Position();
        }
        private void OnEnable()
        {
            Debug.Log("InterpolateAnchorMove OnEnable called");
            StartCoroutine(OnEnableRoutine());
        }
        [ContextMenu("Position")]
        public void Position()
        {

            foreach (var positioner in positioners)
            {
                positioner.Position(this.transform, target);
            }
        }
        public void Update()
        {
            if (constantUpdate && target != null)
            {
                Position();
            }
        }
    }
}
