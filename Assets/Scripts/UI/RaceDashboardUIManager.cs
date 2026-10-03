using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ARTrackBuilder.AR;
using ARTrackBuilder.Core;
using ARTrackBuilder.Data;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Tablero de mando: tarjetas de tiro, cierre de turno y botones de las reglas de la pista activa.
    /// </summary>
    public class RaceDashboardUIManager : MonoBehaviour
    {
        [Header("Jugadores")]
        public Text CurrentPlayerText;
        public Text NextPlayerText;
        public Text TrackNameText;

        [Header("Tarjetas de Tiros")]
        public Image[] ShotCards;
        public Text[] ShotCardTexts;

        [Header("Controles")]
        public Button ActionButton;
        public Button PassTurnButton;
        public Text ActionButtonText;
        public Text HistoryText;
        public Transform RuleButtonRoot;

        private readonly RaceThrowOrder _throwOrder = new RaceThrowOrder();
        private TrackWaypointProjection _projection;
        private Font _font;

        private void OnEnable()
        {
            if (ActionButton) ActionButton.onClick.AddListener(OnShotClicked);
            if (PassTurnButton) PassTurnButton.onClick.AddListener(OnPassClicked);
            BindShotCards(true);
            if (RaceTurnController.Instance)
            {
                RaceTurnController.Instance.OnShotChanged += UpdateUI;
                RaceTurnController.Instance.OnRuleAlertTriggered += OnAlert;
            }

            BindProjection();
        }

        private void OnDisable()
        {
            if (ActionButton) ActionButton.onClick.RemoveListener(OnShotClicked);
            if (PassTurnButton) PassTurnButton.onClick.RemoveListener(OnPassClicked);
            BindShotCards(false);
            if (RaceTurnController.Instance)
            {
                RaceTurnController.Instance.OnShotChanged -= UpdateUI;
                RaceTurnController.Instance.OnRuleAlertTriggered -= OnAlert;
            }

            if (_projection != null)
            {
                _projection.OnTrackProjected -= RefreshRules;
            }
        }

        private void Start()
        {
            if (_throwOrder.Count == 0)
            {
                _throwOrder.SetGrid(new[] { "Jugador 1", "Jugador 2" });
            }

            if (RaceTurnController.Instance != null)
            {
                RaceTurnController.Instance.OnShotChanged -= UpdateUI;
                RaceTurnController.Instance.OnShotChanged += UpdateUI;
                RaceTurnController.Instance.OnRuleAlertTriggered -= OnAlert;
                RaceTurnController.Instance.OnRuleAlertTriggered += OnAlert;
            }

            BindProjection();
            int shot = RaceTurnController.Instance != null ? RaceTurnController.Instance.CurrentShot : 1;
            UpdateUI(shot);
            RefreshRules();
        }

        private void BindShotCards(bool listen)
        {
            if (ShotCards == null)
            {
                return;
            }

            for (int i = 0; i < ShotCards.Length; i++)
            {
                Image card = ShotCards[i];
                if (card == null)
                {
                    continue;
                }

                Button button = card.GetComponent<Button>();
                if (button == null)
                {
                    button = card.gameObject.AddComponent<Button>();
                }

                button.targetGraphic = card;
                button.transition = Selectable.Transition.None;
                button.onClick.RemoveListener(OnShotClicked);
                if (listen)
                {
                    button.onClick.AddListener(OnShotClicked);
                }
            }
        }

        /// <summary>
        /// Sustituye los nombres por defecto con los participantes registrados.
        /// </summary>
        public void SetPlayers(string[] players)
        {
            if (players == null || players.Length == 0)
            {
                return;
            }

            int count = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (!string.IsNullOrEmpty(players[i]))
                {
                    count++;
                }
            }

            if (count == 0)
            {
                return;
            }

            string[] grid = new string[count];
            int write = 0;
            for (int i = 0; i < players.Length; i++)
            {
                if (!string.IsNullOrEmpty(players[i]))
                {
                    grid[write] = players[i];
                    write++;
                }
            }

            _throwOrder.SetGrid(grid);
            RememberCurrentPlayer();
            UpdateUI(RaceTurnController.Instance != null ? RaceTurnController.Instance.CurrentShot : 1);
        }

        public void OnShotClicked()
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.RegisterShot();
        }

        public void OnPassClicked()
        {
            if (HistoryText) HistoryText.text += $"\n• {GetCurrentPlayer()}: terminó el turno antes de tiempo.";
            AdvancePlayer();
        }

        /// <summary>
        /// El árbitro otorga un tiro extra. Vale desde el primer tiro del turno.
        /// </summary>
        public void ApplyTurbo()
        {
            OnAlert("🔥 ¡TURBO! +1 Tiro extra.");
            if (RaceTurnController.Instance == null || RaceTurnController.Instance.CurrentShot < 1)
            {
                return;
            }

            RaceTurnController.Instance.GrantTurboShot();
        }

        /// <summary>
        /// El árbitro cierra el turno por trampa y pasa al siguiente piloto.
        /// </summary>
        public void ApplyTrap()
        {
            OnAlert("🧊 ¡TRAMPA! Turno finalizado.");
            AdvancePlayer();
        }

        /// <summary>
        /// El árbitro confirma que el tiro quedó en la zona exacta.
        /// </summary>
        public void ApplyExactShot()
        {
            OnAlert("🎯 ¡TIRO EXACTO! Movimiento perfecto.");
        }

        /// <summary>
        /// El árbitro marca un choque y deja la posición penalizada.
        /// </summary>
        public void ApplyCrash()
        {
            OnAlert("💥 ¡CHOQUE! Posición penalizada.");
        }

        private void UpdateUI(int currentShot)
        {
            if (CurrentPlayerText) CurrentPlayerText.text = "TURNO: " + GetCurrentPlayer() + " (" + _throwOrder.CurrentPlace + "º)";
            if (NextPlayerText) NextPlayerText.text = "SIGUIENTE: " + _throwOrder.Next + "  ·  1º " + _throwOrder.Leader;

            bool turnLost = RaceTurnController.Instance != null && RaceTurnController.Instance.CurrentTurnLost;
            int limit = RaceTurnController.Instance != null ? RaceTurnController.Instance.ShotLimit : 3;
            if (ShotCards != null)
            {
                for (int i = 0; i < ShotCards.Length; i++)
                {
                    if (ShotCards[i] == null)
                    {
                        continue;
                    }

                    if (i >= limit)
                    {
                        ShotCards[i].color = new Color(0.15f, 0.15f, 0.15f, 0.35f);
                        SetCardStatus(i, string.Empty);
                    }
                    else if (turnLost || i > currentShot - 1)
                    {
                        ShotCards[i].color = Color.white;
                        SetCardStatus(i, "PENDIENTE");
                    }
                    else if (i < currentShot - 1)
                    {
                        ShotCards[i].color = new Color(0.2f, 0.8f, 0.4f, 0.5f);
                        SetCardStatus(i, "HECHO");
                    }
                    else
                    {
                        ShotCards[i].color = new Color(0.2f, 0.6f, 1f, 0.8f);
                        SetCardStatus(i, "EN CURSO");
                    }
                }
            }

            if (ActionButtonText == null)
            {
                return;
            }

            if (turnLost)
            {
                ActionButtonText.text = "TURNO PERDIDO";
                return;
            }

            ActionButtonText.text = $"¡YA TIRÉ! (TIRO {currentShot} DE {limit})";
        }

        private void SetCardStatus(int index, string status)
        {
            if (ShotCardTexts == null || index >= ShotCardTexts.Length || ShotCardTexts[index] == null)
            {
                return;
            }

            ShotCardTexts[index].text = status;
        }

        private void AdvancePlayer()
        {
            ShiftPlayer();
            if (RaceTurnController.Instance) RaceTurnController.Instance.ResetTurn();
            else UpdateUI(1);
        }

        private void ShiftPlayer()
        {
            bool realigned = _throwOrder.CompleteTurn();
            RememberCurrentPlayer();
            if (realigned && HistoryText != null)
            {
                HistoryText.text += "\n• Orden alineado: tira primero " + _throwOrder.Leader + ".";
            }
        }

        private void RememberCurrentPlayer()
        {
            if (RaceTurnController.Instance != null)
            {
                RaceTurnController.Instance.CurrentPlayerName = GetCurrentPlayer();
            }
        }

        private void OnAlert(string msg)
        {
            if (HistoryText) HistoryText.text += $"\n• {GetCurrentPlayer()}: {msg}";
            if (string.IsNullOrEmpty(msg))
            {
                return;
            }

            if (msg.Contains("CAMBIO DE TURNO") || msg.Contains("Pasas este turno"))
            {
                ShiftPlayer();
            }
        }

        private string GetCurrentPlayer()
        {
            return _throwOrder.Current;
        }

        private void DeclareLeader()
        {
            string leader = GetCurrentPlayer();
            if (string.IsNullOrEmpty(leader))
            {
                return;
            }

            _throwOrder.DeclareLeader(leader);
            if (HistoryText != null)
            {
                HistoryText.text += "\n• " + leader + " va primero en la carrera. Tira primero al cerrar esta vuelta de turnos.";
            }

            int shot = RaceTurnController.Instance != null ? RaceTurnController.Instance.CurrentShot : 1;
            UpdateUI(shot);
        }

        private void BindProjection()
        {
            TrackWaypointProjection found = FindObjectOfType<TrackWaypointProjection>();
            if (_projection == found)
            {
                return;
            }

            if (_projection != null)
            {
                _projection.OnTrackProjected -= RefreshRules;
            }

            _projection = found;
            if (_projection != null)
            {
                _projection.OnTrackProjected += RefreshRules;
            }
        }

        private void RefreshRules()
        {
            EnsureRuleRoot();
            if (TrackNameText != null)
            {
                TrackNameText.text = _projection != null && !string.IsNullOrEmpty(_projection.ActiveTrackName)
                    ? _projection.ActiveTrackName
                    : "Sin pista";
            }

            ClearRuleButtons();
            DefinedTrack track = _projection != null ? _projection.CurrentTrack : null;
            bool exact = false;
            bool pit = false;
            bool longShot = false;
            bool lava = false;
            bool ice = false;
            bool rain = false;
            bool trap1 = false;
            bool trap2 = false;
            bool trap3 = false;
            if (track != null && track.Waypoints != null)
            {
                for (int i = 0; i < track.Waypoints.Length; i++)
                {
                    TrackWaypoint point = track.Waypoints[i];
                    if (point.Rule == WaypointRule.ExactShot) exact = true;
                    else if (point.Rule == WaypointRule.Pit) pit = true;
                    else if (point.Rule == WaypointRule.LongShot) longShot = true;
                    else if (point.Rule == WaypointRule.Lava) lava = true;
                    else if (point.Rule == WaypointRule.Ice) ice = true;
                    else if (point.Rule == WaypointRule.Rain) rain = true;
                    else if (point.Rule == WaypointRule.TensionTrap)
                    {
                        int shots = Mathf.Clamp(point.RuleValue, 1, 3);
                        if (shots == 1) trap1 = true;
                        else if (shots == 2) trap2 = true;
                        else trap3 = true;
                    }
                }
            }

            if (exact) AddRuleButton("EXACTO", ConfirmExact);
            if (pit) AddRuleButton("FOSO", ConfirmPit);
            if (longShot) AddRuleButton("TIRO LARGO", ConfirmLongShot);
            if (trap1) AddRuleButton("TRAMPA 1", () => ConfirmTrap(1));
            if (trap2) AddRuleButton("TRAMPA 2", () => ConfirmTrap(2));
            if (trap3) AddRuleButton("TRAMPA 3", () => ConfirmTrap(3));
            if (lava) AddRuleButton("LAVA", () => ConfirmDanger(true));
            if (ice) AddRuleButton("HIELO", () => ConfirmDanger(false));
            if (rain) AddRuleButton("LLUVIA", ConfirmRain);
            AddRuleButton("VA PRIMERO", DeclareLeader);
            AddRuleButton("TRÁFICO", ConfirmTraffic);
            AddRuleButton("TAKEDOWN", ConfirmTakedown);
            AddRuleButton("REBUFO", ConfirmDraft);
        }

        private void ConfirmExact()
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.ConfirmExactShot();
        }

        private void ConfirmPit()
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.ApplyPit();
        }

        private void ConfirmLongShot()
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.GrantLongShot();
        }

        private void ConfirmTrap(int shots)
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.ApplyTensionTrap(shots);
        }

        private void ConfirmDanger(bool lava)
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.ApplyDanger(lava);
        }

        private void ConfirmRain()
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.ApplyRain();
        }

        private void ConfirmTraffic()
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.ApplyTraffic();
        }

        private void ConfirmTakedown()
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.ApplyTakedown();
        }

        private void ConfirmDraft()
        {
            if (RaceTurnController.Instance) RaceTurnController.Instance.ApplyDraft();
        }

        private void EnsureRuleRoot()
        {
            if (RuleButtonRoot != null)
            {
                return;
            }

            GameObject root = new GameObject("RuleButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            root.transform.SetParent(transform, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 520f);
            rect.anchoredPosition = new Vector2(620f, 0f);
            VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            RuleButtonRoot = root.transform;
        }

        private void ClearRuleButtons()
        {
            if (RuleButtonRoot == null)
            {
                return;
            }

            List<GameObject> children = new List<GameObject>();
            for (int i = 0; i < RuleButtonRoot.childCount; i++)
            {
                children.Add(RuleButtonRoot.GetChild(i).gameObject);
            }

            for (int i = 0; i < children.Count; i++)
            {
                if (Application.isPlaying) Destroy(children[i]);
                else DestroyImmediate(children[i]);
            }
        }

        private void AddRuleButton(string label, UnityEngine.Events.UnityAction action)
        {
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                {
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }
            }

            GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(RuleButtonRoot, false);
            buttonObject.GetComponent<LayoutElement>().preferredHeight = 46f;
            buttonObject.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.22f, 0.95f);
            buttonObject.GetComponent<Button>().onClick.AddListener(action);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.font = _font;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;
        }
    }
}
