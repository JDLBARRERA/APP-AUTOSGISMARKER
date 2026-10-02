using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ARTrackBuilder.AR;
using ARTrackBuilder.Competition;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Torre de tiempos, parrilla y ficha del líder. En un segundo monitor ocupa esa pantalla para el televisor.
    /// </summary>
    public class SlotBroadcastUI : MonoBehaviour
    {
        private enum BoardLayout
        {
            Tower,
            Grid,
            Leader
        }

        private SlotSessionController _session;
        private SlotArchive _archive;
        private VoiceCommentator _voice;
        private Font _font;
        private BoardLayout _layout = BoardLayout.Tower;
        private Text _title;
        private Text _subtitle;
        private Text _record;
        private readonly Text[] _rows = new Text[LapClock.LaneCount];
        private readonly InputField[] _pilotFields = new InputField[LapClock.LaneCount];
        private readonly InputField[] _carFields = new InputField[LapClock.LaneCount];
        private bool _secondDisplay;
        private TrackWaypointProjection _projection;

        private void Start()
        {
            EnsureSystems();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            Build();
            if (_session != null)
            {
                _session.OnClockTick += Refresh;
                _session.OnSessionChanged += Refresh;
            }

            if (_voice != null)
            {
                _voice.OnLine += ShowLine;
            }

            if (_archive != null)
            {
                _archive.OnArchiveChanged += Refresh;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_session != null)
            {
                _session.OnClockTick -= Refresh;
                _session.OnSessionChanged -= Refresh;
            }

            if (_voice != null)
            {
                _voice.OnLine -= ShowLine;
            }

            if (_archive != null)
            {
                _archive.OnArchiveChanged -= Refresh;
            }
        }

        private void EnsureSystems()
        {
            _session = SlotSessionController.Instance != null ? SlotSessionController.Instance : FindObjectOfType<SlotSessionController>();
            _archive = SlotArchive.Instance != null ? SlotArchive.Instance : FindObjectOfType<SlotArchive>();
            _voice = VoiceCommentator.Instance != null ? VoiceCommentator.Instance : FindObjectOfType<VoiceCommentator>();
            if (_archive == null)
            {
                _archive = gameObject.AddComponent<SlotArchive>();
            }

            if (_session == null)
            {
                _session = gameObject.AddComponent<SlotSessionController>();
            }

            if (_voice == null)
            {
                _voice = gameObject.AddComponent<VoiceCommentator>();
            }

            _projection = FindObjectOfType<TrackWaypointProjection>();
        }

        private void Build()
        {
            EnsureEventSystem();
            Transform host = ResolveHost();
            GameObject panel = CreatePanel(host, "Panel_SlotBroadcast", new Color(0f, 0f, 0f, 0.92f), new Vector2(420f, 0f), new Vector2(1000f, 1000f));
            _title = CreateText(panel.transform, "Title", "SLOT", 32, TextAnchor.MiddleCenter, new Vector2(0f, 450f), new Vector2(700f, 48f), Color.white);
            _subtitle = CreateText(panel.transform, "Subtitle", string.Empty, 22, TextAnchor.MiddleCenter, new Vector2(0f, 400f), new Vector2(940f, 36f), new Color(1f, 0.92f, 0.2f));
            _record = CreateText(panel.transform, "Record", string.Empty, 20, TextAnchor.MiddleCenter, new Vector2(0f, 360f), new Vector2(940f, 32f), new Color(0.6f, 0.95f, 1f));

            for (int i = 0; i < LapClock.LaneCount; i++)
            {
                _rows[i] = CreateText(panel.transform, "Row_" + (i + 1), string.Empty, 24, TextAnchor.MiddleLeft, new Vector2(20f, 270f - (i * 72f)), new Vector2(940f, 68f), Color.white);
            }

            CreateButton(panel.transform, "ENTRENO", new Vector2(-360f, -70f), () => Begin(() => _session.StartPractice()));
            CreateButton(panel.transform, "CLASIFICACION", new Vector2(-130f, -70f), () => Begin(() => _session.StartQualifying()));
            CreateButton(panel.transform, "CARRERA", new Vector2(100f, -70f), () => Begin(() => _session.StartRace()));
            CreateButton(panel.transform, "CERRAR", new Vector2(330f, -70f), () => _session.CloseSession());

            CreateButton(panel.transform, "TORRE", new Vector2(-300f, -140f), () => SetLayout(BoardLayout.Tower));
            CreateButton(panel.transform, "PARRILLA", new Vector2(-80f, -140f), () => SetLayout(BoardLayout.Grid));
            CreateButton(panel.transform, "LIDER", new Vector2(140f, -140f), () => SetLayout(BoardLayout.Leader));
            CreateButton(panel.transform, "- VUELTAS", new Vector2(340f, -140f), () => _session.SetRaceLaps(_session.RaceLaps - 1));
            CreateButton(panel.transform, "+ VUELTAS", new Vector2(340f, -210f), () => _session.SetRaceLaps(_session.RaceLaps + 1));

            for (int i = 0; i < LapClock.LaneCount; i++)
            {
                int lane = i;
                CreateButton(panel.transform, "V" + (i + 1), new Vector2(-330f + (i * 100f), -210f), () => _session.PulseLane(lane));
                _pilotFields[i] = CreateField(panel.transform, "Piloto " + (i + 1), new Vector2(-220f, -280f - (i * 48f)), new Vector2(300f, 42f));
                _carFields[i] = CreateField(panel.transform, "Auto " + (i + 1), new Vector2(180f, -280f - (i * 48f)), new Vector2(300f, 42f));
                int captured = i;
                _pilotFields[i].onEndEdit.AddListener(value => CommitPilot(captured));
                _carFields[i].onEndEdit.AddListener(value => CommitPilot(captured));
                if (_archive != null)
                {
                    SlotPilot pilot = _archive.Pilot(i);
                    _pilotFields[i].text = pilot.Name;
                    _carFields[i].text = pilot.Car;
                }
            }
        }

        private void Begin(UnityEngine.Events.UnityAction open)
        {
            for (int i = 0; i < LapClock.LaneCount; i++)
            {
                CommitPilot(i);
            }

            open();
        }

        private void SetLayout(BoardLayout layout)
        {
            _layout = layout;
            Refresh();
        }

        private void ShowLine(string line)
        {
            if (_subtitle != null)
            {
                _subtitle.text = line;
            }
        }

        private void Refresh()
        {
            if (_session == null || _title == null)
            {
                return;
            }

            _title.text = TitleFor(_session.Kind) + "  ·  " + _session.RaceLaps + " VUELTAS";
            if (_voice != null && !string.IsNullOrEmpty(_voice.Subtitle))
            {
                _subtitle.text = _voice.Subtitle;
            }

            string trackId = "slot-local";
            if (_projection != null && _projection.CurrentTrack != null)
            {
                trackId = _projection.CurrentTrack.TrackID;
            }

            if (_archive != null && _record != null)
            {
                TrackBest best = _archive.RecordFor(trackId);
                _record.text = best.Ticks > 0
                    ? "RÉCORD  " + best.PilotName + "  " + LapClock.Format(best.Ticks)
                    : "RÉCORD  sin marca";
            }

            List<SlotStanding> standings = _session.BuildStandings();
            if (_layout == BoardLayout.Leader)
            {
                FillLeader(standings);
                return;
            }

            if (_layout == BoardLayout.Grid)
            {
                FillRows(standings, true);
                return;
            }

            FillTower();
        }

        private void FillTower()
        {
            for (int i = 0; i < LapClock.LaneCount; i++)
            {
                LaneSnapshot clock = _session.Clock.Snapshot(i);
                string live = clock.Running ? LapClock.Format(_session.Clock.LiveTicks(i)) : LapClock.Format(clock.TotalTicks);
                _rows[i].gameObject.SetActive(true);
                _rows[i].fontSize = 26;
                _rows[i].text = (i + 1) + "  " + _session.PilotName(i) + "   " + _session.CarName(i)
                    + "\nVUELTAS " + clock.Laps + "   ÚLTIMA " + LapClock.Format(clock.LastLapTicks)
                    + "   MEJOR " + LapClock.Format(clock.BestTicks) + "   " + live;
            }
        }

        private void FillRows(List<SlotStanding> standings, bool grid)
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].gameObject.SetActive(true);
                _rows[i].fontSize = 26;
                if (i >= standings.Count)
                {
                    _rows[i].text = string.Empty;
                    continue;
                }

                SlotStanding row = standings[i];
                _rows[i].text = row.Position + "  " + row.PilotName + "  " + row.CarName
                    + "   CARRIL " + (row.Lane + 1)
                    + "   MEJOR " + LapClock.Format(row.Clock.BestTicks)
                    + (grid ? string.Empty : "   VUELTAS " + row.Clock.Laps);
            }
        }

        private void FillLeader(List<SlotStanding> standings)
        {
            for (int i = 1; i < _rows.Length; i++)
            {
                _rows[i].gameObject.SetActive(false);
            }

            _rows[0].gameObject.SetActive(true);
            _rows[0].fontSize = 42;
            if (standings.Count == 0)
            {
                _rows[0].text = "SIN LÍDER";
                return;
            }

            SlotStanding leader = standings[0];
            _rows[0].text = "LÍDER\n" + leader.PilotName + "\n" + leader.CarName
                + "\nMEJOR " + LapClock.Format(leader.Clock.BestTicks)
                + "\nVUELTAS " + leader.Clock.Laps;
        }

        private void CommitPilot(int lane)
        {
            if (_session == null)
            {
                return;
            }

            _session.SetPilot(lane, _pilotFields[lane].text, _carFields[lane].text);
        }

        private static string TitleFor(SlotSessionKind kind)
        {
            switch (kind)
            {
                case SlotSessionKind.Practice: return "ENTRENAMIENTO";
                case SlotSessionKind.Qualifying: return "CLASIFICACIÓN";
                case SlotSessionKind.Race: return "CARRERA";
                default: return "SLOT EN ESPERA";
            }
        }

        private Transform ResolveHost()
        {
            if (Display.displays != null && Display.displays.Length > 1)
            {
                Display.displays[1].Activate();
                _secondDisplay = true;
                GameObject canvasObject = new GameObject("Canvas_SlotTV", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.targetDisplay = 1;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                return canvasObject.transform;
            }

            Canvas own = GetComponent<Canvas>();
            if (own != null)
            {
                return own.transform;
            }

            Canvas found = FindObjectOfType<Canvas>();
            return found != null ? found.transform : transform;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private GameObject CreatePanel(Transform parent, string name, Color color, Vector2 position, Vector2 size)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = _secondDisplay ? Vector2.zero : position;
            if (_secondDisplay)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(40f, 40f);
                rect.offsetMax = new Vector2(-40f, -40f);
                rect.anchoredPosition = Vector2.zero;
            }

            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor, Vector2 position, Vector2 box, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = box;
            rect.anchoredPosition = position;
            Text text = textObject.GetComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = color;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private void CreateButton(Transform parent, string label, Vector2 position, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(210f, 52f);
            rect.anchoredPosition = position;
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 1f);
            buttonObject.GetComponent<Button>().onClick.AddListener(action);
            CreateText(buttonObject.transform, "Text", label, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(200f, 48f), Color.white);
        }

        private InputField CreateField(Transform parent, string placeholder, Vector2 position, Vector2 size)
        {
            GameObject fieldObject = new GameObject(placeholder, typeof(RectTransform), typeof(Image), typeof(InputField));
            fieldObject.transform.SetParent(parent, false);
            RectTransform rect = fieldObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            fieldObject.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
            Text text = CreateText(fieldObject.transform, "Text", string.Empty, 18, TextAnchor.MiddleLeft, new Vector2(8f, 0f), size, Color.white);
            Text hint = CreateText(fieldObject.transform, "Placeholder", placeholder, 18, TextAnchor.MiddleLeft, new Vector2(8f, 0f), size, new Color(1f, 1f, 1f, 0.45f));
            InputField field = fieldObject.GetComponent<InputField>();
            field.textComponent = text;
            field.placeholder = hint;
            return field;
        }
    }
}
