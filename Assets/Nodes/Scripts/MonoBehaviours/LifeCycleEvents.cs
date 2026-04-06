using System;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DLN
{
    [ExecuteAlways]
    public class LifeCycleEvents : MonoBehaviour
    {
        [Flags]
        public enum Hook
        {
            None        = 0,
            Awake       = 1 << 0,
            Start       = 1 << 1,
            Enable      = 1 << 2,
            Disable     = 1 << 3,
            Update      = 1 << 4,
            FixedUpdate = 1 << 5,
            DrawGizmos  = 1 << 6,
            Validate    = 1 << 7, // “inspector changed” signal (handled safely)
        }

        [Header("Which hooks fire in Play Mode")]
        [SerializeField] private Hook playModeHooks =
            Hook.Awake | Hook.Start | Hook.Enable | Hook.Disable | Hook.Update | Hook.FixedUpdate | Hook.DrawGizmos;

        [Header("Which hooks fire in Edit Mode")]
        [SerializeField] private Hook editModeHooks =
            Hook.Enable | Hook.Disable | Hook.DrawGizmos | Hook.Validate;

        [Header("Edit Mode Throttling")]
        [Tooltip("Prevents hot spam in Edit Mode when using Update-like behavior.")]
        [Min(0f)]
        [SerializeField] private float editorMinInterval = 0.1f;

        [Header("Events")]
        [SerializeField] private UnityEvent onAwake;
        [SerializeField] private UnityEvent onStart;
        [SerializeField] private UnityEvent onEnable;
        [SerializeField] private UnityEvent onDisable;
        [SerializeField] private UnityEvent onUpdate;
        [SerializeField] private UnityEvent onFixedUpdate;
        [SerializeField] private UnityEvent onDrawGizmos;
        [SerializeField] private UnityEvent onValidateSafe;

        private double _lastEditorTick;

        private bool ShouldRun(Hook hook)
        {
            if (Application.isPlaying)
                return (playModeHooks & hook) != 0;

            return (editModeHooks & hook) != 0;
        }

        private void Awake()
        {
            if (!ShouldRun(Hook.Awake)) return;
            onAwake?.Invoke();
        }

        private void OnEnable()
        {
            if (!ShouldRun(Hook.Enable)) return;
            onEnable?.Invoke();

#if UNITY_EDITOR
            // Optional: if you ever want a true editor "update loop", hook it here.
            EditorApplication.update -= EditorTick;
            EditorApplication.update += EditorTick;
#endif
        }

        private void Start()
        {
            if (!ShouldRun(Hook.Start)) return;
            onStart?.Invoke();
        }

        private void OnDisable()
        {
            if (!ShouldRun(Hook.Disable)) return;
            onDisable?.Invoke();

#if UNITY_EDITOR
            EditorApplication.update -= EditorTick;
#endif
        }

        private void Update()
        {
            if (!ShouldRun(Hook.Update)) return;

            // In Edit Mode, Unity can call Update *a lot* with ExecuteAlways.
            if (!Application.isPlaying && editorMinInterval > 0f)
            {
                var now = EditorTime();
                if (now - _lastEditorTick < editorMinInterval)
                    return;

                _lastEditorTick = now;
            }

            onUpdate?.Invoke();
        }

        private void FixedUpdate()
        {
            if (!ShouldRun(Hook.FixedUpdate)) return;
            onFixedUpdate?.Invoke();
        }

        private void OnDrawGizmos()
        {
            if (!ShouldRun(Hook.DrawGizmos)) return;
            onDrawGizmos?.Invoke();
        }

        private void OnValidate()
        {
            // Unity recommends: validate/sanitize only; don’t spawn objects or do heavy work here. :contentReference[oaicite:4]{index=4}
            if (!ShouldRun(Hook.Validate)) return;

#if UNITY_EDITOR
            // Defer the callback onto the editor main loop safely.
            EditorApplication.delayCall -= InvokeValidateSafe;
            EditorApplication.delayCall += InvokeValidateSafe;
#endif
        }

#if UNITY_EDITOR
        private void InvokeValidateSafe()
        {
            if (this == null) return; // object might have been destroyed
            if (!ShouldRun(Hook.Validate)) return;
            onValidateSafe?.Invoke();
        }

        private void EditorTick()
        {
            // Only used if you want a predictable edit-mode “heartbeat” (optional).
            // Keeping it empty is fine; you can also route to onUpdate if desired.
        }

        private static double EditorTime() => EditorApplication.timeSinceStartup;
#else
        private static double EditorTime() => Time.realtimeSinceStartupAsDouble;
#endif
    }
}
