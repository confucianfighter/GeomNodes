using UnityEngine;

namespace DLN
{
    public class ShowIfEnumAttribute : PropertyAttribute
    {
        public readonly string controllingEnumFieldName;
        public readonly string[] requiredEnumValueNames;
        public readonly string overrideLabel;
        public readonly float indentAmount;

        public ShowIfEnumAttribute(
            string controllingEnumFieldName,
            string requiredEnumValueName,
            string overrideLabel = null,
            float indentAmount = 0f)
        {
            this.controllingEnumFieldName = controllingEnumFieldName;
            this.requiredEnumValueNames = new[] { requiredEnumValueName };
            this.overrideLabel = overrideLabel;
            this.indentAmount = indentAmount;
        }

        public ShowIfEnumAttribute(
            string controllingEnumFieldName,
            string requiredEnumValueName1,
            string requiredEnumValueName2,
            string overrideLabel = null,
            float indentAmount = 0f)
        {
            this.controllingEnumFieldName = controllingEnumFieldName;
            this.requiredEnumValueNames = new[] { requiredEnumValueName1, requiredEnumValueName2 };
            this.overrideLabel = overrideLabel;
            this.indentAmount = indentAmount;
        }

        public ShowIfEnumAttribute(
            string controllingEnumFieldName,
            string requiredEnumValueName1,
            string requiredEnumValueName2,
            string requiredEnumValueName3,
            string overrideLabel = null,
            float indentAmount = 0f)
        {
            this.controllingEnumFieldName = controllingEnumFieldName;
            this.requiredEnumValueNames = new[] { requiredEnumValueName1, requiredEnumValueName2, requiredEnumValueName3 };
            this.overrideLabel = overrideLabel;
            this.indentAmount = indentAmount;
        }
    }
}