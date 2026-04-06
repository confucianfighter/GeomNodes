using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DLN
{
    public class BoolRow : MonoBehaviour, IBoundRow
    {
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] Toggle toggle;

        Func<bool> _get;
        Action<bool> _set;
        bool _writable;

        public void Bind(string name, Func<bool> getter, Action<bool> setter, bool writable)
        {
            _get = getter;
            _set = setter;
            _writable = writable;

            if (label) label.text = name;
            if (toggle) toggle.interactable = writable;

            if (toggle)
            {
                toggle.onValueChanged.RemoveListener(_OnToggleChanged);
                toggle.onValueChanged.AddListener(_OnToggleChanged);
            }

            Refresh();
        }
        private void _OnToggleChanged(bool value)
        {
            if (!_writable) return;
            _set?.Invoke(value);
            toggle.graphic.SetAllDirty(); // Force visual update.
        }

        public void Refresh()
        {
            if (toggle == null || _get == null) return;

            bool val;
            try { val = _get(); }
            catch
            {
                Debug.LogError("Could not get toggle value in Refresh()");
                return;
            }

            if (toggle.isOn != val) toggle.isOn = val;
            // Force the toggle to update its visual state, even if the value didn't change.
        }

    }
}
