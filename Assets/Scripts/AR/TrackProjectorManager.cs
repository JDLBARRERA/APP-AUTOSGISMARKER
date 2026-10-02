using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ARTrackBuilder.Core;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.AR
{
    public class TrackProjectorManager : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private ImageTrackerManager _imageTracker;
        [SerializeField] private TrackDataManager _trackData;

        private GameObject _activeTrackInstance;
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

            // Si ya hay una pista proyectada en el tapete, la destruimos e instanciamos la nueva
            if (_activeTrackInstance != null && _currentTrackedImage != null)
            {
                Destroy(_activeTrackInstance);
                _activeTrackInstance = SpawnTrack(_currentTrackedImage.transform);
            }
        }

        private void HandleMatFound(ARTrackedImage trackedImage)
        {
            _currentTrackedImage = trackedImage;

            if (_activeTrackInstance == null && _currentPrefabToInstantiate != null)
            {
                _activeTrackInstance = SpawnTrack(trackedImage.transform);
            }
            else if (_activeTrackInstance != null)
            {
                _activeTrackInstance.SetActive(true);
            }
        }

        private void HandleMatUpdated(ARTrackedImage trackedImage)
        {
            if (_activeTrackInstance == null) return;

            if (trackedImage.trackingState == TrackingState.Limited)
            {
                _activeTrackInstance.SetActive(false);
            }
            else if (trackedImage.trackingState == TrackingState.Tracking && !_activeTrackInstance.activeSelf)
            {
                _activeTrackInstance.SetActive(true);
            }
        }

        private GameObject SpawnTrack(Transform parent)
        {
            var instance = Instantiate(_currentPrefabToInstantiate, parent);
            instance.transform.localScale = TrackPhysicalSpec.PREFAB_SCALE;
            WarnIfOutsideMat(instance);
            return instance;
        }

        private static void WarnIfOutsideMat(GameObject instance)
        {
            var filters = instance.GetComponentsInChildren<MeshFilter>(true);
            var hasBounds = false;
            var bounds = new Bounds(Vector3.zero, Vector3.zero);

            for (var i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                var meshBounds = mesh.bounds;
                var center = meshBounds.center;
                var extents = meshBounds.extents;
                for (var x = -1; x <= 1; x += 2)
                {
                    for (var y = -1; y <= 1; y += 2)
                    {
                        for (var z = -1; z <= 1; z += 2)
                        {
                            var corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                            var local = instance.transform.InverseTransformPoint(filters[i].transform.TransformPoint(corner));
                            if (!hasBounds)
                            {
                                bounds = new Bounds(local, Vector3.zero);
                                hasBounds = true;
                            }
                            else
                            {
                                bounds.Encapsulate(local);
                            }
                        }
                    }
                }
            }

            if (!hasBounds)
            {
                return;
            }

            var maxSize = TrackPhysicalSpec.TRACK_BOUNDS_MAX_METERS;
            if (bounds.size.x > maxSize || bounds.size.z > maxSize)
            {
                Debug.LogWarning($"[AR] La pista supera el tapete ({bounds.size.x:0.00} x {bounds.size.z:0.00} m). El máximo es {maxSize:0.00} m.");
            }
        }

        private void HandleMatLost(ARTrackedImage trackedImage)
        {
            _currentTrackedImage = null;
            if (_activeTrackInstance != null)
            {
                _activeTrackInstance.SetActive(false);
            }
        }
    }
}
