using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN
{
    public class ComponentMenu : MonoBehaviour
    {
        [Header("Row Prefabs")]
        [SerializeField] BoolRow boolRowPrefab;
        [SerializeField] FloatRow floatRowPrefab;
        [SerializeField] StringRow stringRowPrefab;
        [SerializeField] EnumRow enumRowPrefab;

        [Header("Layout")]
        [SerializeField] Transform contentParent;

        [Header("Target")]
        [SerializeField] Component component;
        [SerializeField] InvokableRow invokableRowPrefab;

        readonly List<IBoundRow> _rows = new();

        public void SetTargetComponent(Component component)
        {
            this.component = component;
            PopulateMenu();
        }

        [ContextMenu("PopulateMenu")]
        public void PopulateMenu()
        {
            ClearContent();
            _rows.Clear();

            if (component == null || contentParent == null)
                return;

            var members = ReflectionMenuBindings.GetBindableMembers(
                component,
                includeFields: true,
                includeProperties: true,
                includeReadOnlyProperties: false
            );

            foreach (var m in members)
            {
                // bool
                if (m.ValueType == typeof(bool))
                {
                    SpawnBool(m);
                    continue;
                }

                // float
                if (m.ValueType == typeof(float))
                {
                    SpawnFloat(m);
                    continue;
                }

                // string
                if (m.ValueType == typeof(string))
                {
                    SpawnString(m);
                    continue;
                }

                // Vector3 -> 3 FloatRows in SAME contentParent
                if (m.ValueType == typeof(Vector3))
                {
                    SpawnVector3AsThreeFloats(m);
                    continue;
                }

                // enum
                if (m.ValueType.IsEnum)
                {
                    SpawnEnum(m);
                    continue;
                }
            }
            // UnityEvents
            var events = ReflectionInvokables.GetUnityEventBindings(component);
            foreach (var e in events)
            {
                SpawnInvokable(e);
            }

            // Public Methods (void)
            var methods = ReflectionInvokables.GetMethodBindings(component);
            foreach (var m in methods)
            {
                SpawnInvokable(m);
            }


            RefreshAll();
        }

        [ContextMenu("RefreshAll")]
        public void RefreshAll()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                try { _rows[i]?.Refresh(); }
                catch { /* swallow */ }
            }
        }

        // ---------------------------
        // Spawners
        // ---------------------------

        void SpawnBool(MemberBinding m)
        {
            if (!boolRowPrefab) return;

            var row = Instantiate(boolRowPrefab, contentParent);
            row.Bind(
                m.Name,
                getter: () => TryGetValue(m, out bool v) ? v : default,
                setter: v => SafeSet(m, v),
                writable: m.CanWrite
            );

            _rows.Add(row);
        }

        void SpawnFloat(MemberBinding m)
        {
            if (!floatRowPrefab) return;

            var row = Instantiate(floatRowPrefab, contentParent);
            row.Bind(
                m.Name,
                getter: () => TryGetValue(m, out float v) ? v : default,
                setter: v => SafeSet(m, v),
                writable: m.CanWrite
            );

            _rows.Add(row);
        }

        void SpawnString(MemberBinding m)
        {
            if (!stringRowPrefab) return;

            var row = Instantiate(stringRowPrefab, contentParent);
            row.Bind(
                m.Name,
                getter: () => TryGetValue(m, out string v) ? v : "",
                setter: v => SafeSet(m, v),
                writable: m.CanWrite
            );

            _rows.Add(row);
        }

        void SpawnEnum(MemberBinding m)
        {
            if (!enumRowPrefab) return;

            var row = Instantiate(enumRowPrefab, contentParent);
            row.Bind(
                m.Name,
                enumType: m.ValueType,
                getter: () =>
                {
                    if (TryGetValue(m, out Enum v) && v != null) return v;

                    // fallback to first value
                    try { return (Enum)Enum.GetValues(m.ValueType).GetValue(0); }
                    catch { return null; }
                },
                setter: v => SafeSet(m, v),
                writable: m.CanWrite
            );

            _rows.Add(row);
        }
        void SpawnInvokable(InvokableBinding b)
        {
            if (!invokableRowPrefab) return;

            var row = Instantiate(invokableRowPrefab, contentParent);
            row.Bind(b);
            _rows.Add(row);
        }


        void SpawnVector3AsThreeFloats(MemberBinding m)
        {
            if (!floatRowPrefab) return;

            // Create three rows in the same parent
            var rowX = Instantiate(floatRowPrefab, contentParent);
            var rowY = Instantiate(floatRowPrefab, contentParent);
            var rowZ = Instantiate(floatRowPrefab, contentParent);

            // Shared refresher so editing one axis snaps all 3 back to truth
            void RefreshXYZ()
            {
                try { rowX.Refresh(); } catch { }
                try { rowY.Refresh(); } catch { }
                try { rowZ.Refresh(); } catch { }
            }

            rowX.Bind(
                $"{m.Name}.x",
                getter: () =>
                {
                    if (!TryGetValue(m, out Vector3 v)) return 0f;
                    return v.x;
                },
                setter: x =>
                {
                    if (!m.CanWrite) return;
                    if (!TryGetValue(m, out Vector3 v)) return;

                    v.x = x;
                    SafeSet(m, v);
                    RefreshXYZ();
                },
                writable: m.CanWrite
            );

            rowY.Bind(
                $"{m.Name}.y",
                getter: () =>
                {
                    if (!TryGetValue(m, out Vector3 v)) return 0f;
                    return v.y;
                },
                setter: y =>
                {
                    if (!m.CanWrite) return;
                    if (!TryGetValue(m, out Vector3 v)) return;

                    v.y = y;
                    SafeSet(m, v);
                    RefreshXYZ();
                },
                writable: m.CanWrite
            );

            rowZ.Bind(
                $"{m.Name}.z",
                getter: () =>
                {
                    if (!TryGetValue(m, out Vector3 v)) return 0f;
                    return v.z;
                },
                setter: z =>
                {
                    if (!m.CanWrite) return;
                    if (!TryGetValue(m, out Vector3 v)) return;

                    v.z = z;
                    SafeSet(m, v);
                    RefreshXYZ();
                },
                writable: m.CanWrite
            );

            _rows.Add(rowX);
            _rows.Add(rowY);
            _rows.Add(rowZ);
        }

        // ---------------------------
        // Safe helpers (never throw)
        // ---------------------------

        static void SafeSet(MemberBinding binding, object value)
        {
            if (binding?.TrySetValue == null) return;
            try { binding.TrySetValue(value); }
            catch { /* swallow */ }
        }

        static bool TryGetValue<T>(MemberBinding binding, out T value)
        {
            value = default;
            if (binding == null || binding.TryGetValue == null) return false;

            bool ok;
            try { ok = binding.TryGetValue(); }
            catch { return false; }

            if (!ok) return false;

            try
            {
                if (binding.CurrentValue is T t)
                {
                    value = t;
                    return true;
                }

                // Special case: asking for Enum
                if (typeof(T) == typeof(Enum) && binding.CurrentValue != null && binding.ValueType.IsEnum)
                {
                    value = (T)(object)(Enum)binding.CurrentValue;
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        void ClearContent()
        {
            if (contentParent == null) return;

            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                var go = contentParent.GetChild(i).gameObject;
                Destroy(go);
            }
        }
    }
}
