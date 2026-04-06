using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DLN
{
    public class EnumRow : MonoBehaviour, IBoundRow
    {
        [SerializeField] TextMeshProUGUI label;
        [SerializeField] TMP_Dropdown dropdown;

        Type _enumType;
        Func<Enum> _get;
        Action<Enum> _set;
        bool _writable;

        bool _mute;
        List<string> _names;

        public void Bind(string name, Type enumType, Func<Enum> getter, Action<Enum> setter, bool writable)
        {
            _enumType = enumType;
            _get = getter;
            _set = setter;
            _writable = writable;

            if (label) label.text = name;
            if (dropdown) dropdown.interactable = writable;

            _names = new List<string>(Enum.GetNames(enumType));

            if (dropdown)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(_names);

                dropdown.onValueChanged.RemoveAllListeners();
                dropdown.onValueChanged.AddListener(i =>
                {
                    if (_mute || !_writable) return;
                    i = Mathf.Clamp(i, 0, _names.Count - 1);

                    try
                    {
                        var parsed = (Enum)Enum.Parse(_enumType, _names[i]);
                        _set?.Invoke(parsed);
                    }
                    catch
                    {
                        // swallow
                    }

                    Refresh();
                });
            }

            Refresh();
        }

        public void Refresh()
        {
            if (dropdown == null || _get == null || _names == null || _names.Count == 0) return;

            _mute = true;

            Enum v;
            try { v = _get(); }
            catch { _mute = false; return; }

            int idx = 0;
            if (v != null)
            {
                var s = v.ToString();
                idx = _names.IndexOf(s);
                if (idx < 0) idx = 0;
            }

            dropdown.value = idx;
            dropdown.RefreshShownValue();
            _mute = false;
        }
    }
}
