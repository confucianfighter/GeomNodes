using UnityEngine;
using DLN.Preview;
using System.Collections;
using System.ComponentModel;
namespace DLN
{
    public class PreviewBox3D : MonoBehaviour
    {
        [SerializeField] Transform previewRoot;
        [SerializeField] int previewLayer = 0;
        [SerializeField] bool cloneOnStart = false;
        [Header("Just for testing.")]
        [SerializeField] GameObject source;
        GameObject currentPreview;
        public GameObject box;

        public IEnumerator ShowPreviewRoutine()
        {
            yield return Coroutines.WaitUntilWithTimeout(condition: () => GetComponentInParent<DataTargetProvider>().GetTarget() != null,
            onSuccess: () => source = GetComponentInParent<DataTargetProvider>().GetTarget().gameObject,
            onTimeout: () => Debug.LogError("Preview box could not obtain source from targetProvider"),
            timeoutSeconds: 2f
            );
            if (currentPreview != null) Destroy(currentPreview);
            if (source.TryGetComponent<SceneRootTag>(out _))
            {
                yield break;
            }
            var opts = PreviewCloneUtility.DefaultReplicaOptions;
            opts.parent = previewRoot;
            opts.keepWorldPosition = false;
            opts.setLayer = true;
            opts.layer = previewLayer;

            currentPreview = PreviewCloneUtility.CreateRenderReplica(source, opts);
            if (!previewRoot)
            {
                previewRoot = transform.parent;
            }
            if (cloneOnStart)
            {
                if (source)
                {
                    ShowPreview();
                    if (box)
                    {
                        Bnds.FitToBox(
                            boxObject: box,
                            targetObject: currentPreview,
                            setParent: true,
                            resetLocalRotation: true,
                            resetLocalScaleToOne: false,
                            padding: 0f,
                            postScaleMultiplier: 1f,
                            log: false
                        );
                    }
                    else
                    {
                        Debug.LogError("Box should not be null in 3D preview box");
                    }
                }
            }
        }
        public void ShowPreview()
        {
            StartCoroutine(ShowPreviewRoutine());
        }
        void Start()
        {
            ShowPreview();

        }
    }
}