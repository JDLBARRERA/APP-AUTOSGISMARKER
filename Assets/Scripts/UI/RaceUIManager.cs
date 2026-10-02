using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Gestiona la interfaz visual para registrar participantes antes de la carrera
    /// y seleccionar al ganador al finalizar, conectándose con el RaceHistoryManager.
    /// </summary>
    public class RaceUIManager : MonoBehaviour
    {
        [Header("Dependencias")]
        [SerializeField] private RaceHistoryManager _historyManager;

        [Header("Paneles de UI")]
        [SerializeField] private GameObject _registrationPanel;
        [SerializeField] private GameObject _podiumPanel;

        [Header("Registro de Participantes")]
        [SerializeField] private InputField _playerNameInput;
        [SerializeField] private InputField _carModelInput;
        [SerializeField] private Button _addPlayerButton;
        [SerializeField] private Text _playerListText;
        [SerializeField] private Button _startRaceButton;

        [Header("Resultados (Podio)")]
        [SerializeField] private Dropdown _winnerDropdown;
        [SerializeField] private Button _saveResultButton;

        private List<Competitor> _currentParticipants = new List<Competitor>();
        private string _currentTrackName = "Circuito Asfalto AR"; // En el futuro se conectará al TrackDataManager

        private void OnEnable()
        {
            if (_addPlayerButton != null) _addPlayerButton.onClick.AddListener(AddPlayer);
            if (_startRaceButton != null) _startRaceButton.onClick.AddListener(StartRace);
            if (_saveResultButton != null) _saveResultButton.onClick.AddListener(SaveRaceResult);
        }

        private void OnDisable()
        {
            if (_addPlayerButton != null) _addPlayerButton.onClick.RemoveListener(AddPlayer);
            if (_startRaceButton != null) _startRaceButton.onClick.RemoveListener(StartRace);
            if (_saveResultButton != null) _saveResultButton.onClick.RemoveListener(SaveRaceResult);
        }

        private void Start()
        {
            // Estado inicial: Mostrar registro, ocultar podio
            _registrationPanel.SetActive(true);
            _podiumPanel.SetActive(false);
            UpdatePlayerListUI();
        }

        private void AddPlayer()
        {
            if (_currentParticipants.Count >= 10)
            {
                Debug.LogWarning("[UI] Límite de 10 jugadores alcanzado.");
                return;
            }

            if (!string.IsNullOrEmpty(_playerNameInput.text) && !string.IsNullOrEmpty(_carModelInput.text))
            {
                Competitor newPlayer = new Competitor
                {
                    PlayerName = _playerNameInput.text,
                    CarModel = _carModelInput.text
                };

                _currentParticipants.Add(newPlayer);
                
                // Limpiar campos para el siguiente jugador
                _playerNameInput.text = "";
                _carModelInput.text = "";
                
                UpdatePlayerListUI();
            }
        }

        private void UpdatePlayerListUI()
        {
            if (_playerListText == null) return;

            _playerListText.text = "Participantes:\n";
            foreach (var p in _currentParticipants)
            {
                _playerListText.text += $"- {p.PlayerName} ({p.CarModel})\n";
            }

            // Solo permitir iniciar carrera si hay al menos 2 jugadores
            _startRaceButton.interactable = _currentParticipants.Count >= 2;
        }

        private void StartRace()
        {
            // Ocultar registro, la carrera física comienza
            _registrationPanel.SetActive(false);
            
            // Preparar el dropdown del ganador con los nombres de los participantes
            _winnerDropdown.ClearOptions();
            List<string> participantNames = new List<string>();
            foreach (var p in _currentParticipants)
            {
                participantNames.Add(p.PlayerName);
            }
            _winnerDropdown.AddOptions(participantNames);

            // Mostrar el botón de podio (simularemos que aparece cuando terminan de jugar)
            _podiumPanel.SetActive(true);
        }

        private void SaveRaceResult()
        {
            if (_historyManager != null && _currentParticipants.Count > 0)
            {
                string winnerName = _winnerDropdown.options[_winnerDropdown.value].text;
                _historyManager.AddRaceResult(_currentTrackName, _currentParticipants, winnerName);
                
                // Reiniciar para una nueva carrera
                _currentParticipants.Clear();
                UpdatePlayerListUI();
                _podiumPanel.SetActive(false);
                _registrationPanel.SetActive(true);
            }
        }
    }
}
