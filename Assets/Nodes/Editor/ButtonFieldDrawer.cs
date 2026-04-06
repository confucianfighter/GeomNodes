using System.Reflection;
using UnityEditor;
using UnityEngine;
using DLN;

[CustomPropertyDrawer(typeof(ButtonFieldAttribute))]
public class ButtonFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (ButtonFieldAttribute)attribute;

        Rect buttonRect = new Rect(
            position.x + attr.indentAmount,
            position.y,
            Mathf.Max(0f, position.width - attr.indentAmount),
            attr.height
        );

        string buttonLabel = string.IsNullOrEmpty(attr.label)
            ? label.text
            : attr.label;

        if (GUI.Button(buttonRect, buttonLabel))
        {
            InvokeMethod(property, attr.methodName);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (ButtonFieldAttribute)attribute;
        return attr.height;
    }

    private void InvokeMethod(SerializedProperty property, string methodName)
    {
        if (property == null || string.IsNullOrEmpty(methodName))
            return;

        Object targetObject = property.serializedObject.targetObject;
        if (targetObject == null)
            return;

        object callTarget = GetCallTargetObject(property) ?? targetObject;

        MethodInfo method = callTarget.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (method == null)
        {
            Debug.LogWarning($"ButtonField could not find method '{methodName}' on '{callTarget.GetType().Name}'.");
            return;
        }

        if (method.GetParameters().Length != 0)
        {
            Debug.LogWarning($"ButtonField method '{methodName}' must have no parameters.");
            return;
        }

        method.Invoke(callTarget, null);

        property.serializedObject.Update();

        if (!Application.isPlaying)
            EditorUtility.SetDirty(targetObject);
    }

    private object GetCallTargetObject(SerializedProperty property)
    {
        if (property == null)
            return null;

        object obj = property.serializedObject.targetObject;
        if (obj == null)
            return null;

        string path = property.propertyPath.Replace(".Array.data[", "[");
        string[] elements = path.Split('.');

        for (int i = 0; i < elements.Length - 1; i++)
        {
            string element = elements[i];

            if (element.Contains("["))
            {
                string elementName = element.Substring(0, element.IndexOf("["));
                int index = int.Parse(element.Substring(element.IndexOf("[") + 1).Replace("]", ""));

                obj = GetMemberValue(obj, elementName, index);
            }
            else
            {
                obj = GetMemberValue(obj, element);
            }

            if (obj == null)
                return null;
        }

        return obj;
    }

    private object GetMemberValue(object source, string name)
    {
        if (source == null)
            return null;

        var type = source.GetType();

        FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
            return field.GetValue(source);

        PropertyInfo prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null)
            return prop.GetValue(source);

        return null;
    }

    private object GetMemberValue(object source, string name, int index)
    {
        object enumerable = GetMemberValue(source, name);
        if (enumerable is System.Collections.IEnumerable seq)
        {
            var en = seq.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!en.MoveNext())
                    return null;
            }
            return en.Current;
        }

        return null;
    }
}