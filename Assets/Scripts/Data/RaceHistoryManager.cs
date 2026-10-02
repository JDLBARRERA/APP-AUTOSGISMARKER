using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ARTrackBuilder.Data
{
    /// <summary>
    /// Estructura de datos para un participante físico.
    /// </summary>
    [Serializable]
    public struct Competitor
    {
        public string PlayerName;
        public string CarModel; // Ej. "Bone Shaker", "Twin Mill"
    }

    /// <summary>
    /// Registro inmutable de una carrera finalizada.
    /// </summary>
    [Serializable]
    public struct RaceRecord
    {
        public string RaceID;
        public string Date; // Guardado en formato ISO-8601
        public string TrackName;
        public string WinnerName;
        public List<Competitor> Participants;
    }

    /// <summary>
    /// Gestiona el guardado, carga y registro del historial competitivo local en formato JSON.
    /// </summary>
    public class RaceHistoryManager : MonoBehaviour
    {
        /// <summary>
        /// Clase "Wrapper" necesaria porque JsonUtility de Unity no serializa Listas directamente en la raíz.
        /// </summary>
        [Serializable]
        private class RaceDatabase
        {
            public List<RaceRecord> History = new List<RaceRecord>();
        }

        private string _saveFilePath;
        private RaceDatabase _database;

        public event Action<List<RaceRecord>> OnHistoryUpdated;

        private void Awake()
        {
            // Crea una ruta segura en el almacenamiento interno del dispositivo (iOS/Android)
            _saveFilePath = Path.Combine(Application.persistentDataPath, "race_history.json");
            LoadHistory();
        }

        /// <summary>
        /// Registra una nueva carrera en el historial y guarda en el disco.
        /// </summary>
        public void AddRaceResult(string trackName, List<Competitor> participants, string winnerName)
        {
            if (participants == null || participants.Count == 0)
            {
                Debug.LogWarning("[Data] No se puede registrar una carrera sin participantes.");
                return;
            }

            if (participants.Count > 10)
            {
                Debug.LogWarning("[Data] El límite máximo es de 10 participantes por carrera. Truncando lista.");
                participants = participants.GetRange(0, 10);
            }

            RaceRecord newRecord = new RaceRecord
            {
                RaceID = Guid.NewGuid().ToString(),
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                TrackName = trackName,
                WinnerName = winnerName,
                Participants = new List<Competitor>(participants)
            };

            _database.History.Add(newRecord);
            SaveHistory();

            Debug.Log($"[Data] Carrera guardada: {trackName} | Ganador: {winnerName}");
        }

        /// <summary>
        /// Devuelve el historial completo, ordenado del más reciente al más antiguo.
        /// </summary>
        public List<RaceRecord> GetSortedHistory()
        {
            List<RaceRecord> sorted = new List<RaceRecord>(_database.History);
            sorted.Reverse(); // Invertir para que el último jugado salga primero
            return sorted;
        }

        private void SaveHistory()
        {
            try
            {
                string json = JsonUtility.ToJson(_database, true);
                File.WriteAllText(_saveFilePath, json);
                OnHistoryUpdated?.Invoke(GetSortedHistory());
            }
            catch (Exception e)
            {
                Debug.LogError($"[Data] Error al guardar el historial: {e.Message}");
            }
        }

        private void LoadHistory()
        {
            _database = new RaceDatabase();

            if (File.Exists(_saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_saveFilePath);
                    JsonUtility.FromJsonOverwrite(json, _database);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Data] Error al cargar el historial: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Útil para pruebas o para dar la opción al usuario de borrar datos locales.
        /// </summary>
        public void ClearHistory()
        {
            _database.History.Clear();
            SaveHistory();
        }
    }
}
