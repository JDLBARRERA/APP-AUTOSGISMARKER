using System;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace ARTrackBuilder.AR
{
    /// <summary>
    /// Gestiona la detección del tapete físico usando AR Foundation.
    /// Utiliza el patrón Observer (Eventos) para notificar al resto de la app.
    /// </summary>
    [RequireComponent(typeof(ARTrackedImageManager))]
    public class ImageTrackerManager : MonoBehaviour
    {
        private ARTrackedImageManager _trackedImageManager;
        private TrackingState _lastTrackingState = TrackingState.None;

        // Eventos públicos para no acoplar el código (Regla SOLID de nuestro .cursorrules)
        public event Action<ARTrackedImage> OnMatFound;
        public event Action<ARTrackedImage> OnMatUpdated;
        public event Action<ARTrackedImage> OnMatLost;

        private void Awake()
        {
            // Cacheamos la referencia desde el inicio (Regla de rendimiento)
            _trackedImageManager = GetComponent<ARTrackedImageManager>();
        }

        private void OnEnable()
        {
            // Nos suscribimos a los eventos de AR Foundation
            _trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        private void OnDisable()
        {
            // Nos desuscribimos para evitar memory leaks
            _trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
        }

        private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
        {
            foreach (var trackedImage in eventArgs.added)
            {
                _lastTrackingState = trackedImage.trackingState;
                OnMatFound?.Invoke(trackedImage);
                LogTrackingState(trackedImage);
            }

            foreach (var trackedImage in eventArgs.updated)
            {
                if (trackedImage.trackingState == _lastTrackingState)
                {
                    continue;
                }

                _lastTrackingState = trackedImage.trackingState;
                OnMatUpdated?.Invoke(trackedImage);
                LogTrackingState(trackedImage);
            }

            foreach (var trackedImage in eventArgs.removed)
            {
                _lastTrackingState = TrackingState.None;
                OnMatLost?.Invoke(trackedImage);
            }
        }

        private void LogTrackingState(ARTrackedImage trackedImage)
        {
            if (trackedImage.trackingState == TrackingState.Tracking)
            {
                Debug.Log($"[AR] Tapete detectado y anclado: {trackedImage.referenceImage.name}");
            }
            else if (trackedImage.trackingState == TrackingState.Limited)
            {
                Debug.LogWarning("[AR] Tapete detectado, pero la iluminación es pobre o está borroso.");
            }
        }
    }
}
