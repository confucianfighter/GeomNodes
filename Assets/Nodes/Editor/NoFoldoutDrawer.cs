using UnityEditor;
using UnityEngine;
using DLN;

[CustomPropertyDrawer(typeof(NoFoldoutAttribute))]
public class NoFoldoutDrawer : PropertyDrawer
{
    private const float VerticalSpacing = 2f;
    private const float UnderlineGap = 2f;
    private const float UnderlineThickness = 1f;
    private const float SameLineGap = 6f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (NoFoldoutAttribute)attribute;

        EditorGUI.BeginProperty(position, label, property);

        Rect contentRect = new Rect(
            position.x + attr.indentAmount,
            position.y,
            Mathf.Max(0f, position.width - attr.indentAmount),
            position.height
        );

        string headerText = string.IsNullOrEmpty(attr.overrideLabel) ? label.text : attr.overrideLabel;

        SerializedProperty firstChild = GetFirstDirectChild(property);

        float y = contentRect.y;

        bool firstChildDrawnInline = false;

        switch (attr.headerMode)
        {
            case NoFoldoutHeaderMode.None:
                break;

            case NoFoldoutHeaderMode.Label:
                {
                    Rect labelRect = new Rect(
                        contentRect.x,
                        y,
                        contentRect.width,
                        EditorGUIUtility.singleLineHeight
                    );

                    EditorGUI.LabelField(labelRect, headerText);
                    y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
                    break;
                }

            case NoFoldoutHeaderMode.UnderlinedLabel:
                {
                    Rect labelRect = new Rect(
                        contentRect.x,
                        y,
                        contentRect.width,
                        EditorGUIUtility.singleLineHeight
                    );

                    EditorGUI.LabelField(labelRect, headerText);
                    y += EditorGUIUtility.singleLineHeight + UnderlineGap;

                    Rect lineRect = new Rect(
                        contentRect.x,
                        y,
                        contentRect.width,
                        UnderlineThickness
                    );

                    EditorGUI.DrawRect(lineRect, GetDividerColor());
                    y += UnderlineThickness + VerticalSpacing;
                    break;
                }

            case NoFoldoutHeaderMode.FirstFieldSameLine:
                {
                    if (firstChild != null)
                    {
                        DrawFirstFieldSameLine(contentRect, ref y, headerText, firstChild);
                        firstChildDrawnInline = true;
                    }
                    else
                    {
                        Rect labelRect = new Rect(
                            contentRect.x,
                            y,
                            contentRect.width,
                            EditorGUIUtility.singleLineHeight
                        );

                        EditorGUI.LabelField(labelRect, headerText);
                        y += EditorGUIUtility.singleLineHeight + VerticalSpacing;
                    }
                    break;
                }
        }

        SerializedProperty child = property.Copy();
        SerializedProperty end = property.GetEndProperty();

        bool enterChildren = true;
        bool skippedFirstChild = false;

        while (child.Next(enterChildren) && !SerializedProperty.EqualContents(child, end))
        {
            enterChildren = false;

            if (child.depth != property.depth + 1)
                continue;

            if (firstChildDrawnInline && !skippedFirstChild && firstChild != null &&
                SerializedProperty.EqualContents(child, firstChild))
            {
                skippedFirstChild = true;
                continue;
            }

            float childHeight = EditorGUI.GetPropertyHeight(child, true);

            Rect childRect = new Rect(
                contentRect.x,
                y,
                contentRect.width,
                childHeight
            );

            EditorGUI.PropertyField(childRect, child, true);
            y += childHeight + VerticalSpacing;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var attr = (NoFoldoutAttribute)attribute;

        string headerText = string.IsNullOrEmpty(attr.overrideLabel) ? label.text : attr.overrideLabel;

        SerializedProperty firstChild = GetFirstDirectChild(property);

        float height = 0f;
        bool firstChildDrawnInline = false;

        switch (attr.headerMode)
        {
            case NoFoldoutHeaderMode.None:
                break;

            case NoFoldoutHeaderMode.Label:
                height += EditorGUIUtility.singleLineHeight + VerticalSpacing;
                break;

            case NoFoldoutHeaderMode.UnderlinedLabel:
                height += EditorGUIUtility.singleLineHeight + UnderlineGap + UnderlineThickness + VerticalSpacing;
                break;

            case NoFoldoutHeaderMode.FirstFieldSameLine:
                if (firstChild != null)
                {
                    float firstChildHeight = EditorGUI.GetPropertyHeight(firstChild, true);
                    height += Mathf.Max(EditorGUIUtility.singleLineHeight, firstChildHeight) + VerticalSpacing;
                    firstChildDrawnInline = true;
                }
                else
                {
                    height += EditorGUIUtility.singleLineHeight + VerticalSpacing;
                }
                break;
        }

        SerializedProperty child = property.Copy();
        SerializedProperty end = property.GetEndProperty();

        bool enterChildren = true;
        bool skippedFirstChild = false;

        while (child.Next(enterChildren) && !SerializedProperty.EqualContents(child, end))
        {
            enterChildren = false;

            if (child.depth != property.depth + 1)
                continue;

            if (firstChildDrawnInline && !skippedFirstChild && firstChild != null &&
                SerializedProperty.EqualContents(child, firstChild))
            {
                skippedFirstChild = true;
                continue;
            }

            height += EditorGUI.GetPropertyHeight(child, true) + VerticalSpacing;
        }

        if (height > 0f)
            height -= VerticalSpacing;

        return Mathf.Max(0f, height);
    }

    private void DrawFirstFieldSameLine(Rect contentRect, ref float y, string headerText, SerializedProperty firstChild)
    {
        float lineHeight = Mathf.Max(
            EditorGUIUtility.singleLineHeight,
            EditorGUI.GetPropertyHeight(firstChild, true)
        );

        Rect rowRect = new Rect(
            contentRect.x,
            y,
            contentRect.width,
            lineHeight
        );

        Rect fieldRect = EditorGUI.PrefixLabel(rowRect, new GUIContent(headerText));

        EditorGUI.PropertyField(fieldRect, firstChild, GUIContent.none, true);

        y += lineHeight + VerticalSpacing;
    }

    private SerializedProperty GetFirstDirectChild(SerializedProperty property)
    {
        SerializedProperty child = property.Copy();
        SerializedProperty end = property.GetEndProperty();

        bool enterChildren = true;

        while (child.Next(enterChildren) && !SerializedProperty.EqualContents(child, end))
        {
            enterChildren = false;

            if (child.depth == property.depth + 1)
                return child.Copy();
        }

        return null;
    }

    private Color GetDividerColor()
    {
        return EditorGUIUtility.isProSkin
            ? new Color(0.35f, 0.35f, 0.35f, 1f)
            : new Color(0.65f, 0.65f, 0.65f, 1f);
    }
}