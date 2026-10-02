using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ARTrackBuilder.Competition
{
    [Serializable]
    public struct SlotPilot
    {
        public string Name;
        public string Car;
        public int Lane;
    }

    [Serializable]
    public struct TrackBest
    {
        public string TrackID;
        public string TrackName;
        public string PilotName;
        public string CarName;
        public long Ticks;
        public string Date;
    }

    [Serializable]
    public struct SessionResultRow
    {
        public int Position;
        public int Lane;
        public string PilotName;
        public string CarName;
        public int Laps;
        public long BestTicks;
        public long TotalTicks;
    }

    [Serializable]
    public struct SessionSummary
    {
        public string Date;
        public string TrackName;
        public string SessionKind;
        public List<SessionResultRow> Results;
    }

    /// <summary>
    /// Pilotos, récords de pista y el resumen de cada sesión, en JSON local.
    /// </summary>
    public class SlotArchive : MonoBehaviour
    {
        private const int MaxSessions = 50;

        public static SlotArchive Instance { get; private set; }

        [Serializable]
        private class ArchiveFile
        {
            public List<SlotPilot> Pilots = new List<SlotPilot>();
            public List<TrackBest> Records = new List<TrackBest>();
            public List<SessionSummary> Sessions = new List<SessionSummary>();
        }

        private string _path;
        private ArchiveFile _file = new ArchiveFile();

        public event Action OnArchiveChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _path = Path.Combine(Application.persistentDataPath, "slot_archive.json");
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public SlotPilot Pilot(int lane)
        {
            for (int i = 0; i < _file.Pilots.Count; i++)
            {
                if (_file.Pilots[i].Lane == lane)
                {
                    return _file.Pilots[i];
                }
            }

            return new SlotPilot { Name = "Piloto " + (lane + 1), Car = "Auto " + (lane + 1), Lane = lane };
        }

        /// <summary>
        /// Guarda el nombre y el auto de un carril.
        /// </summary>
        public void RememberPilot(int lane, string name, string car)
        {
            SlotPilot pilot = new SlotPilot
            {
                Lane = lane,
                Name = name,
                Car = car
            };

            for (int i = 0; i < _file.Pilots.Count; i++)
            {
                if (_file.Pilots[i].Lane == lane)
                {
                    _file.Pilots[i] = pilot;
                    Save();
                    return;
                }
            }

            _file.Pilots.Add(pilot);
            Save();
        }

        public TrackBest RecordFor(string trackId)
        {
            for (int i = 0; i < _file.Records.Count; i++)
            {
                if (_file.Records[i].TrackID == trackId)
                {
                    return _file.Records[i];
                }
            }

            return new TrackBest();
        }

        /// <summary>
        /// Si el tiempo mejora el récord de esa pista, lo sustituye y devuelve true.
        /// </summary>
        public bool TryRecord(string trackId, string trackName, string pilot, string car, long ticks)
        {
            if (ticks <= 0 || string.IsNullOrEmpty(trackId))
            {
                return false;
            }

            for (int i = 0; i < _file.Records.Count; i++)
            {
                if (_file.Records[i].TrackID != trackId)
                {
                    continue;
                }

                if (_file.Records[i].Ticks > 0 && ticks >= _file.Records[i].Ticks)
                {
                    return false;
                }

                _file.Records[i] = MakeBest(trackId, trackName, pilot, car, ticks);
                Save();
                return true;
            }

            _file.Records.Add(MakeBest(trackId, trackName, pilot, car, ticks));
            Save();
            return true;
        }

        /// <summary>
        /// Añade el cierre de una sesión al archivo.
        /// </summary>
        public void SaveSession(string trackName, SlotSessionKind kind, List<SlotStanding> standings)
        {
            SessionSummary summary = new SessionSummary
            {
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                TrackName = trackName,
                SessionKind = kind.ToString(),
                Results = new List<SessionResultRow>()
            };

            if (standings != null)
            {
                for (int i = 0; i < standings.Count; i++)
                {
                    SlotStanding row = standings[i];
                    summary.Results.Add(new SessionResultRow
                    {
                        Position = row.Position,
                        Lane = row.Lane,
                        PilotName = row.PilotName,
                        CarName = row.CarName,
                        Laps = row.Clock.Laps,
                        BestTicks = row.Clock.BestTicks,
                        TotalTicks = row.Clock.TotalTicks
                    });
                }
            }

            _file.Sessions.Add(summary);
            if (_file.Sessions.Count > MaxSessions)
            {
                _file.Sessions.RemoveAt(0);
            }

            Save();
        }

        private static TrackBest MakeBest(string trackId, string trackName, string pilot, string car, long ticks)
        {
            return new TrackBest
            {
                TrackID = trackId,
                TrackName = trackName,
                PilotName = pilot,
                CarName = car,
                Ticks = ticks,
                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };
        }

        private void Load()
        {
            _file = new ArchiveFile();
            if (!File.Exists(_path))
            {
                SeedPilots();
                return;
            }

            try
            {
                JsonUtility.FromJsonOverwrite(File.ReadAllText(_path), _file);
            }
            catch (Exception error)
            {
                Debug.LogError("[SLOT] No se pudo leer el archivo: " + error.Message);
                _file = new ArchiveFile();
            }

            if (_file.Pilots == null) _file.Pilots = new List<SlotPilot>();
            if (_file.Records == null) _file.Records = new List<TrackBest>();
            if (_file.Sessions == null) _file.Sessions = new List<SessionSummary>();
            if (_file.Pilots.Count == 0)
            {
                SeedPilots();
            }
        }

        private void SeedPilots()
        {
            _file.Pilots.Clear();
            for (int i = 0; i < LapClock.LaneCount; i++)
            {
                _file.Pilots.Add(new SlotPilot
                {
                    Lane = i,
                    Name = "Piloto " + (i + 1),
                    Car = "Auto " + (i + 1)
                });
            }

            Save();
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(_path, JsonUtility.ToJson(_file, true));
                OnArchiveChanged?.Invoke();
            }
            catch (Exception error)
            {
                Debug.LogError("[SLOT] No se pudo guardar el archivo: " + error.Message);
            }
        }
    }
}
