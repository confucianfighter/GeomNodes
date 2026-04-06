using System.Globalization;
using UnityEditor;

namespace DLN.Editor
{
    public static class PropertySessionState
    {
        public static string GetKey(SerializedProperty property, string scope, string name)
        {
            return $"{scope}.{name}.{property.serializedObject.targetObject.GetInstanceID()}.{property.propertyPath}";
        }

        public static bool GetBool(SerializedProperty property, string scope, string name, bool fallback)
        {
            return SessionState.GetBool(GetKey(property, scope, name), fallback);
        }

        public static void SetBool(SerializedProperty property, string scope, string name, bool value)
        {
            SessionState.SetBool(GetKey(property, scope, name), value);
        }

        public static float GetFloat(SerializedProperty property, string scope, string name, float fallback)
        {
            string key = GetKey(property, scope, name);
            string stored = SessionState.GetString(key, fallback.ToString(CultureInfo.InvariantCulture));

            if (float.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                return result;

            return fallback;
        }

        public static void SetFloat(SerializedProperty property, string scope, string name, float value)
        {
            string key = GetKey(property, scope, name);
            SessionState.SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }
    }
}