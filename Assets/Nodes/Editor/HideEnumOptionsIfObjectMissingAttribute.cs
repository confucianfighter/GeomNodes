using UnityEngine;

namespace DLN
{
    public class HideEnumOptionsIfObjectMissingAttribute : PropertyAttribute
    {
        public readonly string controllingObjectFieldName;
        public readonly string[] hiddenEnumValueNames;
        public readonly string invalidValueLabel;
        public readonly float indentAmount;

        public HideEnumOptionsIfObjectMissingAttribute(
            string controllingObjectFieldName,
            string hiddenEnumValueName,
            string invalidValueLabel = "Missing Required Object",
            float indentAmount = 0f)
        {
            this.controllingObjectFieldName = controllingObjectFieldName;
            this.hiddenEnumValueNames = new[] { hiddenEnumValueName };
            this.invalidValueLabel = invalidValueLabel;
            this.indentAmount = indentAmount;
        }

        public HideEnumOptionsIfObjectMissingAttribute(
            string controllingObjectFieldName,
            string hiddenEnumValueName1,
            string hiddenEnumValueName2,
            string invalidValueLabel = "Missing Required Object",
            float indentAmount = 0f)
        {
            this.controllingObjectFieldName = controllingObjectFieldName;
            this.hiddenEnumValueNames = new[] { hiddenEnumValueName1, hiddenEnumValueName2 };
            this.invalidValueLabel = invalidValueLabel;
            this.indentAmount = indentAmount;
        }

        public HideEnumOptionsIfObjectMissingAttribute(
            string controllingObjectFieldName,
            string hiddenEnumValueName1,
            string hiddenEnumValueName2,
            string hiddenEnumValueName3,
            string invalidValueLabel = "Missing Required Object",
            float indentAmount = 0f)
        {
            this.controllingObjectFieldName = controllingObjectFieldName;
            this.hiddenEnumValueNames = new[] { hiddenEnumValueName1, hiddenEnumValueName2, hiddenEnumValueName3 };
            this.invalidValueLabel = invalidValueLabel;
            this.indentAmount = indentAmount;
        }
    }
}