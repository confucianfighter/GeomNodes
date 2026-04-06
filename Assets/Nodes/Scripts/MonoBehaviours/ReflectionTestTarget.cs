using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace DLN
{
    public enum ReflectionTestEnum
    {
        OptionA,
        OptionB,
        OptionC
    }
    public class ReflectionTestTarget : MonoBehaviour
    {
        public ReflectionTestEnum testEnumField;
        public string testStringField = "Hello, Reflection!";
        public int testIntField = 42;
        public float testFloatField = 3.14f;
        public bool testBoolField = true;
        public UnityEvent testEventField;

        public ReflectionTestEnum TestEnumProperty { get; set; } = ReflectionTestEnum.OptionB;
        public string TestStringProperty { get; set; } = "Property String";
        public int TestIntProperty { get; set; } = 100;
        public float testFloatProperty { get; set; } = 2.71f;

        public bool testBoolProperty { get; private set; } = false;

        public List<int> testIntList = new List<int> { 1, 2, 3, 4, 5 };
        public List<float> floatList = new List<float> { 1.1f, 2.2f, 3.3f };
        public GameObject testGameObjectField;
        public Title testComponentField;

        public UnityEvent<int> testIntEvent;
        public UnityEvent<string> testStringEvent;
        public UnityEvent<float> testFloatEvent;

        public UnityEvent<ReflectionTestEnum> unityEventWithEnum;

        [SerializeField] private float privateSerializedFloat = 9.81f;

        [SerializeField] float nonPrivateSerializedFloat = 1.618f;

    }
}