using UnityEngine;

namespace DLN
{
    public class CheckboxLeftAttribute : PropertyAttribute
    {
        public readonly string overrideLabel;
        public readonly float indentAmount;

        public CheckboxLeftAttribute(string overrideLabel = null, float indentAmount = 0f)
        {
            this.overrideLabel = overrideLabel;
            this.indentAmount = indentAmount;
        }
    }
}