// stores a reference to a tmp on which to set the title via a method
using UnityEngine;
using TMPro;

namespace DLN
{
    public class Title : MonoBehaviour
    {
        public TextMeshProUGUI tmp;

        public void SetTitle(string title)
        {
            if (tmp != null)
            {
                tmp.text = title;
            }
            else
            {
                Debug.LogWarning("Title: tmp reference is null.");
            }
        }
    }
}