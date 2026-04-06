using UnityEditor;

namespace DLN.Editor
{
    public static class SerializedPropertyPathUtility
    {
        public static SerializedProperty FindNearbyOrAncestorProperty(
            SerializedProperty property,
            string targetFieldName)
        {
            if (property == null || string.IsNullOrEmpty(targetFieldName))
                return null;

            var serializedObject = property.serializedObject;
            string path = property.propertyPath;

            // Start from the container of the current property.
            int lastDot = path.LastIndexOf('.');
            string currentContainerPath = lastDot >= 0 ? path.Substring(0, lastDot) : string.Empty;

            while (true)
            {
                string candidatePath = string.IsNullOrEmpty(currentContainerPath)
                    ? targetFieldName
                    : currentContainerPath + "." + targetFieldName;

                SerializedProperty found = serializedObject.FindProperty(candidatePath);
                if (found != null)
                    return found;

                if (string.IsNullOrEmpty(currentContainerPath))
                    break;

                int nextDotUp = currentContainerPath.LastIndexOf('.');
                if (nextDotUp < 0)
                    currentContainerPath = string.Empty;
                else
                    currentContainerPath = currentContainerPath.Substring(0, nextDotUp);
            }

            return null;
        }
    }
}