using UnityEngine;

namespace DLN
{
    public class ShowIfObjectAssignedAttribute : PropertyAttribute
    {
        public readonly string controllingObjectFieldName;
        public readonly string overrideLabel;
        public readonly float indentAmount;

        public ShowIfObjectAssignedAttribute(
            string controllingObjectFieldName,
            string overrideLabel = null,
            float indentAmount = 0f)
        {
            this.controllingObjectFieldName = controllingObjectFieldName;
            this.overrideLabel = overrideLabel;
            this.indentAmount = indentAmount;
        }
    }
}