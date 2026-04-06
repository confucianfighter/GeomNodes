using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DLN
{
    // NOTE: File is EventRow.cs but the class name is InvokableRow. That's fine, Unity doesn't care,
    // but you may want to rename the file later for sanity.
    public class InvokableRow : MonoBehaviour, IBoundRow
    {
        [SerializeField] TextMeshProUGUI label;

        [Header("Invoke button (optional; will auto-find in children if missing)")]
        [SerializeField] UnityEngine.UI.Button invokeButton;

        [Header("Arg row prefabs")]
        [SerializeField] BoolRow boolRowPrefab;
        [SerializeField] FloatRow floatRowPrefab;
        [SerializeField] StringRow stringRowPrefab;
        [SerializeField] EnumRow enumRowPrefab;

        [Header("Setting it to content transform at runtime")]
        [SerializeField] Transform argsParent;

        InvokableBinding _binding;

        object[] _argValues = Array.Empty<object>();
        readonly List<IBoundRow> _argRows = new();

        void Awake()
        {
            if (!invokeButton)
                invokeButton = GetComponentInChildren<UnityEngine.UI.Button>();
        }
        void Start()
        {
            argsParent = transform.parent;
        }

        public void Bind(InvokableBinding binding)
        {
            _binding = binding;

            if (label)
                label.text = binding?.ToString() ?? "(null)";

            BuildArgRows(binding);

            if (invokeButton != null)
            {
                invokeButton.onClick.RemoveAllListeners();
                invokeButton.onClick.AddListener(() =>
                {
                    if (_binding == null || _binding.Invoke == null) return;

                    // We invoke with the current boxed values, already maintained by the arg rows.
                    _binding.Invoke(_argValues);
                });
            }
        }

        void BuildArgRows(InvokableBinding binding)
        {
            _argRows.Clear();

            var parent = GetOrCreateArgsParent();
            //ClearChildren(parent);

            if (binding == null || binding.ArgTypes == null || binding.ArgTypes.Length == 0)
            {
                _argValues = Array.Empty<object>();
                return;
            }

            _argValues = new object[binding.ArgTypes.Length];

            for (int i = 0; i < binding.ArgTypes.Length; i++)
            {
                var t = binding.ArgTypes[i];

                // Set a sensible default so Invoke always gets *something*.
                _argValues[i] = GetDefaultValue(t);

                // bool
                if (t == typeof(bool))
                {
                    if (!boolRowPrefab) continue;

                    var row = Instantiate(boolRowPrefab, parent);
                    row.Bind(
                        name: $"arg{i} : bool",
                        getter: () => _argValues[i] is bool b ? b : default,
                        setter: v => _argValues[i] = v,
                        writable: true
                    );
                    _argRows.Add(row);
                    continue;
                }

                // float
                if (t == typeof(float))
                {
                    if (!floatRowPrefab) continue;

                    var row = Instantiate(floatRowPrefab, parent);
                    row.Bind(
                        name: $"arg{i} : float",
                        getter: () => _argValues[i] is float f ? f : default,
                        setter: v => _argValues[i] = v,
                        writable: true
                    );
                    _argRows.Add(row);
                    continue;
                }

                // string
                if (t == typeof(string))
                {
                    if (!stringRowPrefab) continue;

                    var row = Instantiate(stringRowPrefab, parent);
                    row.Bind(
                        name: $"arg{i} : string",
                        getter: () => _argValues[i] as string ?? "",
                        setter: v => _argValues[i] = v ?? "",
                        writable: true
                    );
                    _argRows.Add(row);
                    continue;
                }

                // int (no IntRow yet -> use StringRow as editor, store int in _argValues)
                if (t == typeof(int))
                {
                    if (!stringRowPrefab) continue;

                    var row = Instantiate(stringRowPrefab, parent);
                    row.Bind(
                        name: $"arg{i} : int",
                        getter: () => (_argValues[i] is int n) ? n.ToString() : "0",
                        setter: s =>
                        {
                            if (int.TryParse(s, out var n)) _argValues[i] = n;
                            else _argValues[i] = 0;
                            row.Refresh(); // snap text back if user typed junk
                        },
                        writable: true
                    );
                    _argRows.Add(row);
                    continue;
                }

                // Vector3 -> 3 FloatRows, store Vector3 in _argValues[i]
                if (t == typeof(Vector3))
                {
                    if (!floatRowPrefab) continue;

                    // Ensure stored value is a Vector3
                    if (!(_argValues[i] is Vector3)) _argValues[i] = Vector3.zero;

                    var rowX = Instantiate(floatRowPrefab, parent);
                    var rowY = Instantiate(floatRowPrefab, parent);
                    var rowZ = Instantiate(floatRowPrefab, parent);

                    void RefreshXYZ()
                    {
                        try { rowX.Refresh(); } catch { }
                        try { rowY.Refresh(); } catch { }
                        try { rowZ.Refresh(); } catch { }
                    }

                    rowX.Bind(
                        name: $"arg{i} : Vector3.x",
                        getter: () => (_argValues[i] is Vector3 v) ? v.x : 0f,
                        setter: x =>
                        {
                            var v = (_argValues[i] is Vector3 vv) ? vv : Vector3.zero;
                            v.x = x;
                            _argValues[i] = v;
                            RefreshXYZ();
                        },
                        writable: true
                    );

                    rowY.Bind(
                        name: $"arg{i} : Vector3.y",
                        getter: () => (_argValues[i] is Vector3 v) ? v.y : 0f,
                        setter: y =>
                        {
                            var v = (_argValues[i] is Vector3 vv) ? vv : Vector3.zero;
                            v.y = y;
                            _argValues[i] = v;
                            RefreshXYZ();
                        },
                        writable: true
                    );

                    rowZ.Bind(
                        name: $"arg{i} : Vector3.z",
                        getter: () => (_argValues[i] is Vector3 v) ? v.z : 0f,
                        setter: z =>
                        {
                            var v = (_argValues[i] is Vector3 vv) ? vv : Vector3.zero;
                            v.z = z;
                            _argValues[i] = v;
                            RefreshXYZ();
                        },
                        writable: true
                    );

                    _argRows.Add(rowX);
                    _argRows.Add(rowY);
                    _argRows.Add(rowZ);
                    continue;
                }

                // enum
                if (t != null && t.IsEnum)
                {
                    if (!enumRowPrefab) continue;

                    // Default to first enum value
                    try
                    {
                        var vals = Enum.GetValues(t);
                        _argValues[i] = vals.Length > 0 ? (Enum)vals.GetValue(0) : null;
                    }
                    catch { _argValues[i] = null; }

                    var row = Instantiate(enumRowPrefab, parent);
                    row.Bind(
                        name: $"arg{i} : {t.Name}",
                        enumType: t,
                        getter: () => _argValues[i] as Enum,
                        setter: v => _argValues[i] = v,
                        writable: true
                    );
                    _argRows.Add(row);
                    continue;
                }

                // Fallback: unsupported type -> show string row but do not attempt conversion
                if (stringRowPrefab)
                {
                    var row = Instantiate(stringRowPrefab, parent);
                    row.Bind(
                        name: $"arg{i} : {(t != null ? t.Name : "null")} (unsupported)",
                        getter: () => _argValues[i]?.ToString() ?? "",
                        setter: _ => { /* ignore */ },
                        writable: false
                    );
                    _argRows.Add(row);
                }
            }

            Refresh();
        }

        Transform GetOrCreateArgsParent()
        {
            return transform.parent;
            if (argsParent) return argsParent;

            // Try to find an existing child named "Args"
            var existing = transform.Find("Args");
            if (existing)
            {
                argsParent = existing;
                return argsParent;
            }

            // Create it
            var go = new GameObject("Args", typeof(RectTransform));
            go.transform.SetParent(transform, worldPositionStays: false);
            argsParent = go.transform;

            // Optional: ensure it layouts nicely under the row
            var vlg = go.GetComponent<VerticalLayoutGroup>();
            if (!vlg) vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 2f;

            var fitter = go.GetComponent<ContentSizeFitter>();
            if (!fitter) fitter = go.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            return argsParent;
        }

        static void ClearChildren(Transform parent)
        {
            if (!parent) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        static object GetDefaultValue(Type t)
        {
            try
            {
                if (t == typeof(string)) return "";
                if (t == typeof(Vector3)) return Vector3.zero;
                if (t != null && t.IsEnum)
                {
                    var vals = Enum.GetValues(t);
                    return vals.Length > 0 ? vals.GetValue(0) : null;
                }

                // value types -> default(T)
                if (t != null && t.IsValueType)
                    return Activator.CreateInstance(t);

                return null;
            }
            catch
            {
                return null;
            }
        }

        public void Refresh()
        {
            // Refresh arg editors (useful if you later programmatically set defaults)
            for (int i = 0; i < _argRows.Count; i++)
            {
                try { _argRows[i]?.Refresh(); }
                catch { /* swallow */ }
            }
        }
    }
}