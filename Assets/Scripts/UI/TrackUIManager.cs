using UnityEngine;
using UnityEngine.UI;
using ARTrackBuilder.AR;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Ajusta la emisión de la pista instanciada y la fija en coordenadas de mundo sin apagar la sesión AR.
    /// </summary>
    public class TrackUIManager : MonoBehaviour
    {
        [Header("Dependencias de AR")]
        [SerializeField] private TrackProjectorManager _projectorManager;

        [Header("Controles UI")]
        [SerializeField] private Slider _opacitySlider;
        [SerializeField] private Button _lockButton;
        [SerializeField] private Text _lockButtonText;

        private Transform _originalParent;
        private bool _isLocked = false;
        private GameObject _opacityTarget;
        private Color _baseColor;
        private Color _baseEmission;
        private bool _hasEmission;
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void OnEnable()
        {
            if (_opacitySlider != null)
                _opacitySlider.onValueChanged.AddListener(UpdateOpacity);

            if (_lockButton != null)
                _lockButton.onClick.AddListener(ToggleLock);
        }

        private void OnDisable()
        {
            if (_opacitySlider != null)
                _opacitySlider.onValueChanged.RemoveListener(UpdateOpacity);

            if (_lockButton != null)
                _lockButton.onClick.RemoveListener(ToggleLock);
        }

        private void UpdateOpacity(float value)
        {
            if (_projectorManager == null || _projectorManager.ActiveTrackInstance == null) return;

            // Modificar la transparencia / emisión directa del Renderer instanciado
            Renderer renderer = _projectorManager.ActiveTrackInstance.GetComponent<Renderer>();
            if (renderer == null || renderer.material == null) return;

            CacheBaseColors(renderer);

            Color color = _baseColor;
            color.a = value;
            renderer.material.color = color;

            if (_hasEmission)
            {
                renderer.material.SetColor(EmissionColor, _baseEmission * value);
            }
        }

        private void CacheBaseColors(Renderer renderer)
        {
            if (_opacityTarget == _projectorManager.ActiveTrackInstance)
            {
                return;
            }

            _opacityTarget = _projectorManager.ActiveTrackInstance;
            _baseColor = renderer.material.color;
            _hasEmission = renderer.material.HasProperty(EmissionColor);
            if (_hasEmission)
            {
                _baseEmission = renderer.material.GetColor(EmissionColor);
            }
        }

        private void ToggleLock()
        {
            if (_projectorManager == null || _projectorManager.ActiveTrackInstance == null) return;

            GameObject trackInstance = _projectorManager.ActiveTrackInstance;
            _isLocked = !_isLocked;

            if (_isLocked)
            {
                // Desconectar del Image Target y congelar en coordenadas de mundo
                _originalParent = trackInstance.transform.parent;
                trackInstance.transform.SetParent(null, true);
            }
            else
            {
                // Reconectar al marcador detectado
                if (_originalParent != null)
                {
                    trackInstance.transform.SetParent(_originalParent, false);
                    trackInstance.transform.localPosition = Vector3.zero;
                    trackInstance.transform.localRotation = Quaternion.identity;
                }
            }

            if (_lockButtonText != null)
            {
                _lockButtonText.text = _isLocked ? "DESBLOQUEAR PISTA" : "FIJAR PISTA";
            }
        }
    }
}
