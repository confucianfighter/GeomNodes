using UnityEngine;
using System;
using System.Collections;
using DLN;

namespace DLN
{
    public static class Coroutines
    {
        public static IEnumerator WaitUntilWithTimeout(
            Func<bool> condition,
            Action onSuccess,
            Action onTimeout = null,
            float timeoutSeconds = -1f
        )
        {
            float startTime = Time.time;

            while (!condition())
            {
                if (timeoutSeconds >= 0f && Time.time - startTime >= timeoutSeconds)
                {
                    onTimeout?.Invoke();
                    yield break;
                }

                yield return null; // next frame
            }

            onSuccess?.Invoke();
        }
    }
}
