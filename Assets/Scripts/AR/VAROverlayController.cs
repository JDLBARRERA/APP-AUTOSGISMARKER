using UnityEngine;
using UnityEngine.UI;
using ARTrackBuilder.Core;

namespace ARTrackBuilder.AR
{
    /// <summary>
    /// Muestra el marco visual de 4.3 cm para que los jugadores dicten si el auto tocó la línea.
    /// </summary>
    public class VAROverlayController : MonoBehaviour
    {
        [Header("UI del Árbitro VAR")]
        [SerializeField] private GameObject _varPanel;
        [SerializeField] private Text _varStatusText;
        [SerializeField] private RectTransform _caliperBox; // Cuadro visual de 4.3cm
        [SerializeField] private Text _shotCountText;

        private bool _varActive;

        private void OnEnable()
        {
            if (RaceTurnController.Instance == null)
            {
                return;
            }

            RaceTurnController.Instance.OnShotChanged += UpdateShotCount;
            UpdateShotCount(RaceTurnController.Instance.CurrentShot);
        }

        private void OnDisable()
        {
            if (RaceTurnController.Instance == null)
            {
                return;
            }

            RaceTurnController.Instance.OnShotChanged -= UpdateShotCount;
        }

        /// <summary>
        /// Alterna el panel VAR. Es el método que debe enlazarse al botón, porque no pide parámetros.
        /// </summary>
        public void ToggleVAR()
        {
            ToggleVARMode(!_varActive);
        }

        public void ToggleVARMode(bool active)
        {
            _varActive = active;
            if (_varPanel != null) _varPanel.SetActive(active);
            if (_caliperBox != null) _caliperBox.gameObject.SetActive(active);

            if (active && _varStatusText != null)
            {
                _varStatusText.text = "ÁRBITRO VAR: Alinea el marco con el ancho del carril de gis (4.3 cm) para verificar el toque.";
            }
        }

        private void UpdateShotCount(int shot)
        {
            if (_shotCountText == null)
            {
                return;
            }

            if (RaceTurnController.Instance != null && RaceTurnController.Instance.CurrentTurnLost)
            {
                _shotCountText.text = "TURNO PERDIDO (FOSO)";
                return;
            }

            int limit = RaceTurnController.Instance != null ? RaceTurnController.Instance.ShotLimit : 3;
            string hand = RaceTurnController.Instance != null && RaceTurnController.Instance.MustUseNonDominantHand
                ? " · MANO NO DOMINANTE"
                : string.Empty;
            _shotCountText.text = $"TIRO: {shot} / {limit}{hand}";
        }

        public void DeclareInBounds()
        {
            if (_varStatusText != null) _varStatusText.text = "VEREDICTO VAR: DENTRO DE PISTA. El tiro es válido.";
        }

        public void DeclareOutOfBounds()
        {
            if (_varStatusText != null) _varStatusText.text = "VEREDICTO VAR: FUERA DE GIS. Se aplica penalización.";
            if (RaceTurnController.Instance != null)
            {
                RaceTurnController.Instance.ReportOutOfBounds();
            }
        }
    }
}
