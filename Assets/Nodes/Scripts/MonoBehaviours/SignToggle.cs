using UnityEngine;
using DLN;
using UnityEngine.Events;
using TMPro;

namespace DLN
{
    public class SignToggle : MonoBehaviour
    {
        public TMP_InputField signText;
        [SerializeField]
        private int sign;
        public UnityEvent<int> OnUserChangedSign;
        public void Start()
        {
            if (signText == null)
            {
                signText = GetComponentInChildren<TMP_InputField>();
            }
        }
        public void ToggleSign()
        {
            sign *= -1;
            signText.text = sign == 1 ? "+" : "-";
            OnUserChangedSign?.Invoke(sign);
        }

        public int GetSign()
        {
            return sign;
        }
        public void SetSign(int newSign)
        {
            if (sign != newSign)
            {
                sign = newSign;
                signText.text = sign == 1 ? "+" : "-";
            }
        }

    }
}