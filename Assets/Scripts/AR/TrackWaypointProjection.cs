using System;
using System.Collections.Generic;
using UnityEngine;
using ARTrackBuilder.Competition;
using ARTrackBuilder.Data;
using ARTrackBuilder.UI;

namespace ARTrackBuilder.AR
{
    public enum GamePhase { SetupDrawing, ActiveRacing }

    /// <summary>
    /// Proyecta una pista ya definida: números para el gis y, en carrera, exactos, fosos y tiros largos.
    /// </summary>
    public class TrackWaypointProjection : MonoBehaviour
    {
        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        [Header("Fase de Juego")]
        public GamePhase CurrentPhase = GamePhase.SetupDrawing;

        [Header("Configuración de Nodos de Luz")]
        [SerializeField] private GameObject _waypointDotPrefab;
        [SerializeField] private GameObject _exactZonePrefab;
        [SerializeField] private GameObject _turboBoostPrefab;
        [SerializeField] private GameObject _finishArchPrefab;

        private readonly List<GameObject> _spawnedProjectionElements = new List<GameObject>();
        private DefinedTrack _currentTrack;
        private RaceFormat _format = RaceFormat.City;
        private int _moduleIndex;
        private int _raceIndex;

        public string ActiveTrackName => _currentTrack != null ? _currentTrack.DisplayName : string.Empty;

        public int ActiveLaps => _currentTrack != null ? _currentTrack.Laps : 1;

        public RaceFormat ActiveFormat => _format;

        public int ActiveModule => _moduleIndex;

        public int ActiveRace => _raceIndex;

        public DefinedTrack CurrentTrack => _currentTrack;

        public event Action OnTrackProjected;

        private void Awake()
        {
            if (FindObjectOfType<FloorReliefScanner>() == null)
            {
                gameObject.AddComponent<FloorReliefScanner>();
            }

            if (FindObjectOfType<LandscapeDirector>() == null)
            {
                gameObject.AddComponent<LandscapeDirector>();
            }

            if (GetComponent<TrackChalkRibbon>() == null)
            {
                gameObject.AddComponent<TrackChalkRibbon>();
            }

            if (GetComponent<LandscapeGround>() == null)
            {
                gameObject.AddComponent<LandscapeGround>();
            }

            if (FindObjectOfType<LandscapeUIManager>() == null)
            {
                gameObject.AddComponent<LandscapeUIManager>();
            }

            if (FindObjectOfType<SlotArchive>() == null)
            {
                gameObject.AddComponent<SlotArchive>();
            }

            if (FindObjectOfType<SlotSessionController>() == null)
            {
                gameObject.AddComponent<SlotSessionController>();
            }

            if (FindObjectOfType<VoiceCommentator>() == null)
            {
                gameObject.AddComponent<VoiceCommentator>();
            }

            if (FindObjectOfType<SlotBroadcastUI>() == null)
            {
                gameObject.AddComponent<SlotBroadcastUI>();
            }
        }

        /// <summary>
        /// Muestra el Circuito, en fase de dibujo.
        /// </summary>
        private void Start()
        {
            if (_currentTrack == null)
            {
                LoadPackage(RaceFormat.City, 0, 0, GamePhase.SetupDrawing);
            }
        }

        public void GenerateDefaultCircuit()
        {
            SelectCityRace();
        }

        public void SelectCityRace()
        {
            LoadPackage(RaceFormat.City, 0, 0, GamePhase.SetupDrawing);
        }

        public void SelectMediumRace()
        {
            LoadPackage(RaceFormat.Medium, 0, 0, GamePhase.SetupDrawing);
        }

        public void SelectGrandPrixRace()
        {
            LoadPackage(RaceFormat.GrandPrix, 0, 0, GamePhase.SetupDrawing);
        }

        public void SelectNextDefinedTrack()
        {
            LoadPackage((RaceFormat)(((int)_format + 1) % 3), 0, 0, CurrentPhase);
        }

        public void SelectPreviousDefinedTrack()
        {
            LoadPackage((RaceFormat)(((int)_format + 2) % 3), 0, 0, CurrentPhase);
        }

