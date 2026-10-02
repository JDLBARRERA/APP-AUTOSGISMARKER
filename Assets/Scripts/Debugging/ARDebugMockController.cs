using UnityEngine;
using ARTrackBuilder.AR;
using ARTrackBuilder.Core;

namespace ARTrackBuilder.Debugging
{
    /// <summary>
    /// Permite probar todo el flujo del juego en el Editor de Unity mediante teclado
    /// o botones de la interfaz sin necesidad de compilar a teléfono físico.
    /// </summary>
    public class ARDebugMockController : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private TrackProjectorManager _projectorManager;
        [SerializeField] private VAROverlayController _varController;

        [Header("Atajos de Teclado (Editor)")]
        [Tooltip("Presiona 'M' para simular que la cámara encontró o perdió el tapete.")]
        [SerializeField] private KeyCode _toggleMatKey = KeyCode.M;

        [Tooltip("Presiona 'ESPACIO' para simular que el jugador realizó un tiro con el índice.")]
        [SerializeField] private KeyCode _registerShotKey = KeyCode.Space;

        [Tooltip("Presiona 'O' para simular que el auto se salió de la línea de gis.")]
        [SerializeField] private KeyCode _outOfBoundsKey = KeyCode.O;

        [Tooltip("Presiona 'V' para abrir o cerrar el retículo de precisión del VAR.")]
        [SerializeField] private KeyCode _toggleVarKey = KeyCode.V;

        [Tooltip("Presiona 'F' para simular que el auto cayó en un foso.")]
        [SerializeField] private KeyCode _pitKey = KeyCode.F;

        private bool _isMatSimulatedFound = false;
        private GameObject _mockTrackedImageAnchor;

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(_toggleMatKey))
            {
                ToggleSimulatedMat();
            }

            if (Input.GetKeyDown(_registerShotKey))
            {
                SimulateShot();
            }

            if (Input.GetKeyDown(_outOfBoundsKey))
            {
                SimulateOutOfBounds();
            }

            if (Input.GetKeyDown(_toggleVarKey))
            {
                ToggleVAR();
            }

            if (Input.GetKeyDown(_pitKey))
            {
                SimulatePit();
            }
        }
#endif

        public void ToggleSimulatedMat()
        {
            _isMatSimulatedFound = !_isMatSimulatedFound;

            if (_isMatSimulatedFound)
            {
                Debug.Log("<color=cyan>[MOCK AR] Tapete DETECTADO. Generando anclaje virtual...</color>");
                if (_mockTrackedImageAnchor == null)
                {
                    _mockTrackedImageAnchor = new GameObject("MOCK_AR_TrackedImage_Anchor");
                    _mockTrackedImageAnchor.transform.position = Vector3.zero;
                }

                if (_projectorManager != null && _projectorManager.ActiveTrackInstance == null)
                {
                    NeonTrackGenerator generator = _mockTrackedImageAnchor.GetComponent<NeonTrackGenerator>();
                    if (generator == null)
                    {
                        generator = _mockTrackedImageAnchor.AddComponent<NeonTrackGenerator>();
                    }

                    generator.BuildTrackMeshAndMeta();
                    _projectorManager.RegisterSimulatedTrack(_mockTrackedImageAnchor);
                }
                else if (_projectorManager != null && _projectorManager.ActiveTrackInstance != null)
                {
                    _projectorManager.ActiveTrackInstance.SetActive(true);
                }
            }
            else
            {
                Debug.Log("<color=yellow>[MOCK AR] Tapete PERDIDO. Ocultando holograma...</color>");
                if (_projectorManager != null && _projectorManager.ActiveTrackInstance != null)
                {
                    _projectorManager.ActiveTrackInstance.SetActive(false);
                }
            }
        }

        public void SimulateShot()
        {
            if (RaceTurnController.Instance != null)
            {
                RaceTurnController.Instance.RegisterShot();
                Debug.Log($"<color=green>[MOCK JUEGO] Tiro registrado. Tiro actual: {RaceTurnController.Instance.CurrentShot} / 3</color>");
            }
            else
            {
                Debug.LogWarning("[MOCK JUEGO] No existe RaceTurnController en la escena.");
            }
        }

        public void SimulateOutOfBounds()
        {
            if (RaceTurnController.Instance != null)
            {
                RaceTurnController.Instance.ReportOutOfBounds();
                Debug.Log("<color=red>[MOCK REGLAS] ¡Auto fuera de la línea de gis! Se aplica regreso al origen.</color>");
            }
        }

        public void SimulatePit()
        {
            if (RaceTurnController.Instance != null)
            {
                RaceTurnController.Instance.ApplyPit();
                Debug.Log("<color=red>[MOCK REGLAS] Auto en el foso. Pierde el turno siguiente.</color>");
            }
        }

        public void ToggleVAR()
        {
            if (_varController != null)
            {
                _varController.ToggleVAR();
                Debug.Log("[MOCK VAR] Modo Árbitro VAR alternado.");
            }
        }
    }
}
