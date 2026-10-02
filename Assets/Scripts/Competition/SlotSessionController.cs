using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ARTrackBuilder.AR;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.Competition
{
    public enum SlotSessionKind
    {
        Idle,
        Practice,
        Qualifying,
        Race
    }

    /// <summary>
    /// Una fila ya ordenada de la sesión.
    /// </summary>
    public struct SlotStanding
    {
        public int Position;
        public int Lane;
        public string PilotName;
        public string CarName;
        public LaneSnapshot Clock;
    }

    /// <summary>
    /// Entrenamiento, clasificación y carrera por vueltas. El pulso de cada carril entra por PulseLane.
    /// </summary>
    public class SlotSessionController : MonoBehaviour
    {
        public const int RaceLapsDefault = 10;

        public static SlotSessionController Instance { get; private set; }

        [SerializeField] private int _raceLaps = RaceLapsDefault;

        private readonly LapClock _clock = new LapClock();
        private readonly string[] _pilots = new string[LapClock.LaneCount];
        private readonly string[] _cars = new string[LapClock.LaneCount];
        private SlotSessionKind _kind = SlotSessionKind.Idle;
        private Coroutine _ticker;
        private bool _closed;
        private List<SlotStanding> _frozen;

        public SlotSessionKind Kind => _kind;

        public int RaceLaps => _raceLaps;

        public LapClock Clock => _clock;

        /// <summary>
        /// Avisa el carril que acaba de recibir un pulso.
        /// </summary>
        public event Action<int> OnLapPulse;

        public event Action OnSessionChanged;

        public event Action OnClockTick;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            for (int i = 0; i < LapClock.LaneCount; i++)
            {
                _pilots[i] = "Piloto " + (i + 1);
                _cars[i] = "Auto " + (i + 1);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDisable()
        {
            if (_ticker != null)
            {
                StopCoroutine(_ticker);
                _ticker = null;
            }
        }

        public string PilotName(int lane)
        {
            return LaneOk(lane) ? _pilots[lane] : string.Empty;
        }

        public string CarName(int lane)
        {
            return LaneOk(lane) ? _cars[lane] : string.Empty;
        }

        /// <summary>
        /// Cambia el piloto de un carril. Vale también con la sesión en marcha.
        /// </summary>
        public void SetPilot(int lane, string pilot, string car)
        {
            if (!LaneOk(lane))
            {
                return;
            }

            _pilots[lane] = string.IsNullOrEmpty(pilot) ? "Piloto " + (lane + 1) : pilot;
            _cars[lane] = string.IsNullOrEmpty(car) ? "Auto " + (lane + 1) : car;
            SlotArchive archive = FindArchive();
            if (archive != null)
            {
                archive.RememberPilot(lane, _pilots[lane], _cars[lane]);
            }
        }

        public void SetRaceLaps(int laps)
        {
            _raceLaps = Mathf.Clamp(laps, 1, 200);
            OnSessionChanged?.Invoke();
        }

        public void StartPractice()
        {
            Open(SlotSessionKind.Practice);
        }

        public void StartQualifying()
        {
            Open(SlotSessionKind.Qualifying);
        }

        public void StartRace()
        {
            Open(SlotSessionKind.Race);
        }

        /// <summary>
        /// Paso por meta del carril. Lo llaman los botones y, en el editor, las teclas 1 a 4.
        /// </summary>
        public void PulseLane(int lane)
        {
            if (_kind == SlotSessionKind.Idle || !LaneOk(lane))
            {
                return;
            }

            OnLapPulse?.Invoke(lane);
            if (!_clock.TryCompleteLap(lane, out long lapTicks))
            {
                OnClockTick?.Invoke();
                return;
            }

            LaneSnapshot snap = _clock.Snapshot(lane);
            SlotArchive archive = FindArchive();
            bool record = archive != null && archive.TryRecord(CurrentTrackId(), CurrentTrackName(), _pilots[lane], _cars[lane], lapTicks);
            VoiceCommentator voice = FindVoice();
            if (voice != null)
            {
                voice.SayLap(_pilots[lane], snap.Laps, lapTicks);
                if (record)
                {
                    voice.SayRecord(_pilots[lane], lapTicks);
                }
            }

            if (_kind == SlotSessionKind.Race && snap.Laps >= _raceLaps)
            {
                CloseSession();
                return;
            }

            OnClockTick?.Invoke();
        }

        /// <summary>
        /// Cierra la sesión, ordena la tabla y guarda el resultado.
        /// </summary>
        public void CloseSession()
        {
            if (_kind == SlotSessionKind.Idle || _closed)
            {
                return;
            }

            _closed = true;
            _clock.StopAll();
            if (_ticker != null)
            {
                StopCoroutine(_ticker);
                _ticker = null;
            }

            List<SlotStanding> standings = BuildStandings();
            _frozen = standings;
            SlotArchive archive = FindArchive();
            if (archive != null)
            {
                archive.SaveSession(CurrentTrackName(), _kind, standings);
            }

            if (standings.Count > 0)
            {
                string winner = standings[0].PilotName;
                RaceHistoryManager history = FindObjectOfType<RaceHistoryManager>();
                if (history != null)
                {
                    List<Competitor> people = new List<Competitor>(standings.Count);
                    for (int i = 0; i < standings.Count; i++)
                    {
                        people.Add(new Competitor
                        {
                            PlayerName = standings[i].PilotName,
                            CarModel = standings[i].CarName
                        });
                    }

                    history.AddRaceResult(CurrentTrackName(), people, winner);
                }

                VoiceCommentator voice = FindVoice();
                if (voice != null)
                {
                    voice.SayWinner(winner);
                }
            }

            _kind = SlotSessionKind.Idle;
            OnSessionChanged?.Invoke();
            OnClockTick?.Invoke();
        }

        public List<SlotStanding> BuildStandings()
        {
            if (_kind == SlotSessionKind.Idle && _frozen != null)
            {
                return _frozen;
            }

            List<SlotStanding> rows = new List<SlotStanding>(LapClock.LaneCount);
            for (int i = 0; i < LapClock.LaneCount; i++)
            {
                rows.Add(new SlotStanding
                {
                    Lane = i,
                    PilotName = _pilots[i],
                    CarName = _cars[i],
                    Clock = _clock.Snapshot(i)
                });
            }

            rows.Sort(CompareStanding);
            for (int i = 0; i < rows.Count; i++)
            {
                SlotStanding row = rows[i];
                row.Position = i + 1;
                rows[i] = row;
            }

            return rows;
        }

        private void Open(SlotSessionKind kind)
        {
            if (_ticker != null)
            {
                StopCoroutine(_ticker);
            }

            PullPilots();
            _clock.ResetAll();
            _frozen = null;
            _kind = kind;
            _closed = false;
            _ticker = StartCoroutine(Tick());
            OnSessionChanged?.Invoke();
            OnClockTick?.Invoke();
        }

        private IEnumerator Tick()
        {
            WaitForSeconds wait = new WaitForSeconds(0.05f);
            while (_kind != SlotSessionKind.Idle)
            {
                OnClockTick?.Invoke();
                yield return wait;
            }
        }

        private int CompareStanding(SlotStanding a, SlotStanding b)
        {
            if (_kind == SlotSessionKind.Race)
            {
                int laps = b.Clock.Laps.CompareTo(a.Clock.Laps);
                if (laps != 0)
                {
                    return laps;
                }

                return a.Clock.TotalTicks.CompareTo(b.Clock.TotalTicks);
            }

            long bestA = a.Clock.BestTicks == 0 ? long.MaxValue : a.Clock.BestTicks;
            long bestB = b.Clock.BestTicks == 0 ? long.MaxValue : b.Clock.BestTicks;
            return bestA.CompareTo(bestB);
        }

        private void PullPilots()
        {
            SlotArchive archive = FindArchive();
            if (archive == null)
            {
                return;
            }

            for (int i = 0; i < LapClock.LaneCount; i++)
            {
                SlotPilot pilot = archive.Pilot(i);
                if (!string.IsNullOrEmpty(pilot.Name))
                {
                    _pilots[i] = pilot.Name;
                }

                if (!string.IsNullOrEmpty(pilot.Car))
                {
                    _cars[i] = pilot.Car;
                }
            }
        }

        private static bool LaneOk(int lane)
        {
            return lane >= 0 && lane < LapClock.LaneCount;
        }

        private static SlotArchive FindArchive()
        {
            return SlotArchive.Instance != null ? SlotArchive.Instance : FindObjectOfType<SlotArchive>();
        }

        private static VoiceCommentator FindVoice()
        {
            return VoiceCommentator.Instance != null ? VoiceCommentator.Instance : FindObjectOfType<VoiceCommentator>();
        }

        private static string CurrentTrackName()
        {
            TrackWaypointProjection projection = FindObjectOfType<TrackWaypointProjection>();
            if (projection != null && !string.IsNullOrEmpty(projection.ActiveTrackName))
            {
                return projection.ActiveTrackName;
            }

            return "Pista slot";
        }

        private static string CurrentTrackId()
        {
            TrackWaypointProjection projection = FindObjectOfType<TrackWaypointProjection>();
            if (projection != null && projection.CurrentTrack != null && !string.IsNullOrEmpty(projection.CurrentTrack.TrackID))
            {
                return projection.CurrentTrack.TrackID;
            }

            return "slot-local";
        }

#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) PulseLane(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) PulseLane(1);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) PulseLane(2);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) PulseLane(3);
        }
#endif
    }
}
