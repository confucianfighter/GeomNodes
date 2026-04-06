using System;
using TMPro;
using UnityEngine;

namespace DLN
{
    public class StringRow : MonoBehaviour, IBoundRow
    {
        [SerializeField] TMP_Text label;
        [SerializeField] TMP_InputField input;

        Func<string> _get;
        Action<string> _set;
        bool _writable;
        bool _mute;

        public void Bind(string name, Func<string> getter, Action<string> setter, bool writable)
        {
            _get = getter;
            _set = setter;
            _writable = writable;

            if (label) label.text = name;
            if (input) input.interactable = writable;

            if (input)
            {
                input.onValueChanged.RemoveListener(_OnInputChanged);
                input.onValueChanged.AddListener(_OnInputChanged);
            }

            Refresh();
        }
        private void _OnInputChanged(string txt)
        {
            if (_mute || !_writable) return;
            _set?.Invoke(txt);
            Refresh();
        }

        public void Refresh()
        {
            if (input == null || _get == null) return;

            _mute = true;
            string v;
            try { v = _get(); }
            catch { _mute = false; return; }

            input.text = v ?? string.Empty;
            _mute = false;
        }
    }
}
