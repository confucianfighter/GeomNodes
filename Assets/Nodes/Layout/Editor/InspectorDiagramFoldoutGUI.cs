using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace DLN.Editor
{
    public static class InspectorDiagramFoldoutGUI
    {
        private const float DefaultBottomPadding = 8f;
        private const float FallbackWidthBias = 54f;

        public static float GetTotalHeight(
            SerializedProperty property,
            string sessionScope,
            bool expanded,
            string helpText,
            string assetPath,
            float bottomPadding = DefaultBottomPadding)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            float height = line; // foldout row

            if (!expanded)
                return height;

            float width = GetLastDrawWidth(property, sessionScope);
            height += spacing;
            height += GetExpandedContentHeight(helpText, assetPath, width, bottomPadding);

            return height;
        }

        public static Rect Draw(
            Rect rect,
            SerializedProperty property,
            string sessionScope,
            ref bool expanded,
            string title,
            string helpText,
            string assetPath,
            float bottomPadding = DefaultBottomPadding)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            SetLastDrawWidth(property, sessionScope, rect.width);

            Rect foldoutRect = new Rect(rect.x, rect.y, rect.width, line);
            expanded = EditorGUI.Foldout(foldoutRect, expanded, title, true);

            rect.y += line;

            if (!expanded)
                return rect;

            rect.y += spacing;

            DrawExpandedContent(
                new Rect(rect.x, rect.y, rect.width, 0f),
                helpText,
                assetPath);

            rect.y += GetExpandedContentHeight(helpText, assetPath, rect.width, bottomPadding);
            return rect;
        }

        public static void DrawExpandedContent(
            Rect rect,
            string helpText,
            string assetPath)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float safeWidth = Mathf.Max(1f, rect.width);

            float textHeight = EditorStyles.wordWrappedLabel.CalcHeight(
                new GUIContent(helpText),
                safeWidth);

            Rect helpRect = new Rect(rect.x, rect.y, safeWidth, textHeight);
            EditorGUI.LabelField(helpRect, helpText, EditorStyles.wordWrappedLabel);

            rect.y += textHeight + spacing;

            Texture2D diagram = LoadDiagram(assetPath);
            if (diagram != null && diagram.width > 0)
            {
                float imageHeight = safeWidth * ((float)diagram.height / diagram.width);
                Rect imageRect = new Rect(rect.x, rect.y, safeWidth, imageHeight);
                GUI.DrawTexture(imageRect, diagram, ScaleMode.ScaleToFit);
            }
            else
            {
                Rect missingRect = new Rect(rect.x, rect.y, safeWidth, line * 2f);
                EditorGUI.HelpBox(missingRect, $"Diagram not found at:\n{assetPath}", MessageType.Info);
            }
        }

        public static float GetExpandedContentHeight(
            string helpText,
            string assetPath,
            float width,
            float bottomPadding = DefaultBottomPadding)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            float safeWidth = Mathf.Max(1f, width);

            float textHeight = EditorStyles.wordWrappedLabel.CalcHeight(
                new GUIContent(helpText),
                safeWidth);

            float height = textHeight + spacing;

            Texture2D diagram = LoadDiagram(assetPath);
            if (diagram != null && diagram.width > 0)
            {
                float imageHeight = safeWidth * ((float)diagram.height / diagram.width);
                height += imageHeight;
            }
            else
            {
                height += line * 2f;
            }

            height += bottomPadding;
            return height;
        }

        private static Texture2D LoadDiagram(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static string GetWidthKey(SerializedProperty property, string sessionScope)
        {
            return $"{sessionScope}.DiagramWidth.{property.serializedObject.targetObject.GetInstanceID()}.{property.propertyPath}";
        }

        private static float GetLastDrawWidth(SerializedProperty property, string sessionScope)
        {
            string key = GetWidthKey(property, sessionScope);
            string fallback = Mathf.Max(1f, EditorGUIUtility.currentViewWidth - FallbackWidthBias)
                .ToString(CultureInfo.InvariantCulture);

            string stored = SessionState.GetString(key, fallback);

            if (float.TryParse(stored, NumberStyles.Float, CultureInfo.InvariantCulture, out float width))
                return Mathf.Max(1f, width);

            return Mathf.Max(1f, EditorGUIUtility.currentViewWidth - FallbackWidthBias);
        }

        private static void SetLastDrawWidth(SerializedProperty property, string sessionScope, float width)
        {
            string key = GetWidthKey(property, sessionScope);
            SessionState.SetString(key, Mathf.Max(1f, width).ToString(CultureInfo.InvariantCulture));
        }
    }
}