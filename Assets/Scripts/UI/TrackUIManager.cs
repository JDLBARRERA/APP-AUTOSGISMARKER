using UnityEngine;
using UnityEngine.UI;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Gestiona la interfaz principal del usuario: opacidad del holograma y 
    /// el bloqueo del tracking para dibujar sin que tiemble.
    /// </summary>
    public class TrackUIManager : MonoBehaviour
    {
        [Header("Controles UI")]
        [SerializeField] private Slider _opacitySlider;
        [SerializeField] private Button _lockButton;
        [SerializeField] private Text _lockButtonText;

        [Header("Dependencias de AR")]
        [Tooltip("El objeto raíz que contiene la pista proyectada.")]
        [SerializeField] private CanvasGroup _trackCanvasGroup; // Usado si la pista es UI
        [SerializeField] private GameObject _arSession; 

        private bool _isLocked = false;

        private void OnEnable()
        {
            if (_opacitySlider != null)
                _opacitySlider.onValueChanged.AddListener(UpdateTrackOpacity);

            if (_lockButton != null)
                _lockButton.onClick.AddListener(ToggleTrackingLock);
        }

        private void OnDisable()
        {
            if (_opacitySlider != null)
                _opacitySlider.onValueChanged.RemoveListener(UpdateTrackOpacity);

            if (_lockButton != null)
                _lockButton.onClick.RemoveListener(ToggleTrackingLock);
        }

        private void UpdateTrackOpacity(float value)
        {
            // Nota: Para objetos 3D reales, se requiere modificar el canal Alpha del material.
            // Si usamos un Canvas Overlay espacial, esto ajusta la opacidad completa.
            if (_trackCanvasGroup != null)
            {
                _trackCanvasGroup.alpha = value;
            }
        }

        private void ToggleTrackingLock()
        {
            _isLocked = !_isLocked;
            
            // Al pausar la sesión AR, el holograma se "congela" en su última posición física.
            // Ideal para que el niño pueda calcar sin que un mal movimiento de cámara arruine la pista.
            if (_arSession != null)
            {
                _arSession.SetActive(!_isLocked);
            }

            if (_lockButtonText != null)
            {
                _lockButtonText.text = _isLocked ? "DESBLOQUEAR PISTA" : "FIJAR PISTA";
            }
        }
    }
}
