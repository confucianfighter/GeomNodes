using UnityEngine;
using DLN;
using TMPro;
using UnityEngine.Events;

namespace DLN
{
    /// has increment and decrement unity events
    /// has a reference to a tmp_inputfield to display the digit
    /// 
    /// 
    public struct DigitData
    {
        public int digit;
        public int significand; // 1 for units, 10 for tens, 100 for hundreds, etc.


    }
    public class DigitColumn : MonoBehaviour
    {
        public TMP_InputField inputField;
        public UnityEvent OnValueChanged;
        public int significand = 1;

        protected void Awake()
        {
            if (inputField == null)
            {
                inputField = GetComponent<TMP_InputField>();
                if (inputField == null)
                {
                    Debug.LogError("DigitController: No TMP_InputField found on this GameObject.");
                }
            }
        }

        public void OnInputFieldValueChanged(string newValue)
        {
            // do nothing, just prevent user input
            inputField.text = newValue;
        }

        public void Increment()
        {
            var value = int.Parse(inputField.text);
            value += 1;
            if (value > 9) value = 0;
            SetValue(value);

            OnValueChanged?.Invoke();
        }

        public void Decrement()
        {
            var value = int.Parse(inputField.text);
            value -= 1;
            if (value < 0) value = 9;
            SetValue(value);
            OnValueChanged?.Invoke();
        }
        public void SetValue(int newValue)
        {
            var currentValue = inputField.text.ToString();
            if (currentValue != newValue.ToString())
            {
                inputField.text = newValue.ToString();
            }
        }
        public void SetValue(string newValue)
        {
            var currentValue = inputField.text.ToString();
            if (currentValue != newValue)
            {
                inputField.text = newValue;
            }
        }
    }
}