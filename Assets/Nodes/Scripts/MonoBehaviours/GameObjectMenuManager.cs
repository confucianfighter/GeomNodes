using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;
using Unity.XR.CoreUtils;
namespace DLN
{

    public class GameObjectMenuManager : MonoBehaviour
    {
        public bool debug = true;
        public bool isSceneRoot = false;
        [SerializeField] private Transform menuSectionTransform;
        [SerializeField] private GameObjectMenuButton buttonPrefab;
        [SerializeField] private DataTargetProvider dataTargetProvider;
        [SerializeField] private Transform target;
        [SerializeField] private Title title;
        [SerializeField] private List<GameObjectMenuButton> buttons = new List<GameObjectMenuButton>();
        [Header("This is reserved for the scene root prefab, just need a dummy parent to pass in.")]
        [SerializeField] public GameObject sceneRootGO;
        public void Log(Func<string> messageFunc)
        {
            if (debug)
            {
                Debug.Log(messageFunc());
            }
        }
        [ContextMenu("Awake")]
        public void Awake()
        {
            if (!isSceneRoot) StartCoroutine(WaitForTarget());
        }
        public IEnumerator WaitForTarget()
        {
            if (target == null && dataTargetProvider == null)
            {
                if (TryGetComponent<DataTargetProvider>(out var tp))
                {
                    dataTargetProvider = tp;
                }
                else if (this.TryGetComponentInParent<DataTargetProvider>(out var parentTP))
                {
                    dataTargetProvider = parentTP;
                }
                else
                {
                    Debug.LogError($"GameObjectMenuManager: No TargetProvider found on {this.name} or its parents.");
                }
            }
            if (dataTargetProvider.GetTarget() == null)
            {
                yield return new WaitUntil(() => dataTargetProvider.GetTarget() != null);
            }
            target = dataTargetProvider.GetTarget();

        }
        [ContextMenu("Start")]

        public void Start()
        {
            if (this.title == null && TryGetComponent<Title>(out var title))
            {
                this.title = title; // just for view in the inspector.
                title.SetTitle("GameObjects");
            }
            StartCoroutine(UpdateMenu());
        }

        [ContextMenu("Clear Menu")]

        public void ClearMenu()
        {
            foreach (var button in buttons)
            {
                DestroyImmediate(button.gameObject);
            }
            buttons.Clear();
        }
        [ContextMenu("Update Menu")]
        public IEnumerator UpdateMenu()
        {
            ClearMenu();
            if (!isSceneRoot)
            {
                yield return new WaitUntil(() => target != null);


                foreach (Transform child in target)
                {
                    var button = Instantiate(buttonPrefab, menuSectionTransform);
                    buttons.Add(button);

                    button.targetChildGameObject = child.gameObject;
                    button.targetParentGameObject = target.gameObject;
                }
            }
            else
            {

                var roots = SceneManager.GetActiveScene().GetRootGameObjects();
                foreach (var go in roots)
                {
                    if (go.TryGetComponent<XROrigin>(out _)) continue;
                    var button = Instantiate(buttonPrefab, menuSectionTransform);
                    buttons.Add(button);

                    button.targetChildGameObject = go;
                    if (sceneRootGO == null) { Debug.LogError($"sceneRoot gameobject should not be null for {this.name} in sceneRootPrefab"); }
                    else
                    {
                        button.targetParentGameObject = sceneRootGO;
                    }

                }
            }
        }
    }
}