using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.AR
{
    /// <summary>
    /// Instancia la pista sobre el tapete rastreado y publica la instancia y el nombre activos.
    /// </summary>
    public class TrackProjectorManager : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private ImageTrackerManager _imageTracker;
        [SerializeField] private TrackDataManager _trackData;

        public GameObject ActiveTrackInstance { get; private set; }
        public string ActiveTrackName { get; private set; } = "Circuito Neón Alfa";

        private GameObject _currentPrefabToInstantiate;
        private ARTrackedImage _currentTrackedImage;

        private void OnEnable()
        {
            if (_imageTracker != null)
            {
                _imageTracker.OnMatFound += HandleMatFound;
                _imageTracker.OnMatLost += HandleMatLost;
                _imageTracker.OnMatUpdated += HandleMatUpdated;
            }

            if (_trackData != null)
            {
                _trackData.OnTrackChanged += HandleTrackChange;
            }
        }

        private void OnDisable()
        {
            if (_imageTracker != null)
            {
                _imageTracker.OnMatFound -= HandleMatFound;
                _imageTracker.OnMatLost -= HandleMatLost;
                _imageTracker.OnMatUpdated -= HandleMatUpdated;
            }

            if (_trackData != null)
            {
                _trackData.OnTrackChanged -= HandleTrackChange;
            }
        }

        private void HandleTrackChange(GameObject newTrackPrefab)
        {
            _currentPrefabToInstantiate = newTrackPrefab;
            if (newTrackPrefab != null) ActiveTrackName = newTrackPrefab.name;

            if (ActiveTrackInstance != null)
            {
                Destroy(ActiveTrackInstance);
                ActiveTrackInstance = null;
            }

            if (_currentTrackedImage != null && _currentTrackedImage.trackingState == TrackingState.Tracking)
            {
                InstantiateTrack();
            }
        }

        private void HandleMatFound(ARTrackedImage trackedImage)
        {
            _currentTrackedImage = trackedImage;
            if (ActiveTrackInstance == null)
            {
                InstantiateTrack();
            }
            else
            {
                ActiveTrackInstance.SetActive(true);
            }
        }

        private void HandleMatUpdated(ARTrackedImage trackedImage)
        {
            _currentTrackedImage = trackedImage;
            if (ActiveTrackInstance == null && trackedImage.trackingState == TrackingState.Tracking)
            {
                InstantiateTrack();
                return;
            }

            if (ActiveTrackInstance != null)
            {
                ActiveTrackInstance.SetActive(trackedImage.trackingState == TrackingState.Tracking);
            }
        }

        private void HandleMatLost(ARTrackedImage trackedImage)
        {
            _currentTrackedImage = null;
            if (ActiveTrackInstance != null)
            {
                ActiveTrackInstance.SetActive(false);
            }
        }

        /// <summary>
        /// Publica la pista creada por el simulador local para que la opacidad y el bloqueo actúen sobre ella.
        /// </summary>
        public void RegisterSimulatedTrack(GameObject track)
        {
            ActiveTrackInstance = track;
            if (track != null)
            {
                ActiveTrackName = track.name;
            }
        }

        /// <summary>
        /// Instancia la pista en el origen del mundo, sin imagen rastreada. Lo llama el botón de prueba del Canvas.
        /// </summary>
        public void SimulateMatFound()
        {
            if (_currentPrefabToInstantiate == null)
            {
                Debug.LogWarning("[AR] No hay un prefab de pista. Asigna Prefab_NeonTrack_01 en TrackDataManager.");
                return;
            }

            if (ActiveTrackInstance == null)
            {
                ActiveTrackInstance = Instantiate(_currentPrefabToInstantiate);
                ActiveTrackInstance.transform.position = Vector3.zero;
                ActiveTrackInstance.transform.rotation = Quaternion.identity;
            }

            ActiveTrackInstance.SetActive(true);
        }

        /// <summary>
        /// Oculta la pista de prueba. Lo llama el botón de tapete perdido.
        /// </summary>
        public void SimulateMatLost()
        {
            if (ActiveTrackInstance != null)
            {
                ActiveTrackInstance.SetActive(false);
            }
        }

        private void InstantiateTrack()
        {
            if (_currentPrefabToInstantiate != null && _currentTrackedImage != null)
            {
                ActiveTrackInstance = Instantiate(_currentPrefabToInstantiate, _currentTrackedImage.transform);
                ActiveTrackInstance.transform.localPosition = Vector3.zero;
                ActiveTrackInstance.transform.localRotation = Quaternion.identity;
                ActiveTrackInstance.SetActive(true);
            }
        }
    }
}
