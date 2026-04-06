using System;
using System.Collections.Generic;
using UnityEngine;

namespace DLN
{
    /// <summary>
    /// Recursively searches the transform hierarchy for C3DLS_LayoutOpsList components,
    /// groups them by pass number, and executes them in pass order.
    ///
    public class C3DLS_ExecuteDepthFirst : MonoBehaviour
    {
        [Header("Recursive Execution Meaning")]
        [TextArea(10, 18)]
        [SerializeField]
        private string recursiveMeaning =
            "This component scans its transform subtree for C3DLS_LayoutOpsList components.\n\n" +
            "It collects enabled lists, sorts them by pass number, and executes lower passes before higher passes.\n\n" +
            "Search order is depth-first through the transform hierarchy. Within the same pass, execution order follows discovery order.\n\n" +
            "If an ENABLED child or descendant C3DLS_ExecuteDepthFirst is encountered, that component is executed immediately and this search STOPS on that branch right there.\n\n" +
            "That means a nested recursive executor becomes the local layout authority for its subtree. This helps prefabs encapsulate their own internal layout behavior and prevents double traversal of the same branch.\n\n" +
            "If a nested recursive executor exists but is DISABLED, it does not block traversal. Search continues into that branch as normal.";

        [Header("Search Scope")]
        [SerializeField] private bool includeSelfLists = true;
        [SerializeField] private bool skipDisabledLists = true;
        [SerializeField] private bool executeNestedRecursiveExecutors = true;
        [SerializeField] private bool stopAtNestedRecursiveExecutors = true;

        [Header("Safety")]
        [SerializeField] private bool preventDuplicateNestedRecursiveExecution = true;

        [Header("Deferred Mesh Resize")]
        [Tooltip("If true, DeferredMeshResize helpers in this subtree are cleared before the outermost execution begins.")]
        
        [Header("Debug")]
        [SerializeField] private bool verboseLogging = true;

        private struct CollectedList
        {
            public C3DLS_LayoutOpsList List;
            public int Pass;
            public int DiscoveryOrder;
            public Transform FoundAtTransform;

            public CollectedList(C3DLS_LayoutOpsList list, int pass, int discoveryOrder, Transform foundAtTransform)
            {
                List = list;
                Pass = pass;
                DiscoveryOrder = discoveryOrder;
                FoundAtTransform = foundAtTransform;
            }
        }

        // Top-level shared registry for one full recursive execution run.
        private static int s_executeDepth = 0;

        public static void RegisterDeferredMeshResizeOp(MeshResizeOp op)
        {
            if (op == null)
                return;

            
        }

        public void Log(Func<string> message)
        {
            if (verboseLogging)
                Debug.Log($"Component called {name}, of GameObject named {gameObject.name}: {message()}");
        }

        [ContextMenu("Execute Recursively")]
        public void Execute()
        {
            bool isTopLevel = s_executeDepth == 0;
            s_executeDepth++;

            

            try
            {
                var collected = new List<CollectedList>();
                var invokedNestedRecursive = preventDuplicateNestedRecursiveExecution
                    ? new HashSet<C3DLS_ExecuteDepthFirst>()
                    : null;

                int discoveryOrder = 0;

                Log(() => $"[ExecuteDepthFirst '{name}'] Starting recursive execution from transform '{transform.name}'.");

                TraverseSelfLast(
                    transform,
                    collected,
                    invokedNestedRecursive,
                    ref discoveryOrder,
                    isRoot: true);

                if (collected.Count == 0)
                {
                    Log(() => $"[ExecuteDepthFirst '{name}'] No execute lists found.");
                    return;
                }

                collected.Sort((a, b) =>
                {
                    int passCompare = a.Pass.CompareTo(b.Pass);
                    if (passCompare != 0) return passCompare;
                    return a.DiscoveryOrder.CompareTo(b.DiscoveryOrder);
                });

                int currentPass = -1;
                for (int i = 0; i < collected.Count; i++)
                {
                    CollectedList entry = collected[i];
                    if (entry.List == null)
                        continue;

                    if (entry.Pass != currentPass)
                    {
                        currentPass = entry.Pass;
                        Log(() => $"[ExecuteDepthFirst '{name}'] ---- Starting Pass {currentPass} ----");
                    }

                    if (verboseLogging)
                    {
                        Log(() =>
                            $"[ExecuteDepthFirst '{name}'] Executing list '{entry.List.name}' " +
                            $"found at '{GetTransformPath(entry.FoundAtTransform)}' on pass {entry.Pass}.");
                    }

                    entry.List.ExecuteInternal(
                        logPrefix: $"[ExecuteDepthFirst '{name}' -> List '{entry.List.name}' | Pass {entry.Pass}]");
                }

                Log(() => $"[ExecuteDepthFirst '{name}'] Finished recursive execution.");
            }
            finally
            {
                s_executeDepth--;

                
            }
        }

