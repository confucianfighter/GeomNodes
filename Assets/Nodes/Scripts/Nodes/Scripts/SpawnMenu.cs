using UnityEngine;
using System.Collections.Generic;
using System.Collections;
namespace DLN
{
    public class SpawnMenu : MonoBehaviour
    {
        public List<GameObject> spawnables;
        [SerializeField] private Transform contentTransform;
        public SpawnButton spawnButtonPrefab;

        public void Start()
        {
            StartCoroutine(InitializeRoutine());
        }
        IEnumerator InitializeRoutine()
        {
            yield return new WaitForSeconds(.5f);
            foreach (var prefab in spawnables)
            {
                var button = Instantiate(spawnButtonPrefab, contentTransform);
                button.spawnable = prefab;
            }
        }
    }
}