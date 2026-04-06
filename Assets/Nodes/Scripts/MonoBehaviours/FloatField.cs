using UnityEngine;
using DLN;
using TMPro;
using UnityEngine.Events;

public class FloatField : MonoBehaviour
{
    public TMP_InputField inputField;
    public UnityEvent<float> OnValueChanged;
    public int decimalPlaces = 3;

    [SerializeField] private float lastValue = float.NaN;


    protected void Awake()
    {
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
            if (inputField == null)
            {
                Debug.LogError("FloatField: No TMP_InputField found on this GameObject.");
            }
        }
    }
    public void OnInputFieldValueChanged(string newValue)
    {
        if (float.TryParse(newValue, out float parsedValue))
        {
            OnValueChanged?.Invoke(parsedValue);
        }
        else
        {
            Debug.LogError("FloatField: Invalid float input: " + newValue);
        }
    }
    public void OnTargetValueChanged(float newValue)
    {
        if (newValue != lastValue)
        {
            lastValue = newValue;
            var newText = newValue.ToString("F" + decimalPlaces);
            if (inputField.text != newText)
            {
                inputField.text = newText;
            }
        }
    }


}