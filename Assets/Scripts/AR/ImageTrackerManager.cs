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
            // 1. El tapete acaba de entrar en la cámara
            foreach (var trackedImage in eventArgs.added)
            {
                OnMatFound?.Invoke(trackedImage);
                LogTrackingState(trackedImage);
            }

            // 2. El tapete se está moviendo o la cámara cambia de ángulo
            foreach (var trackedImage in eventArgs.updated)
            {
                OnMatUpdated?.Invoke(trackedImage);
                LogTrackingState(trackedImage);
            }

            // 3. El tapete salió completamente de la vista
            foreach (var trackedImage in eventArgs.removed)
            {
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
