using UnityEngine;
using DLN;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;
using UnityEngine.Events;

// has a list of left digits
// has a list of right digits

public delegate bool condition(int a, int b);

public class DigitsField : MonoBehaviour
{
    public List<DigitColumn> leftDigits = new List<DigitColumn>();
    public List<DigitColumn> rightDigits = new List<DigitColumn>();
    [SerializeField] private Transform leftRow;
    [SerializeField] private Transform rightRow;
    [SerializeField] float currentValue = 123.321f;
    public bool rebuildOnUpdate = false;
    public bool removeIfZero = false;
    public DigitColumn digitPrefab;
    public bool debug = false;
    public UnityEvent<int> SetSign;
    public UnityEvent<float> OnValueChanged;
    public SignToggle signToggle;
    public int startNumberOfDecimalDigits = 3;
    void Log(System.Func<string> message)
    {
        if (debug)
            Debug.Log($"[{name}] {message()}", this);
    }

    private void Awake()
    {
        if (leftRow == null || rightRow == null)
        {
            Debug.LogError("DigitField: leftRow or rightRow is not assigned.");
        }
        else
        {
            clearLeftRow();
            clearRightRow();
        }


    }
    private void Start()
    {
        clearLeftRow();
        clearRightRow();
        InitializeDecimalRow();
        UpdateDigits();
    }
    private void InitializeDecimalRow()
    {
        Log(() => "Initializing decimal row");
        while (rightDigits.Count < startNumberOfDecimalDigits)
        {
            AddRightSideDigit();
        }
    }
    private void clearLeftRow()
    {
        Log(() => "Clearing left row");
        foreach (var digit in leftRow.GetComponentsInChildren<DigitColumn>())
        {
            digit.OnValueChanged.RemoveListener(OnDigitValueChanged);
            Destroy(digit.gameObject);
        }
        leftDigits.Clear();
    }
    private void clearRightRow()
    {
        Log(() => "Clearing right row");
        foreach (var digit in rightRow.GetComponentsInChildren<DigitColumn>())
        {
            digit.OnValueChanged.RemoveListener(OnDigitValueChanged);
            Destroy(digit.gameObject);
        }
        rightDigits.Clear();
    }
    private void EnsureCorrectNumberOfSlots(int numLeftDigits, int numRightDigits)
    {
        if (rebuildOnUpdate)
        {
            Log(() => "Clearing all digit rows for rebuild");
            clearLeftRow();
            clearRightRow();
        }
        condition loopCondition;

        if (removeIfZero)
        {
            loopCondition = (a, b) => a != b;

        }
        else
        {
            loopCondition = (a, b) => a < b;
        }


        while (loopCondition(leftDigits.Count, numLeftDigits))
        {

            if (leftDigits.Count < numLeftDigits)
            {
                Log(() => $"Number of left digits: {leftDigits.Count}. Length of left side string: {numLeftDigits}. Adding left digit");
                AddLeftSideDigit();
            }
            else if (leftDigits.Count > numLeftDigits && removeIfZero)
            {
                Log(() => $"Number of left digits: {leftDigits.Count}. Length of left side string: {numLeftDigits}. Removing left digit");
                RemoveLeftSideDigit();
            }
        }

        // while (loopCondition(rightDigits.Count, numRightDigits))
        // {
        //     if (rightDigits.Count < numRightDigits)
        //     {
        //         Log(() => $"Number of right digits: {rightDigits.Count}. Length of right side string: {numRightDigits}. Adding right digit");
        //         AddRightSideDigit();

        //     }
        //     else if (rightDigits.Count > numRightDigits && removeIfZero)
        //     {
        //         Log(() => $"Number of right digits: {rightDigits.Count}. Length of right side string: {numRightDigits}. Removing right digit");
        //         RemoveRightSideDigit();
        //     }
        // }
    }
    private int GetNumberOfDecimalDigits()
    {
        return rightDigits.Count;
    }
    protected void UpdateDigits()
    {
        // clear existing digits if any
        var floatStr = currentValue.ToString("F" + GetNumberOfDecimalDigits());
        Log(() => $"Digits flield parsed {currentValue} to string: {floatStr}");
        string[] parts = floatStr.Split('.');
        string intString = parts[0];
        string fracString = parts.Length > 1 ? parts[1] : "";
        // trim fracString to number of decimal digits
        if (fracString.Length > GetNumberOfDecimalDigits())
        {
            fracString = fracString.Substring(0, GetNumberOfDecimalDigits());
        }
        // if intString starts with '-' set sign to -1 and remove the '-' from the string
        if (intString.StartsWith("-"))
        {
            intString = intString.Substring(1);
            signToggle.SetSign(-1);
        }
        else
        {
            signToggle.SetSign(1);
        }
        EnsureCorrectNumberOfSlots(intString.Length, fracString.Length);
        // Update left digits
        // flipt the int string
        intString = new string(intString.Reverse().ToArray());
        for (int i = 0; i < intString.Count(); i++)
        {
            var value = int.Parse(intString[i].ToString());
            leftDigits[i].SetValue(value);
        }

        // get only the digits from the float string

        for (int i = 0; i < fracString.Length; i++)
        {
            int value = int.Parse(fracString[i].ToString());
            rightDigits[i].SetValue(value);
        }
    }
    public void OnDigitValueChanged()
    {

        currentValue = float.Parse(GetStringFromDigits());
        OnValueChanged?.Invoke(currentValue);
        UpdateDigits();
    }
    public float GetValue()
    {
        return currentValue;
    }
    public string GetStringFromDigits()
    {
        // don't even use the data, just use a stringbuilder and iterate through all digits to get the number.
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (signToggle.GetSign() == -1)
        {
            sb.Append("-");
        }
        for (int i = leftDigits.Count - 1; i >= 0; i--)
        {
            sb.Append(leftDigits[i].inputField.text);
        }
        if (rightDigits.Count > 0)
        {
            sb.Append(".");
            for (int i = 0; i < rightDigits.Count; i++)
            {
                sb.Append(rightDigits[i].inputField.text);
            }
        }
        return sb.ToString();
    }
    private void AddRightSideDigit()
    {
        var newDigit = Instantiate(digitPrefab, rightRow);
        newDigit.SetValue("1");
        rightDigits.Add(newDigit); // add to the end
        newDigit.significand = rightDigits.Count;
        newDigit.OnValueChanged.AddListener(OnDigitValueChanged);
    }
    private void RemoveRightSideDigit()
    {
        Log(() => "Remove right side digit called");
        if (rightDigits.Count > 1)
        {
            var digitToRemove = rightDigits[rightDigits.Count - 1];
            rightDigits.RemoveAt(rightDigits.Count - 1);
            digitToRemove.OnValueChanged.RemoveListener(OnDigitValueChanged);
            DestroyImmediate(digitToRemove.gameObject);
        }
    }
    private void AddLeftSideDigit()
    {
        Log(() => "Add left side digit called");
        var newDigit = Instantiate(digitPrefab, leftRow);
        newDigit.SetValue("1");
        leftDigits.Add(newDigit); // add to the end
        newDigit.significand = leftDigits.Count;
        newDigit.OnValueChanged.AddListener(OnDigitValueChanged);
    }
    private void RemoveLeftSideDigit()
    {
        Log(() => "Remove left side digit called");
        if (leftDigits.Count > 1)
        {
            var digitToRemove = leftDigits[leftDigits.Count - 1];
            leftDigits.RemoveAt(leftDigits.Count - 1);
            digitToRemove.OnValueChanged.RemoveListener(OnDigitValueChanged);
            DestroyImmediate(digitToRemove.gameObject);

        }
    }
    public void AddLeftClicked()
    {
        Log(() => "Add left clicked called");
        AddLeftSideDigit();
        OnDigitValueChanged();
    }
    public void RemoveLeftClicked()
    {
        Log(() => "Remove left clicked called");
        RemoveLeftSideDigit();
        OnDigitValueChanged();
    }
    public void AddRightClicked()
    {
        Log(() => "Add right clicked called");
        AddRightSideDigit();
        OnDigitValueChanged();
    }
    public void RemoveRightClicked()
    {
        Log(() => "Remove right clicked called");
        RemoveRightSideDigit();
        OnDigitValueChanged();
    }
    public void SetValue(float newValue)
    {
        Log(() => $"SetValue called with {newValue}");
        currentValue = newValue;
        UpdateDigits();
    }
}