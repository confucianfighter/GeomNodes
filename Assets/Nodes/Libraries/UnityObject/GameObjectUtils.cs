using UnityEngine;
using System.Linq;
using DLN;
using System.Collections.Generic;

namespace DLN
{
    public static class GameObjectUtils
    {
        public static void DestroyGameObjects(object gameObjects, SequenceMode mode = SequenceMode.RepeatLast)
        {
            foreach (var go in SequenceUtils.AsSequence(gameObjects))
            {
                if (go is GameObject gameObject)
                {
                    GameObject.Destroy(gameObject);
                }
            }
            // if it's a list, clear it
            if (gameObjects is IList<object> list)
            {
                list.Clear();
            }
        }
        public static void DestroyAllChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }
        public static List<GameObject> GetAllChildren(Transform transform){
            var result = new List<GameObject>();
            foreach(Transform child in transform){
                result.Add(child.gameObject);
            }
            return result;
        }
    }
}