using UnityEngine;

namespace DLN
{
    public class ButtonFieldAttribute : PropertyAttribute
    {
        public readonly string methodName;
        public readonly string label;
        public readonly float height;
        public readonly float indentAmount;

        public ButtonFieldAttribute(
            string methodName,
            string label = null,
            float height = 24f,
            float indentAmount = 0f)
        {
            this.methodName = methodName;
            this.label = label;
            this.height = height;
            this.indentAmount = indentAmount;
        }
    }
}