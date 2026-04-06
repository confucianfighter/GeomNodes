using System.Collections;
using UnityEngine;

namespace DLN
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class GlowPulse : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Usually 1 for the bevel material slot, 0 for the base slot.")]
        public int materialIndex = 1;

        [Tooltip("Built-in Standard usually uses _Color. URP/HDRP Lit usually uses _BaseColor.")]
        public string colorProperty = "_BaseColor";

        [Header("Pulse")]
        public Color minColor = Color.black;
        public Color maxColor = Color.cyan;
        [Min(0.01f)] public float frequency = 1.5f;
        public bool playOnEnable = true;
        public bool useUnscaledTime = false;

        [Header("Blend")]
        [Tooltip("Optional multiplier on top of the sinusoidal pulse.")]
        [Min(0f)] public float intensity = 1f;

        private Renderer _renderer;
        private MaterialPropertyBlock _mpb;
        private int _colorId;
        private Coroutine _pulseRoutine;

        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _colorId = Shader.PropertyToID(colorProperty);
        }

        private void OnEnable()
        {
            if (playOnEnable)
                StartPulsing();
        }

        private void OnDisable()
        {
            StopPulsing();
        }

        [ContextMenu("Start Pulsing")]
        public void StartPulsing()
        {
            StopPulsing();
            _pulseRoutine = StartCoroutine(PulseRoutine());
        }

        [ContextMenu("Stop Pulsing")]
        public void StopPulsing()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }
        }

        [ContextMenu("Apply Min Color")]
        public void ApplyMinColor()
        {
            ApplyColor(minColor * intensity);
        }

        [ContextMenu("Apply Max Color")]
        public void ApplyMaxColor()
        {
            ApplyColor(maxColor * intensity);
        }

        private IEnumerator PulseRoutine()
        {
            while (true)
            {
                float t = useUnscaledTime ? Time.unscaledTime : Time.time;

                // Sin gives -1..1, remap to 0..1
                float wave = Mathf.Sin(t * frequency * Mathf.PI * 2f);
                float lerp = wave * 0.5f + 0.5f;

                Color color = Color.Lerp(minColor, maxColor, lerp) * intensity;
                ApplyColor(color);

                yield return null;
            }
        }

        private void ApplyColor(Color color)
        {
            if (_renderer == null)
                return;

            _renderer.GetPropertyBlock(_mpb, materialIndex);
            _mpb.SetColor(_colorId, color);
            _renderer.SetPropertyBlock(_mpb, materialIndex);
        }
    }
}