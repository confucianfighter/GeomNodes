using UnityEngine;

namespace DLN
{
    public class ShowIfBoolAttribute : PropertyAttribute
    {
        public readonly string controllingBoolFieldName;
        public readonly bool showIf;
        public readonly string overrideLabel;
        public readonly float indentAmount;

        public ShowIfBoolAttribute(
            string controllingBoolFieldName,
            bool showIf = true,
            string overrideLabel = null,
            float indentAmount = 0f)
        {
            this.controllingBoolFieldName = controllingBoolFieldName;
            this.showIf = showIf;
            this.overrideLabel = overrideLabel;
            this.indentAmount = indentAmount;
        }
    }
}