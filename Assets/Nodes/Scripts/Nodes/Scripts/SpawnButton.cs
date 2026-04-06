using UnityEngine;
using TMPro;

namespace DLN
{
    public class SpawnButton : MonoBehaviour
    {
        public GameObject spawnable;

        public void Start()
        {
            var tmp = GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = spawnable.name;
            var button = GetComponentInChildren<UnityEngine.UI.Button>();
            button.onClick.AddListener(Spawn);
        }
        public void Spawn()
        {
            var go = Instantiate(spawnable);
            go.transform.position = new Vector3(1, 1, 1);
        }
    }
}