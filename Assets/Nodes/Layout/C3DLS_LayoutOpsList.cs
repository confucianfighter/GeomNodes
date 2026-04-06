using System.Collections.Generic;
using UnityEngine;

namespace DLN
{
    /// <summary>
    /// A simple ordered batch of LayoutOps that belongs to a single pass number.
    ///
    /// This component does not search recursively on its own.
    /// It only executes the ops listed in <see cref="ops"/>, in the exact order shown.
    ///
    /// Typical usage:
    /// - Put several LayoutOps on the same GameObject or elsewhere in the hierarchy.
    /// - Drag references into this list in the order you want them executed.
    /// - Assign a pass number.
    /// - Let C3DLS_ExecuteDepthFirst discover and run this list.
    ///
    /// Important:
    /// - This list has ONE pass number only.
    /// - If you want similar behavior on another pass, create another list.
    /// - Null entries are skipped.
    /// - Disabled ops are skipped by default if skipDisabledOps is true.
    /// </summary>
    public class C3DLS_LayoutOpsList : LayoutOp
    {
        [Header("Execution Meaning")]
        [TextArea(6, 12)]
        [SerializeField]
        private string executionMeaning = "All Ops are executed by a list, all lists are executed by a depth first executor.";

        [Header("Pass Settings")]
        [Tooltip(
            "The pass number for this entire list. " +
            "Passes start at 1. Lower pass numbers run before higher pass numbers. " +
            "All ops in this list execute during this one pass only."
        )]
        [SerializeField, Min(1)]
        private int whichPass = 1;

        [Header("Execution Options")]
        [Tooltip(
            "If true, any referenced LayoutOp that is disabled will be skipped. " +
            "If false, the list will still try to execute disabled referenced ops."
        )]
        [SerializeField]
        private bool skipDisabledOps = true;

        [Tooltip(
            "If true, logs a message before and after each op execution. " +
            "Useful while debugging pass order and list contents."
        )]
        [SerializeField]
        private bool verboseLogging = false;

        [Header("Ordered Ops")]
        [Tooltip(
            "The LayoutOps executed by this list, in the exact order shown. " +
            "Drag to reorder.\n\n" +
            "These are explicit references, not automatic child lookups.\n\n" +
            "If the same LayoutOp appears in multiple lists, it may execute multiple times."
        )]
        [SerializeField]
        private List<LayoutOp> ops = new List<LayoutOp>();

        public int WhichPass => Mathf.Max(1, whichPass);
        public bool SkipDisabledOps => skipDisabledOps;
        public IReadOnlyList<LayoutOp> Ops => ops;

        [ContextMenu("Execute This List")]
        public override void Execute()
        {
            ExecuteInternal(logPrefix: $"[LayoutOpsList '{name}' | Pass {WhichPass}]");
        }

        /// <summary>
        /// Executes the list in local order.
        /// This is public so a recursive executor can call it explicitly.
        /// </summary>
        public void ExecuteInternal(string logPrefix = null)
        {
            logPrefix ??= $"[LayoutOpsList '{name}' | Pass {WhichPass}]";

            Log(() => $"{logPrefix} Starting list execution. Op count = {ops.Count}");

            for (int i = 0; i < ops.Count; i++)
            {
                LayoutOp op = ops[i];

                if (op == null)
                {
                    if (verboseLogging)
                        Log(() => $"{logPrefix} Skipping null op at index {i}.");
                    continue;
                }

                if (skipDisabledOps && !op.isActiveAndEnabled)
                {
                    if (verboseLogging)
                        Log(() => $"{logPrefix} Skipping disabled op '{op.name}' at index {i}.");
                    continue;
                }

                if (ReferenceEquals(op, this))
                {
                    Log(() => $"{logPrefix} Skipping self-reference at index {i} to avoid immediate self-execution.");
                    continue;
                }

                if (verboseLogging)
                    Log(() => $"{logPrefix} Executing op index {i}: '{op.name}' ({op.GetType().Name})");

                op.Execute();

                if (verboseLogging)
                    Log(() => $"{logPrefix} Finished op index {i}: '{op.name}'");
            }

            Log(() => $"{logPrefix} Finished list execution.");
        }

        public bool TryAddOp(LayoutOp op)
        {
            if (op == null || op == this)
                return false;

            if (ops.Contains(op))
                return false;

            ops.Add(op);
            return true;
        }

    }
}