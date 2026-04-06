using UnityEngine;
using System.Collections.Generic;

namespace DLN
{

    public class C3DLS_Spawner : LayoutOp
    {

        [SerializeField] private List<GameObject> prefabs = new List<GameObject>();
        [SerializeField] private int numInstances = 1;

        public override void Execute()
        {
            for (int i = 0; i < numInstances; i++)
            {
                foreach (GameObject prefab in prefabs)
                {
                    Instantiate(prefab, transform);
                }
            }
        }
    }
}