        /// <summary>
        /// Avanza dentro de las 10 carreras del módulo. Al terminar, abre el módulo siguiente de la misma dificultad.
        /// </summary>
        public void SiguienteCarrera()
        {
            if (_currentTrack == null)
            {
                LoadPackage(_format, 0, 0, CurrentPhase);
                return;
            }

            int race = _raceIndex + 1;
            int module = _moduleIndex;
            if (race >= RaceSeasonCatalog.RacesPerModule)
            {
                race = 0;
                module = (_moduleIndex + 1) % RaceSeasonCatalog.ModuleCount;
            }

            LoadPackage(_format, module, race, CurrentPhase);
        }

        /// <summary>
        /// Apaga los números y enciende meta, tiros exactos, fosos y tiros largos.
        /// </summary>
        public void BeginRace()
        {
            if (_currentTrack == null)
            {
                Debug.LogWarning("[PROYECTOR] No hay una pista definida. Genera la constelación antes de comenzar la carrera.");
                return;
            }

            SwitchToRacePhase();
        }

        public void SwitchToRacePhase()
        {
            CurrentPhase = GamePhase.ActiveRacing;
            ProjectCurrentTrack();
            Debug.Log("<color=green>[PROYECTOR] " + ActiveTrackName + " · " + ActiveLaps + " vuelta(s). Carrera en curso.</color>");
        }

        private void LoadPackage(RaceFormat format, int module, int race, GamePhase phase)
        {
            _format = format;
            _moduleIndex = module;
            _raceIndex = race;
            _currentTrack = RaceSeasonCatalog.Get(format, module, race);
            CurrentPhase = phase;
            ProjectCurrentTrack();
            Debug.Log("[PROYECTOR] " + ActiveTrackName + " · " + ActiveLaps + " vuelta(s).");
            OnTrackProjected?.Invoke();
        }

        private void ProjectCurrentTrack()
        {
            ClearProjection();
            if (_currentTrack == null || _currentTrack.Waypoints == null)
            {
                return;
            }

            TrackWaypoint[] waypoints = _currentTrack.Waypoints;
            for (int i = 0; i < waypoints.Length; i++)
            {
                Vector3 position = waypoints[i].Position;
                if (CurrentPhase == GamePhase.SetupDrawing)
                {
                    SpawnDrawingDot(i, waypoints[i]);
                    Vector3 next = waypoints[(i + 1) % waypoints.Length].Position;
                    SpawnDirectionArrow(position, next);
                }
                else
                {
                    SpawnRaceMarker(i, waypoints[i]);
                }
            }
        }

        private void SpawnDrawingDot(int index, TrackWaypoint waypoint)
        {
            GameObject dot = _waypointDotPrefab != null
                ? Instantiate(_waypointDotPrefab, transform)
                : CreateMarkerBody("Punto", ColorFor(waypoint.Rule), new Vector3(0.03f, 0.01f, 0.03f));

            if (_waypointDotPrefab != null)
            {
                dot.transform.SetParent(transform, false);
            }

            dot.transform.localPosition = waypoint.Position;
            Tint(dot, ColorFor(waypoint.Rule));
            SetLabel(dot, (index + 1).ToString(), Color.black);
            _spawnedProjectionElements.Add(dot);
        }

        private void SpawnRaceMarker(int index, TrackWaypoint waypoint)
        {
            if (waypoint.Rule == WaypointRule.Path)
            {
                return;
            }

            GameObject marker = CreateRuleMarker(waypoint);
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = waypoint.Position;
            marker.name = RuleName(waypoint) + "_" + (index + 1);
            SetLabel(marker, LabelFor(waypoint), Color.white);
            _spawnedProjectionElements.Add(marker);
            Debug.Log("[PROYECTOR] Punto " + (index + 1) + ": " + LabelFor(waypoint));
        }

        private GameObject CreateRuleMarker(TrackWaypoint waypoint)
        {
            GameObject prefab = PrefabFor(waypoint.Rule);
            if (prefab != null)
            {
                return Instantiate(prefab, transform);
            }

            Vector3 scale = waypoint.Rule == WaypointRule.LongShot
                ? new Vector3(0.04f, 0.008f, 0.12f)
                : new Vector3(0.08f, 0.008f, 0.08f);
            return CreateMarkerBody(RuleName(waypoint), ColorFor(waypoint.Rule), scale);
        }

        private GameObject PrefabFor(WaypointRule rule)
        {
            switch (rule)
            {
                case WaypointRule.Finish:
                    return _finishArchPrefab;
                case WaypointRule.ExactShot:
                    return _exactZonePrefab;
                case WaypointRule.LongShot:
                    return _turboBoostPrefab;
                default:
                    return null;
            }
        }

