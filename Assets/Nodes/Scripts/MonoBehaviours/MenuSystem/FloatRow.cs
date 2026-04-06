using System;
using TMPro;
using UnityEngine;

namespace DLN
{
    public class FloatRow : MonoBehaviour, IBoundRow
    {
        [SerializeField] TextMeshProUGUI label;

        // Your custom numeric UI
        [SerializeField] DigitsField digitsField;

        Func<float> _get;
        Action<float> _set;
        bool _writable;
        bool _mute;

        public void Bind(string name, Func<float> getter, Action<float> setter, bool writable)
        {
            _get = getter;
            _set = setter;
            _writable = writable;

            if (label) label.text = name;

            if (digitsField == null)
            {
                Debug.LogError($"FloatRow on '{gameObject.name}' has no DigitsField assigned.", this);
                return;
            }

            // Make sure we don't stack listeners if Bind is called more than once
            digitsField.OnValueChanged.RemoveListener(OnDigitsChanged);
            digitsField.OnValueChanged.AddListener(OnDigitsChanged);

            // If you later add an interactable/lock feature to DigitsField,
            // you can honor _writable here. For now: we just ignore edits when read-only.

            Refresh();
        }

        void OnDigitsChanged(float newValue)
        {
            if (_mute) return;
            if (!_writable) { Refresh(); return; }

            try
            {
                _set?.Invoke(newValue);
            }
            catch
            {
                // swallow (Unity reflection domain hates uncaught exceptions)
            }

            // Snap back to truth (handles clamping/side effects)
            Refresh();
        }

        public void Refresh()
        {
            if (digitsField == null || _get == null) return;

            float v;
            try
            {
                v = _get();
            }
            catch
            {
                return; // swallow
            }

            _mute = true;
            try
            {
                digitsField.SetValue(v);
            }
            catch
            {
                // swallow
            }
            _mute = false;
        }

        void OnDisable()
        {
            // Clean up listener
            if (digitsField != null)
            {
                try { digitsField.OnValueChanged.RemoveListener(OnDigitsChanged); }
                catch { /* swallow */ }
            }
        }

        void OnDestroy()
        {
            // Extra safety (in case OnDisable doesn't run in some destroy paths)
            if (digitsField != null)
            {
                try { digitsField.OnValueChanged.RemoveListener(OnDigitsChanged); }
                catch { /* swallow */ }
            }
        }
    }
}
