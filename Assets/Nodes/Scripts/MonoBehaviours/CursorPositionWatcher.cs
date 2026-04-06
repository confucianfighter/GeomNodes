using UnityEngine;
using DLN;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;
using System.Collections;
using UnityEngine.InputSystem;

namespace DLN
{
    [RequireComponent(typeof(XRKeyboardDisplay))]
    // this no longer watches. It subscribes hopefully based on a method.

    public class CaretPositionRelay : MonoBehaviour
    {
        public XRKeyboardDisplay keyboardDisplay;
        public XRKeyboard keyboard;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private int lastCaretPosition = -1;
        public void OnKeyboardOpened()
        {
            Debug.Log("CaretPositionWatcher: Keyboard opened");
            StartCoroutine(XRKeyboardProvider.GetKeyboard(
                onFound: (keyboard) =>
                {
                    Debug.Log("Caret position watcher Found keyboard display, subscribing to caret position updates.");
                    this.keyboard = keyboard;
                    this.keyboard.OnBeforeProcessKeyPress.AddListener(OnBeforeProcessKeyPress);
                },
                onTimeout: () =>
                {
                    Debug.LogWarning("CursorPositionWatcher: Timeout waiting for XRKeyboardDisplay");
                },
                timeoutSeconds: 2f
            ));
        }
        public void OnKeyboardFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                if (keyboard != null)
                {
                    Debug.Log("CaretPositionWatcher: Keyboard lost focus, unsubscribing from caret position updates.");
                    keyboard.OnBeforeProcessKeyPress.RemoveListener(OnBeforeProcessKeyPress);
                    keyboard = null;
                }
            }
        }
        private void OnBeforeProcessKeyPress()
        {
            Debug.Log("CaretPositionRelay: OnBeforeProcessKeyPress: updating keyboard caret position.");
            if (keyboard == null) Debug.LogError("CaretPositionRelay: keyboard is null yet we are subscribed to its OnBeforeProcessKeyPress event.");
            else
            {
                // check if there is a highlighted selection
                if (inputField.selectionAnchorPosition != inputField.selectionFocusPosition)
                {
                    int selectionStart = Mathf.Min(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);
                    int selectionEnd = Mathf.Max(inputField.selectionAnchorPosition, inputField.selectionFocusPosition);

                    inputField.text = inputField.text.Remove(selectionStart, selectionEnd - selectionStart);
                    inputField.caretPosition = selectionStart;
                    inputField.selectionAnchorPosition = selectionStart;
                    inputField.selectionFocusPosition = selectionStart;
                }
                int caretPos = inputField.caretPosition;
                Debug.Log($"CaretPositionRelay: Setting keyboard caret position to {caretPos}");
                keyboard.caretPosition = caretPos;
            }
        }


    }
}