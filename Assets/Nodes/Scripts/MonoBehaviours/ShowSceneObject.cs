using UnityEngine;
using UnityEngine.ProBuilder;

namespace DLN
{
    public class ShowSceneObject : MonoBehaviour
    {
        [Header("Tries to obtain target from parent search of DataTargetProvider")]
        public Transform target;
        [SerializeField] private Material sphereMat;
        public EncapsulateWithSphere spherePrefab;
        public EncapsulateWithSphere sphere;
        public Transform startTransform;
        public Transform endTransform;
        public GameObject line;




        // this has a port
        // if we want a port on the sphere, it may have to be a prefab.
        // else we could skip the edge manager.
        // the other option is to install the port component directly where the sphere component is
        // easiest may be to ... let's just get the sphere to show up.

        public void Show()
        {
            GetTarget();
            if (!sphere)
            {
                sphere = Instantiate(spherePrefab);
                sphere.transform.parent = target.transform;
                sphere.target = target.gameObject;
                ShowLine();
            }
            else
            {
                Toggle();
                if (sphere.IsVisible())
                {
                    ShowLine();
                }
                else
                {
                    HideLine();
                }
            }

        }

        public void Toggle()
        {
            sphere.Toggle();
        }
        public void GetTarget()
        {
            if (target == null)
            {
                var provider = GetComponentInParent<DataTargetProvider>();
                if (provider != null && provider.GetTarget() != null)
                {
                    target = provider.GetTarget();

                }
                else
                {
                    Debug.LogError("ShowSceneObject: No target assigned and no DataTargetProvider found in parents.");
                }
            }
        }
        public void ShowLine()
        {
            startTransform.position = transform.position;
            var targetProvider = endTransform.GetComponent<PosTargetProvider>();

            if (targetProvider != null)
            {
                targetProvider.SetTarget(sphere.GetLineEndPointTransform());
            }
            endTransform.SetParent(sphere.GetLineEndPointTransform());
            endTransform.localPosition = Vector3.zero;
            endTransform.right = Vector3.down;
            line.SetActive(true);

        }
        public void HideLine()
        {
            line.SetActive(false);
        }
    }
}