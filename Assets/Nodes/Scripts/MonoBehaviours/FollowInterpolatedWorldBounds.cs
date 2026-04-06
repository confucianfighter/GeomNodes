using UnityEngine;
using UnityEngine.UIElements;
namespace DLN
{
    public class FollowInterpolatedWorldBounds : MonoBehaviour
    {
        [SerializeField] private PosTargetProvider targetProvider;
        [SerializeField] private Transform target;

        [SerializeField] private Transform followTransform;
        [SerializeField] private bool debug = true;
        [SerializeField] bool updateEveryFrame = true;
        [SerializeField] Vector3 offset = Vector3.zero;
        [SerializeField] Vector3 interpolatedPositionOnTarget = new Vector3(0.5f, 1.0f, 0.5f);
        [SerializeField] Vector3 forward = Vector3.forward;
        [SerializeField] Vector3 up = Vector3.up;
        // center top

        [SerializeField] private bool ParentToTarget = false;


        private void Awake()
        {
            if (targetProvider == null)
            {
                targetProvider = GetComponentInParent<PosTargetProvider>();
            }
            if (targetProvider == null)
            {
                Debug.LogError($"{nameof(FollowInterpolatedWorldBounds)}: No TargetProvider found in parents.");
            }


        }
        private void Start()
        {
            if (target == null && targetProvider != null)
            {
                target = targetProvider.GetTarget();
            }
            PositionConnectorEnd();
        }
        private void OnEnable()
        {
            PositionConnectorEnd();
        }
        private void Update()
        {
            if (updateEveryFrame)
            {
                PositionConnectorEnd();
            }
        }
        private void PositionConnectorEnd()
        {
            if (target == null) return;
            Bnds.InterpolateWorldBounds(
                target,
                interpolatedPositionOnTarget.x,
                interpolatedPositionOnTarget.y,
                interpolatedPositionOnTarget.z,

                out Vector3 worldPos
            );
            followTransform.position = worldPos;
            followTransform.position += offset;
            if (ParentToTarget)
            {
                followTransform.SetParent(target, true);
            }
            // set rotation using world forward and up vectors
            followTransform.rotation = Quaternion.LookRotation(forward, up);

        }

    }
}