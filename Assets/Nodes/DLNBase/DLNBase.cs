using UnityEngine;
using System;

namespace DLN
{
    public class DLNBase: MonoBehaviour
    {
        [SerializeField] private bool debug = false;

        public void Log(Func<String> str)
        {
            if(debug) Debug.Log($"{this.name} {str()}");
        }
    }
}