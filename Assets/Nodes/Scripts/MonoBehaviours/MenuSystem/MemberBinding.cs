using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DLN
{
    public sealed class MemberBinding
    {
        public string Name;
        public Type ValueType;
        public bool CanWrite;

        /// <summary>Last fetched value (optional convenience for initial bind).</summary>
        public object CurrentValue;

        /// <summary>Safe getter: never throws. Returns true if it successfully got a value.</summary>
        public Func<bool> TryGetValue;

        /// <summary>Safe setter: never throws.</summary>
        public Action<object> TrySetValue;

        public override string ToString() => $"{Name} : {ValueType.Name} (write={CanWrite})";
    }

    public static class ReflectionMenuBindings
    {
        // Tune this list if you see menu spam.
        static readonly HashSet<string> DefaultSkipPropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "gameObject",
            "transform",
            "attachedRigidbody",
            "rigidbody",
            "name",
            "tag",
            "hideFlags"
        };

        public static List<MemberBinding> GetBindableMembers(
            Component target,
            bool includeFields = true,
            bool includeProperties = true,
            bool includeReadOnlyProperties = false,
            Func<MemberInfo, bool> extraFilter = null)
        {
            if (target == null) return new List<MemberBinding>();

            var t = target.GetType();
            var results = new List<MemberBinding>();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            if (includeFields)
                AddFieldBindings(target, t, flags, results, extraFilter);

            if (includeProperties)
                AddPropertyBindings(target, t, flags, results, includeReadOnlyProperties, extraFilter);

            // Nicer ordering + de-dupe by name (rare collisions)
            return results
                .GroupBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        static void AddFieldBindings(
            Component target,
            Type t,
            BindingFlags flags,
            List<MemberBinding> results,
            Func<MemberInfo, bool> extraFilter)
        {
            foreach (var f in t.GetFields(flags))
            {
                if (f == null) continue;
                if (f.IsStatic) continue;
                if (f.IsLiteral) continue;   // const
                if (f.IsInitOnly) continue;  // readonly

                bool unitySerialized = f.IsPublic || f.GetCustomAttribute<SerializeField>() != null;
                if (!unitySerialized) continue;

                if (extraFilter != null && !extraFilter(f)) continue;

                var fieldType = f.FieldType;
                if (!IsSupportedType(fieldType)) continue;

                var field = f; // capture
                var mb = new MemberBinding
                {
                    Name = field.Name,
                    ValueType = fieldType,
                    CanWrite = true
                };

                mb.TryGetValue = () =>
                {
                    try
                    {
                        mb.CurrentValue = field.GetValue(target);
                        return true;
                    }
                    catch
                    {
                        mb.CurrentValue = null;
                        return false;
                    }
                };

                mb.TrySetValue = (obj) =>
                {
                    try
                    {
                        field.SetValue(target, obj);
                    }
                    catch
                    {
                        // swallow
                    }
                };

                // Probe once; skip if getter throws
                if (!mb.TryGetValue()) continue;

                results.Add(mb);
            }
        }

        static void AddPropertyBindings(
    Component target,
    Type t,
    BindingFlags flags,
    List<MemberBinding> results,
    bool includeReadOnlyProperties,
    Func<MemberInfo, bool> extraFilter)
        {
            foreach (var p in t.GetProperties(flags))
            {
                if (p == null) continue;

                // Skip indexers
                if (p.GetIndexParameters().Length != 0) continue;

                // Require public getter
                var getter = p.GetGetMethod(nonPublic: true);
                if (getter == null || !getter.IsPublic) continue;

                // Optional: only properties declared on this type (avoids Unity base spam)
                // If you *want* inherited properties too, remove this.
                if (p.DeclaringType != t) continue;

                // Skip Obsolete
                if (p.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

                // Optional skiplist
                if (DefaultSkipPropertyNames.Contains(p.Name)) continue;

                bool canWrite = p.SetMethod != null && p.SetMethod.IsPublic;
                if (!canWrite && !includeReadOnlyProperties) continue;

                if (extraFilter != null && !extraFilter(p)) continue;

                var propType = p.PropertyType;
                if (!IsSupportedType(propType)) continue;

                var prop = p; // capture
                var mb = new MemberBinding
                {
                    Name = prop.Name,
                    ValueType = propType,
                    CanWrite = canWrite
                };

                mb.TryGetValue = () =>
                {
                    try
                    {
                        mb.CurrentValue = prop.GetValue(target);
                        return true;
                    }
                    catch
                    {
                        mb.CurrentValue = null;
                        return false;
                    }
                };

                mb.TrySetValue = (obj) =>
                {
                    if (!canWrite) return;
                    try { prop.SetValue(target, obj); }
                    catch { /* swallow */ }
                };

                if (!mb.TryGetValue()) continue;

                results.Add(mb);
            }
        }


        public static bool IsSupportedType(Type type)
        {
            return type == typeof(bool)
                || type == typeof(float)
                || type == typeof(string)
                || type == typeof(Vector3)
                || type == typeof(int)
                || type.IsEnum;
        }
    }
}
