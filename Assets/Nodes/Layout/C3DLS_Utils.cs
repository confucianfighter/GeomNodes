using UnityEngine;
using System;

namespace DLN
{
    public static class C3DLS_Utils
    {
        public static bool TryExecuteFirstDepthFirstExecutor(Transform t, bool includeSelf = true)
        {
            var result = ComponentUtils.TryGetFirstUp<C3DLS_ExecuteDepthFirst>(t, out var executor, includeSelf: includeSelf);
            executor.Execute();
            return result;
        }
    }
}