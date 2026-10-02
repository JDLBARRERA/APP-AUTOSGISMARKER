using System;
using System.Collections.Generic;
using UnityEngine;

namespace ARTrackBuilder.Data
{
    /// <summary>
    /// Estructura de datos para definir cada variante de pista en el catálogo.
    /// </summary>
    [Serializable]
    public struct TrackDefinition
    {
        public string TrackID;
        public string DisplayName;
        public GameObject TrackPrefab;
    }

    /// <summary>
    /// Gestiona el inventario de pistas disponibles y notifica cuando el usuario
    /// selecciona un diseño diferente.
    /// </summary>
    public class TrackDataManager : MonoBehaviour
    {
        [Header("Catálogo de Pistas (Hot Wheels)")]
        [SerializeField] private List<TrackDefinition> _availableTracks = new List<TrackDefinition>();

        // Evento que notifica al proyector qué prefab debe mostrar ahora
        public event Action<GameObject> OnTrackChanged;

        private int _currentTrackIndex = 0;

        private void Start()
        {
            // Forzar la selección de la primera pista al iniciar
            if (_availableTracks.Count > 0)
            {
                NotifyTrackChange();
            }
        }

        public void SelectNextTrack()
        {
            if (_availableTracks.Count == 0) return;
            
            _currentTrackIndex++;
            if (_currentTrackIndex >= _availableTracks.Count)
            {
                _currentTrackIndex = 0; // Volver al inicio del catálogo
            }
            
            NotifyTrackChange();
        }

        public void SelectPreviousTrack()
        {
            if (_availableTracks.Count == 0) return;

            _currentTrackIndex--;
            if (_currentTrackIndex < 0)
            {
                _currentTrackIndex = _availableTracks.Count - 1; // Ir al final
            }

            NotifyTrackChange();
        }

        private void NotifyTrackChange()
        {
            GameObject selectedPrefab = _availableTracks[_currentTrackIndex].TrackPrefab;
            OnTrackChanged?.Invoke(selectedPrefab);
            Debug.Log($"[Data] Pista seleccionada: {_availableTracks[_currentTrackIndex].DisplayName}");
        }
    }
}
