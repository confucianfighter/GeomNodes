using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace DLN
{
    public sealed class InvokableBinding
    {
        public string Name;
        public Type[] ArgTypes = Array.Empty<Type>();

        /// <summary>Invoke with boxed args (length must match ArgTypes).</summary>
        public Action<object[]> Invoke;

        public override string ToString() => $"{Name}";
        //=> ArgTypes == null || ArgTypes.Length == 0
        //? $"{Name}()"
        //: $"{Name}({string.Join(", ", ArgTypes.Select(t => t?.Name ?? "null"))})";
    }

    public static class ReflectionInvokables
    {
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Finds UnityEvent fields/properties (UnityEvent, UnityEvent&lt;T&gt;, custom UnityEvent subclasses).
        /// Includes [SerializeField] private fields because we scan NonPublic.
        /// </summary>
        public static List<InvokableBinding> GetUnityEventBindings(
            Component target,
            bool includeFields = true,
            bool includeProperties = true,
            bool includeBaseTypes = true,
            Func<MemberInfo, bool> extraFilter = null)
        {
            if (!target) return new();

            var t = target.GetType();
            var results = new List<InvokableBinding>();

            IEnumerable<Type> typeChain = includeBaseTypes ? EnumerateTypeChain(t) : new[] { t };

            foreach (var type in typeChain)
            {
                if (includeFields)
                {
                    foreach (var f in type.GetFields(Flags))
                    {
                        if (f == null || f.IsStatic) continue;
                        if (!typeof(UnityEventBase).IsAssignableFrom(f.FieldType)) continue;
                        if (extraFilter != null && !extraFilter(f)) continue;

                        var field = f; // capture
                        UnityEventBase evt = null;
                        try { evt = field.GetValue(target) as UnityEventBase; } catch { }
                        if (evt == null) continue;

                        results.Add(MakeUnityEventBinding(field.Name, evt, field.FieldType));
                    }
                }

                if (includeProperties)
                {
                    foreach (var p in type.GetProperties(Flags))
                    {
                        if (p == null) continue;
                        if (p.GetIndexParameters().Length != 0) continue;

                        var getter = p.GetGetMethod(nonPublic: true);
                        if (getter == null) continue;

                        if (!typeof(UnityEventBase).IsAssignableFrom(p.PropertyType)) continue;
                        if (extraFilter != null && !extraFilter(p)) continue;

                        var prop = p; // capture
                        UnityEventBase evt = null;
                        try { evt = prop.GetValue(target) as UnityEventBase; } catch { }
                        if (evt == null) continue;

                        results.Add(MakeUnityEventBinding(prop.Name, evt, prop.PropertyType));
                    }
                }
            }

            return results
                .GroupBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        /// <summary>
        /// Finds public instance void methods (optionally include non-public too).
        /// Skips property getters/setters and Unity magic spam.
        /// </summary>
        public static List<InvokableBinding> GetMethodBindings(
            Component target,
            bool publicOnly = true,
            bool includeBaseTypes = true,
            Func<MethodInfo, bool> extraFilter = null)
        {
            if (!target) return new();

            var t = target.GetType();
            var results = new List<InvokableBinding>();

            var methodFlags = BindingFlags.Instance | (publicOnly ? BindingFlags.Public : (BindingFlags.Public | BindingFlags.NonPublic));

            IEnumerable<Type> typeChain = includeBaseTypes ? EnumerateTypeChain(t) : new[] { t };

            foreach (var type in typeChain)
            {
                foreach (var m in type.GetMethods(methodFlags))
                {
                    if (m == null) continue;
                    if (m.IsStatic) continue;
                    if (m.IsSpecialName) continue;          // property accessors, operators, etc.
                    if (m.ContainsGenericParameters) continue;
                    if (m.ReturnType != typeof(void)) continue;
                    if (m.GetCustomAttribute<ObsoleteAttribute>() != null) continue;

                    // Optional: skip common Unity MonoBehaviour messages if you don't want them
                    if (LooksLikeUnityMessage(m.Name)) continue;

                    if (extraFilter != null && !extraFilter(m)) continue;

                    var mi = m; // capture
                    var argTypes = mi.GetParameters().Select(p => p.ParameterType).ToArray();

                    results.Add(new InvokableBinding
                    {
                        Name = mi.Name,
                        ArgTypes = argTypes,
                        Invoke = (object[] args) =>
                        {
                            try
                            {
                                args ??= Array.Empty<object>();
                                if (args.Length != argTypes.Length) return;

                                // (Optional) type check/conversion here, but usually you'll pre-parse in UI.
                                mi.Invoke(target, args);
                            }
                            catch { }
                        }
                    });
                }
            }

            return results
                .GroupBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // --------------------
        // Helpers
        // --------------------

        static InvokableBinding MakeUnityEventBinding(string name, UnityEventBase evt, Type eventType)
        {
            var invoke = FindBestInvokeMethod(eventType);

            var argTypes = invoke != null
                ? invoke.GetParameters().Select(p => p.ParameterType).ToArray()
                : Array.Empty<Type>();

            return new InvokableBinding
            {
                Name = name,
                ArgTypes = argTypes,
                Invoke = (object[] args) =>
                {
                    try
                    {
                        args ??= Array.Empty<object>();
                        if (invoke == null) return;
                        if (args.Length != argTypes.Length) return;

                        invoke.Invoke(evt, args);
                    }
                    catch { }
                }
            };
        }

        static MethodInfo FindBestInvokeMethod(Type eventType)
        {
            if (eventType == null) return null;

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Grab all Invoke methods (there can be multiple across base types)
            var invokes = eventType
                .GetMethods(flags)
                .Where(m => m.Name == "Invoke" && !m.IsGenericMethodDefinition)
                .ToList();

            if (invokes.Count == 0) return null;

            bool IsObjectArrayInvoke(MethodInfo m)
            {
                var ps = m.GetParameters();
                return ps.Length == 1 && ps[0].ParameterType == typeof(object[]);
            }

            // Prefer public, non-object[] Invoke, with the most parameters (UnityEvent<T0..T3>)
            var best = invokes
                .Where(m => m.IsPublic && !IsObjectArrayInvoke(m))
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault();

            if (best != null) return best;

            // Next: any visibility, non-object[] Invoke
            best = invokes
                .Where(m => !IsObjectArrayInvoke(m))
                .OrderByDescending(m => m.GetParameters().Length)
                .FirstOrDefault();

            if (best != null) return best;

            // Last resort: whatever exists (probably object[] one)
            return invokes[0];
        }

        static IEnumerable<Type> EnumerateTypeChain(Type t)
        {
            for (var cur = t; cur != null && cur != typeof(object); cur = cur.BaseType)
                yield return cur;
        }

        static bool LooksLikeUnityMessage(string name)
        {
            // You can tune this list. Keeps menus clean.
            switch (name)
            {
                case "Awake":
                case "Start":
                case "Update":
                case "LateUpdate":
                case "FixedUpdate":
                case "OnEnable":
                case "OnDisable":
                case "OnDestroy":
                case "OnGUI":
                    return true;
                default:
                    return false;
            }
        }
    }
}
