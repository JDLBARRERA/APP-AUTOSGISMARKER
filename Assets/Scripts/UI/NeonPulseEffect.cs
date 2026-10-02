using System.Collections;
using UnityEngine;

namespace ARTrackBuilder.UI
{
    [RequireComponent(typeof(Renderer))]
    public class NeonPulseEffect : MonoBehaviour
    {
        [Header("Configuración del Neón")]
        [ColorUsage(true, true)]
        [SerializeField] private Color _baseColor = Color.cyan;
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _minIntensity = 1.5f;
        [SerializeField] private float _maxIntensity = 3.0f;

        private Material _material;
        private Coroutine _pulseCoroutine;
        private float _masterOpacity = 1f;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void OnEnable()
        {
            BindGeneratedMaterial();
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        private void BindGeneratedMaterial()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer == null)
            {
                return;
            }

            _material = renderer.material;
            _material.EnableKeyword("_EMISSION");
        }

        private void OnDisable()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
            }
            if (_material != null && _material.HasProperty(EmissionColor))
            {
                _material.SetColor(EmissionColor, Color.black);
            }
        }

        public void SetMasterOpacity(float opacity)
        {
            _masterOpacity = Mathf.Clamp01(opacity);
        }

        private IEnumerator PulseRoutine()
        {
            while (true)
            {
                float intensity = Mathf.Lerp(_minIntensity, _maxIntensity,
                    Mathf.PingPong(Time.time * _pulseSpeed, 1f));

                if (_material == null || !_material.HasProperty(EmissionColor))
                {
                    yield return null;
                    continue;
                }

                // Escalar emisión por opacidad maestra
                Color finalColor = _baseColor * (intensity * _masterOpacity);
                _material.SetColor(EmissionColor, finalColor);

                yield return null;
            }
        }
    }
}