        private void TraverseSelfLast(
            Transform current,
            List<CollectedList> collected,
            HashSet<C3DLS_ExecuteDepthFirst> invokedNestedRecursive,
            ref int discoveryOrder,
            bool isRoot = false)
        {
            if (current == null)
                return;

            C3DLS_ExecuteDepthFirst nestedRecursive = current.GetComponent<C3DLS_ExecuteDepthFirst>();

            if (!isRoot &&
                nestedRecursive != null &&
                nestedRecursive != this &&
                nestedRecursive.isActiveAndEnabled)
            {
                if (verboseLogging)
                {
                    Log(() =>
                        $"[ExecuteDepthFirst '{name}'] Encountered enabled nested recursive executor " +
                        $"'{nestedRecursive.name}' at '{GetTransformPath(current)}'.");
                }

                if (executeNestedRecursiveExecutors)
                {
                    bool shouldExecuteNested = true;

                    if (invokedNestedRecursive != null && !invokedNestedRecursive.Add(nestedRecursive))
                    {
                        shouldExecuteNested = false;

                        if (verboseLogging)
                        {
                            Log(() =>
                                $"[ExecuteDepthFirst '{name}'] Nested recursive executor '{nestedRecursive.name}' " +
                                $"was already invoked during this run. Skipping duplicate invocation.");
                        }
                    }

                    if (shouldExecuteNested)
                    {
                        if (verboseLogging)
                        {
                            Log(() =>
                                $"[ExecuteDepthFirst '{name}'] Delegating branch execution to nested recursive executor " +
                                $"'{nestedRecursive.name}', then stopping deeper search on this branch.");
                        }

                        nestedRecursive.Execute();
                    }
                }

                if (stopAtNestedRecursiveExecutors)
                {
                    if (verboseLogging)
                    {
                        Log(() =>
                            $"[ExecuteDepthFirst '{name}'] Branch search stopped at nested recursive executor " +
                            $"'{nestedRecursive.name}'.");
                    }

                    return;
                }
            }

            for (int i = 0; i < current.childCount; i++)
            {
                TraverseSelfLast(
                    current.GetChild(i),
                    collected,
                    invokedNestedRecursive,
                    ref discoveryOrder);
            }

            if (current != transform || includeSelfLists)
            {
                CollectListsOnTransform(current, collected, ref discoveryOrder);
            }
        }

        private void CollectListsOnTransform(
            Transform tx,
            List<CollectedList> collected,
            ref int discoveryOrder)
        {
            if (tx == null)
                return;

            C3DLS_LayoutOpsList[] lists = tx.GetComponents<C3DLS_LayoutOpsList>();
            if (lists == null || lists.Length == 0)
                return;

            for (int i = 0; i < lists.Length; i++)
            {
                C3DLS_LayoutOpsList list = lists[i];
                if (list == null)
                    continue;

                if (skipDisabledLists && !list.isActiveAndEnabled)
                {
                    if (verboseLogging)
                    {
                        Log(() =>
                            $"[ExecuteDepthFirst '{name}'] Skipping disabled execute list '{list.name}' " +
                            $"at '{GetTransformPath(tx)}'.");
                    }
                    continue;
                }

                collected.Add(new CollectedList(
                    list,
                    list.WhichPass,
                    discoveryOrder++,
                    tx));

                if (verboseLogging)
                {
                    Log(() =>
                        $"[ExecuteDepthFirst '{name}'] Collected list '{list.name}' " +
                        $"at '{GetTransformPath(tx)}' for pass {list.WhichPass}.");
                }
            }
        }

        private static string GetTransformPath(Transform tx)
        {
            if (tx == null)
                return "(null)";

            var stack = new Stack<string>();
            Transform current = tx;

            while (current != null)
            {
                stack.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", stack);
        }
        
    }
}