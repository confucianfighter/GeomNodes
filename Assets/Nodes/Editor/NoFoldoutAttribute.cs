using UnityEngine;

namespace DLN
{
    public class NoFoldoutAttribute : PropertyAttribute
    {
        public readonly NoFoldoutHeaderMode headerMode;
        public readonly string overrideLabel;
        public readonly float indentAmount;

        public NoFoldoutAttribute(
            NoFoldoutHeaderMode headerMode = NoFoldoutHeaderMode.Label,
            string overrideLabel = null,
            float indentAmount = 0f)
        {
            this.headerMode = headerMode;
            this.overrideLabel = overrideLabel;
            this.indentAmount = indentAmount;
        }
    }
    public enum NoFoldoutHeaderMode
    {
        None,
        Label,
        UnderlinedLabel,
        FirstFieldSameLine,
    }
}