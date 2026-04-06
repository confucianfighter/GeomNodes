using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Events;
namespace DLN
{
    public static class ComponentUtils
    {


        public enum SearchDirection
        {
            Up,
            Down
        }


        public static T GetFirst<T>(
            this Component source,
            SearchDirection searchDirection = SearchDirection.Up,
            bool includeSelf = true)
            where T : Component
        {
            if (source == null)
                return null;

            switch (searchDirection)
            {
                case SearchDirection.Up:
                    return GetFirstUp<T>(source.transform, includeSelf);

                case SearchDirection.Down:
                    return GetFirstDown<T>(source.transform, includeSelf);

                default:
                    return null;
            }
        }

        public static T GetFirst<T>(
            this GameObject source,
            SearchDirection searchDirection = SearchDirection.Up,
            bool includeSelf = true)
            where T : Component
        {
            if (source == null)
                return null;

            return GetFirst<T>(source.transform, searchDirection, includeSelf);
        }

        private static T GetFirstUp<T>(this Transform start, bool includeSelf = true)
            where T : Component
        {
            Transform current = includeSelf ? start : start.parent;

            while (current != null)
            {
                T found = current.GetComponent<T>();
                if (found != null)
                    return found;

                current = current.parent;
            }

            return null;
        }
        public static bool TryGetFirstUp<T>(this Transform tx, out T component, bool includeSelf = true) where T: UnityEngine.Component
        {
            component = tx.GetFirstUp<T>();
            if(component != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static T GetFirstDown<T>(Transform start, bool includeSelf)
            where T : Component
        {
            if (includeSelf)
            {
                T self = start.GetComponent<T>();
                if (self != null)
                    return self;
            }

            for (int i = 0; i < start.childCount; i++)
            {
                T found = GetFirstDown<T>(start.GetChild(i), true);
                if (found != null)
                    return found;
            }

            return null;
        }

        public static bool TryGetComponentInParent<T>(this Component component, out T result) where T : Component
        {
            result = component.GetComponentInParent<T>();
            return result != null;
        }
        public static void GetComponentTypes(UnityEngine.Object target, out string[] componentTypes)
        {
            componentTypes = Array.Empty<string>();

            if (target == null)
                return;

            GameObject go = target.AsGameObject();
            if (go == null)
                return;

            componentTypes = go
                .GetComponents<Component>()
                .Where(c => c != null)
                .Select(c => c.GetType().Name)
                .Distinct()
                .OrderBy(name => name)
                .ToArray();
        }
        public static bool TryGetComponentInChildren<T>(this Component component, out T result) where T : Component
        {
            result = component.GetComponentInChildren<T>();
            return result != null;
        }
        /// <summary>
        /// Finds all *public instance fields* of type Vector3 on the given component instance and prints their names.
        /// </summary>
        public static String PrintFields(Component component, List<BindingFlags> bindingFlagsList)
        {
            StringBuilder sb = new StringBuilder();

            var bindingFlags = bindingFlagsList.Aggregate((a, b) => a | b);


            var type = component.GetType();

            // Public instance fields (includes inherited public fields)
            var fields = type.GetFields(bindingFlags);
            sb.AppendLine($"[{type.Name}] has {fields.Length} fields.");
            foreach (var f in fields)
            {
                sb.AppendLine($"Field name: {f.Name}, Type: {f.FieldType.FullName}");
                String propertyValue;
                try
                {
                    propertyValue = f.GetValue(component)?.ToString() ?? "null";
                }
                catch (Exception e)
                {
                    propertyValue = $"<Error retrieving value: {e.Message}>";
                }
                sb.AppendLine($"Field value: {propertyValue}");
                if (IsUnitySerialized(f))
                {
                    sb.AppendLine($"  • {f.Name} is Unity serialized");
                }
                else
                {
                    sb.AppendLine($"  • {f.Name} is NOT Unity serialized");
                }
                if (f.FieldType == typeof(Vector3))
                {
                    Vector3 vec = (Vector3)f.GetValue(component);
                    sb.AppendLine($"  • Accessing Vector3 values individually: x = {vec.x}, y = {vec.y}, z = {vec.z}");
                }
                if (f.IsPublic)
                {
                    sb.AppendLine($"  • {f.Name} is public");
                }
                else
                {
                    sb.AppendLine($"  • {f.Name} is private or protected");
                }

            }
            return sb.ToString();
        }
        public static bool IsUnitySerialized(FieldInfo field)
        {
            // Public fields are serialized by default
            if (field.IsPublic)
                return true;

            // Private/protected need [SerializeField]
            return field.GetCustomAttribute<SerializeField>() != null;
        }
        public static T AddIfNotExists<T>(this GameObject target) where T : Component
        {
            if (!target.TryGetComponent<T>(out var c))
            {
                c = target.AddComponent<T>();
            }
            return c;
        }
        public static String PrintProperties(Component component, List<BindingFlags> bindingFlagsList)
        {
            StringBuilder sb = new StringBuilder();
            var bindingFlags = bindingFlagsList.Aggregate((a, b) => a | b);

            var type = component.GetType();

            // Public instance fields (includes inherited public fields)


            var properties = type.GetProperties(bindingFlags);
            sb.AppendLine($"[{type.Name}] has {properties.Length} properties.");
            foreach (var p in properties)
            {
                sb.AppendLine($"Property name: {p.Name}, Type: {p.PropertyType.FullName}");
                sb.AppendLine($"  • Accessors: get = {p.CanRead}, set = {p.CanWrite}");
                String propertyValue;
                try
                {
                    propertyValue = p.GetValue(component)?.ToString() ?? "null";
                }
                catch (Exception e)
                {
                    propertyValue = $"  • <Error retrieving value: {e.Message}>";
                }
                sb.AppendLine($"  • Property value: {propertyValue}");
                if (p.PropertyType == typeof(Vector3))
                {
                    sb.AppendLine($"  • p.PropertyType == typeof(Vector3) is True");
                    Vector3 vec = (Vector3)p.GetValue(component);
                    sb.AppendLine($"  • Accessing Vector3 values individually: x = {vec.x}, y = {vec.y}, z = {vec.z}");
                }
                if (p.PropertyType.IsEnum)
                {
                    sb.AppendLine($"  • Found an Enum property:  • {p.Name} (property)");
                    Enum enumVal = (Enum)p.GetValue(component);
                    // get enum options
                    var enumOptions = Enum.GetNames(p.PropertyType);
                    sb.AppendLine($"  • Enum options: {string.Join(", ", enumOptions)}");
                    sb.AppendLine($"  • Enum value: {enumVal.ToString()}");
                }
                if (p.PropertyType == typeof(float))
                {
                    sb.AppendLine($"Found a float property:  • {p.Name} (property)");
                    float val = (float)p.GetValue(component);
                    sb.AppendLine($"  • float value: {val}");
                }
                if (p.CanWrite)
                {
                    sb.AppendLine($"  • {p.Name} is writable");
                }
                else
                {
                    sb.AppendLine($"  • {p.Name} is read-only");
                }
            }
            return sb.ToString();
        }

        internal static List<UnityEvent<T>> GetUnityEventsOfType<T>(Component targetComponent)
        {
            if (targetComponent == null) throw new ArgumentNullException(nameof(targetComponent));

            var type = targetComponent.GetType();
            var unityEventType = typeof(UnityEvent<T>);

            return type
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => unityEventType.IsAssignableFrom(f.FieldType))
                .Select(f => f.GetValue(targetComponent) as UnityEvent<T>)
                .Where(e => e != null)
                .ToList();
        }

        internal static void SubscribeAll<T>(Component targetComponent, UnityAction<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            foreach (var ev in GetUnityEventsOfType<T>(targetComponent))
                ev.AddListener(handler);
        }

        internal static void UnsubscribeAll<T>(Component targetComponent, UnityAction<T> handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            foreach (var ev in GetUnityEventsOfType<T>(targetComponent))
                ev.RemoveListener(handler);
        }
        /// <summary>
        /// Finds the first PUBLIC INSTANCE method on the component that matches:
        /// - exactly one parameter of type T
        /// - return type void (preferred). If allowNonVoid=true, returns first match regardless of return type.
        /// </summary>
        public static MethodInfo GetFirstPublicMethodWithSingleArg<T>(Component target, bool allowNonVoid = false)
        {
            if (target == null) return null;

            var t = target.GetType();
            var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public);

            // Prefer void-return methods (UnityAction<T> compatible)
            var voidMatch = methods.FirstOrDefault(m =>
            {
                if (m.IsSpecialName) return false;              // skip property getters/setters, operators
                if (m.ContainsGenericParameters) return false;  // skip open generics
                var p = m.GetParameters();
                return m.ReturnType == typeof(void)
                    && p.Length == 1
                    && p[0].ParameterType == typeof(T);
            });

            if (voidMatch != null) return voidMatch;
            if (!allowNonVoid) return null;

            // Fallback: accept any return type, still requires single T arg
            return methods.FirstOrDefault(m =>
            {
                if (m.IsSpecialName) return false;
                if (m.ContainsGenericParameters) return false;
                var p = m.GetParameters();
                return p.Length == 1 && p[0].ParameterType == typeof(T);
            });
        }

        /// <summary>
        /// Creates a UnityAction<T> that calls the method on target.
        /// Works for void-return methods. For non-void, you can still invoke it (return ignored).
        /// </summary>
        public static UnityAction<T> CreateUnityAction<T>(Component target, MethodInfo method)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (method == null) throw new ArgumentNullException(nameof(method));

            // Fast path: void-return method can be bound directly.
            if (method.ReturnType == typeof(void))
            {
                // This creates a delegate bound to target instance: (T) => target.Method(T)
                return (UnityAction<T>)Delegate.CreateDelegate(typeof(UnityAction<T>), target, method);
            }

            // Fallback: wrap non-void method and ignore return value
            return (T arg) => method.Invoke(target, new object[] { arg });
        }





    }
}