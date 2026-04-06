using System;
using UnityEngine;
namespace DLN
{
    public sealed class DestroyNotifier : MonoBehaviour
    {
        public event Action Destroyed;

        private void OnDestroy() => Destroyed?.Invoke();
    }
}