        private static string RuleName(TrackWaypoint waypoint)
        {
            switch (waypoint.Rule)
            {
                case WaypointRule.Finish: return "Meta";
                case WaypointRule.ExactShot: return "Exacto";
                case WaypointRule.Pit: return "Foso";
                case WaypointRule.LongShot: return "TiroLargo";
                case WaypointRule.TensionTrap: return "Trampa";
                case WaypointRule.Lava: return "Lava";
                case WaypointRule.Ice: return "Hielo";
                default: return "Punto";
            }
        }

        private static string LabelFor(TrackWaypoint waypoint)
        {
            switch (waypoint.Rule)
            {
                case WaypointRule.Finish: return "META";
                case WaypointRule.ExactShot: return "EXACTO";
                case WaypointRule.Pit: return "FOSO";
                case WaypointRule.LongShot: return "TIRO LARGO";
                case WaypointRule.TensionTrap: return "TRAMPA " + Mathf.Clamp(waypoint.RuleValue, 1, 3);
                case WaypointRule.Lava: return "LAVA";
                case WaypointRule.Ice: return "HIELO";
                default: return string.Empty;
            }
        }

        private static Color ColorFor(WaypointRule rule)
        {
            switch (rule)
            {
                case WaypointRule.Finish: return new Color(1f, 0.15f, 0.05f);
                case WaypointRule.ExactShot: return new Color(1f, 0.85f, 0.1f);
                case WaypointRule.Pit: return new Color(0.35f, 0.02f, 0.02f);
                case WaypointRule.LongShot: return new Color(0.1f, 1f, 0.3f);
                case WaypointRule.TensionTrap: return new Color(0.75f, 0.15f, 1f);
                case WaypointRule.Lava: return new Color(1f, 0.35f, 0.05f);
                case WaypointRule.Ice: return new Color(0.55f, 0.9f, 1f);
                default: return new Color(0.2f, 0.9f, 1f);
            }
        }

        private GameObject CreateMarkerBody(string name, Color color, Vector3 scale)
        {
            PrimitiveType primitive = name == "Foso" ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            GameObject body = GameObject.CreatePrimitive(primitive);
            body.name = name;
            body.transform.SetParent(transform, false);
            body.transform.localScale = scale;
            Tint(body, color);
            Collider bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null)
            {
                Destroy(bodyCollider);
            }

            return body;
        }

        private void Tint(GameObject target, Color color)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer.GetComponent<TextMesh>() != null)
                {
                    continue;
                }
                renderer.material.color = color;
                renderer.material.EnableKeyword("_EMISSION");
                if (renderer.material.HasProperty(EmissionColor))
                {
                    renderer.material.SetColor(EmissionColor, color * 2f);
                }
            }
        }

        private static void SetLabel(GameObject target, string value, Color color)
        {
            TextMesh numberText = target.GetComponentInChildren<TextMesh>();
            if (numberText == null)
            {
                GameObject label = new GameObject("Label");
                label.transform.SetParent(target.transform, false);
                label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                label.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                numberText = label.AddComponent<TextMesh>();
                numberText.characterSize = 0.15f;
                numberText.anchor = TextAnchor.MiddleCenter;
                numberText.alignment = TextAlignment.Center;
            }

            numberText.text = value;
            numberText.color = color;
        }

        private void SpawnDirectionArrow(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            GameObject arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "DirectionArrow";
            arrow.transform.SetParent(transform, false);
            arrow.transform.localPosition = Vector3.Lerp(from, to, 0.65f);
            arrow.transform.localRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            arrow.transform.localScale = new Vector3(0.008f, 0.002f, 0.02f);
            Tint(arrow, Color.white);

            Collider arrowCollider = arrow.GetComponent<Collider>();
            if (arrowCollider != null)
            {
                Destroy(arrowCollider);
            }

            _spawnedProjectionElements.Add(arrow);
        }

        private void ClearProjection()
        {
            for (int i = 0; i < _spawnedProjectionElements.Count; i++)
            {
                if (_spawnedProjectionElements[i] != null)
                {
                    Destroy(_spawnedProjectionElements[i]);
                }
            }

            _spawnedProjectionElements.Clear();
        }
    }
}
