using UnityEngine;
using UnityEngine.XR.ARFoundation;
using ARTrackBuilder.AR;

namespace ARTrackBuilder.Core
{
    public enum DisplayMode { MobileAR, TableProjector }

    /// <summary>
    /// Alterna entre la cámara de AR Foundation y una cámara de proyector con fondo negro.
    /// No destruye la pista ni toca el turno de la carrera.
    /// </summary>
    public class DisplayModeManager : MonoBehaviour
    {
        public static DisplayModeManager Instance { get; private set; }

        [Header("Modo Actual")]
        [SerializeField] private DisplayMode _currentMode = DisplayMode.MobileAR;

        [Header("Referencias de Cámara y AR")]
        [SerializeField] private GameObject _xrOriginObject; // XR Origin con AR Camera
        [SerializeField] private Camera _projectorCamera;   // Cámara Ortográfica para Proyector
        [SerializeField] private ARSession _arSession;
        [SerializeField] private TrackProjectorManager _projector;

        [Header("Pista e Interfaz")]
        [SerializeField] private GameObject _keystoneCalibrationPanel;

        private Transform _storedTrackParent;

        public DisplayMode CurrentMode => _currentMode;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            SetDisplayMode(_currentMode);
        }

        public void SetDisplayMode(DisplayMode mode)
        {
            _currentMode = mode;

            if (mode == DisplayMode.MobileAR)
            {
                if (_arSession != null) _arSession.gameObject.SetActive(true);
                if (_xrOriginObject != null) _xrOriginObject.SetActive(true);
                if (_projectorCamera != null) _projectorCamera.gameObject.SetActive(false);
                if (_keystoneCalibrationPanel != null) _keystoneCalibrationPanel.SetActive(false);
                AttachTrackToAr();

                Debug.Log("<color=cyan>[MODO] Activado: REALIDAD AUMENTADA MÓVIL (AR Foundation)</color>");
            }
            else if (mode == DisplayMode.TableProjector)
            {
                DetachTrackFromAr();

                if (_arSession != null) _arSession.gameObject.SetActive(false);
                if (_xrOriginObject != null) _xrOriginObject.SetActive(false);

                if (_projectorCamera != null)
                {
                    _projectorCamera.gameObject.SetActive(true);
                    _projectorCamera.orthographic = true;
                    _projectorCamera.clearFlags = CameraClearFlags.SolidColor;
                    _projectorCamera.backgroundColor = Color.black; // Negro = Luz apagada en proyector
                }

                if (_keystoneCalibrationPanel != null) _keystoneCalibrationPanel.SetActive(true);

                Debug.Log("<color=green>[MODO] Activado: PROYECTOR DE MESA (Fondo Negro Absoluto)</color>");
            }
        }

        public void ToggleMode()
        {
            SetDisplayMode(_currentMode == DisplayMode.MobileAR ? DisplayMode.TableProjector : DisplayMode.MobileAR);
        }

        private void DetachTrackFromAr()
        {
            if (_projector == null || _projector.ActiveTrackInstance == null)
            {
                return;
            }

            Transform track = _projector.ActiveTrackInstance.transform;
            if (track.parent != null)
            {
                _storedTrackParent = track.parent;
                track.SetParent(null, true);
            }

            track.gameObject.SetActive(true);
        }

        private void AttachTrackToAr()
        {
            if (_projector == null || _projector.ActiveTrackInstance == null || _storedTrackParent == null)
            {
                return;
            }

            Transform track = _projector.ActiveTrackInstance.transform;
            track.SetParent(_storedTrackParent, false);
            track.localPosition = Vector3.zero;
            track.localRotation = Quaternion.identity;
            track.localScale = Vector3.one;
            track.gameObject.SetActive(true);
            _storedTrackParent = null;
        }
    }
}
