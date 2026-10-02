using System.Collections;
using UnityEngine;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Crea un efecto de latido (pulso) en el canal de emisión del material.
    /// Ideal para que la pista destaque sobre el asfalto bajo la luz del sol.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class NeonPulseEffect : MonoBehaviour
    {
        [Header("Configuración del Neón")]
        [ColorUsage(true, true)] // Habilita HDR para el brillo
        [SerializeField] private Color _baseColor = Color.green;
        [SerializeField] private float _pulseSpeed = 2f;
        [SerializeField] private float _minIntensity = 1.5f;
        [SerializeField] private float _maxIntensity = 3.0f;

        private Material _material;
        private Coroutine _pulseCoroutine;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            // Instanciamos el material para no afectar a otros objetos
            _material = GetComponent<Renderer>().material;
            _material.EnableKeyword("_EMISSION");
        }

        private void OnEnable()
        {
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }

        private void OnDisable()
        {
            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
            }
            // Apagar la emisión al desactivar
            _material.SetColor(EmissionColor, Color.black);
        }

        private IEnumerator PulseRoutine()
        {
            while (true)
            {
                // Usamos Mathf.PingPong para crear un ciclo suave entre los valores
                float intensity = Mathf.Lerp(_minIntensity, _maxIntensity, 
                    Mathf.PingPong(Time.time * _pulseSpeed, 1f));
                
                _material.SetColor(EmissionColor, _baseColor * intensity);
                
                yield return null; // Espera al siguiente frame (Mejor que usar Update)
            }
        }
    }
}
