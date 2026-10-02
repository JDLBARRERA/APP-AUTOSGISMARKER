using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ARTrackBuilder.AR;
using ARTrackBuilder.Competition;
using ARTrackBuilder.Core;
using ARTrackBuilder.Debugging;
using ARTrackBuilder.UI;

namespace ARTrackBuilder.Editor
{
    public static class ARMasterSetup
    {
        [MenuItem("ARTrackBuilder/Configurar Escena Completa (1-Click)")]
        public static void ConfigureActiveScene()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject managers = FindOrCreate("AR_Managers");
            TrackProjectorManager projector = FindOrAdd<TrackProjectorManager>(managers);
            FindOrAdd<RaceTurnController>(managers);
            VAROverlayController varOverlay = FindOrAdd<VAROverlayController>(managers);
            FindOrAdd<DisplayModeManager>(managers);
            TrackWaypointProjection waypoints = FindOrAdd<TrackWaypointProjection>(managers);
            FindOrAdd<FloorReliefScanner>(managers);
            FindOrAdd<LandscapeDirector>(managers);
            FindOrAdd<TrackChalkRibbon>(waypoints.gameObject);
            FindOrAdd<LandscapeGround>(waypoints.gameObject);
            FindOrAdd<SlotArchive>(managers);
            FindOrAdd<SlotSessionController>(managers);
            FindOrAdd<VoiceCommentator>(managers);

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }

            GameObject canvasObject = FindOrCreate("Canvas_Dashboard");
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = canvasObject.AddComponent<Canvas>();
            }

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvasObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            if (canvasObject.GetComponent<GraphicRaycaster>() == null)
            {
                canvasObject.AddComponent<GraphicRaycaster>();
            }

            FindOrAdd<LandscapeUIManager>(canvasObject);
            FindOrAdd<SlotBroadcastUI>(canvasObject);
            RaceDashboardUIManager dashboard = BuildDashboard(canvasObject.transform, font);
            dashboard.TrackNameText = EnsureLabel(canvasObject.transform, "Text_TrackName", "Sin pista", font, 22, new Vector2(500f, 300f), new Vector2(340f, 48f), Color.white);
            dashboard.RuleButtonRoot = EnsureRuleColumn(canvasObject.transform);
            EditorUtility.SetDirty(dashboard);
            Button nextRace = EnsureButton(canvasObject.transform, "Button_SiguienteCarrera", "SIGUIENTE CARRERA", font, new Vector2(480f, 360f), new Vector2(340f, 64f), new Color(0.1f, 0.35f, 0.7f, 1f), out _);
            WireOnce(nextRace, waypoints.SiguienteCarrera);
            Assign(varOverlay, "_shotCountText", dashboard.ActionButtonText);

            GameObject harness = FindOrCreate("Local_Debug_Harness");
            ARDebugMockController mock = FindOrAdd<ARDebugMockController>(harness);
            LocalTestOverlay overlay = FindOrAdd<LocalTestOverlay>(harness);
            Assign(mock, "_projectorManager", projector);
            Assign(mock, "_varController", varOverlay);
            Assign(overlay, "_mockController", mock);

            EditorSceneManager.MarkSceneDirty(canvasObject.scene);
            EditorUtility.DisplayDialog("Escena lista", "AR_Managers, Canvas_Dashboard y Local_Debug_Harness quedaron enlazados. Pulsa Play.", "OK");
        }

        private static RaceDashboardUIManager BuildDashboard(Transform canvas, Font font)
        {
            GameObject panel = FindOrCreateChild(canvas, "Panel_Dashboard", typeof(RectTransform), typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(520f, 760f);
            panelRect.anchoredPosition = new Vector2(-680f, 40f);
            panel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.88f);

            RaceDashboardUIManager manager = panel.GetComponent<RaceDashboardUIManager>();
            if (manager == null)
            {
                manager = panel.AddComponent<RaceDashboardUIManager>();
            }

            manager.CurrentPlayerText = EnsureLabel(panel.transform, "Text_CurrentPlayer", "TURNO: Jugador 1", font, 26, new Vector2(0f, 300f), new Vector2(480f, 48f), Color.white);
            manager.NextPlayerText = EnsureLabel(panel.transform, "Text_NextPlayer", "SIGUIENTE: Jugador 2", font, 20, new Vector2(0f, 250f), new Vector2(480f, 36f), new Color(0.75f, 0.85f, 0.95f, 1f));
            manager.ShotCards = new Image[3];
            manager.ShotCardTexts = new Text[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject card = FindOrCreateChild(panel.transform, "Card_Tiro_" + (i + 1), typeof(RectTransform), typeof(Image));
                RectTransform cardRect = card.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(140f, 130f);
                cardRect.anchoredPosition = new Vector2(-155f + (i * 155f), 120f);
                Image cardImage = card.GetComponent<Image>();
                cardImage.color = Color.white;
                manager.ShotCards[i] = cardImage;
                manager.ShotCardTexts[i] = EnsureLabel(card.transform, "Text", "TIRO " + (i + 1), font, 18, Vector2.zero, new Vector2(120f, 40f), Color.black);
            }

            manager.ActionButton = EnsureButton(panel.transform, "Button_YaTire", "¡YA TIRÉ!", font, new Vector2(0f, -40f), new Vector2(440f, 72f), new Color(0.15f, 0.45f, 0.95f, 1f), out manager.ActionButtonText);
            manager.PassTurnButton = EnsureButton(panel.transform, "Button_PasarTurno", "PASAR TURNO", font, new Vector2(0f, -130f), new Vector2(440f, 52f), new Color(0.12f, 0.12f, 0.12f, 0.9f), out _);
            manager.HistoryText = EnsureLabel(panel.transform, "Text_History", "Historial", font, 18, new Vector2(0f, -280f), new Vector2(460f, 160f), Color.white);
            manager.HistoryText.alignment = TextAnchor.UpperLeft;
            EditorUtility.SetDirty(manager);
            return manager;
        }

        private static Transform EnsureRuleColumn(Transform canvas)
        {
            GameObject column = FindOrCreateChild(canvas, "RuleButtons", typeof(RectTransform), typeof(VerticalLayoutGroup));
            RectTransform rect = column.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 520f);
            rect.anchoredPosition = new Vector2(500f, -40f);
            VerticalLayoutGroup layout = column.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            return column.transform;
        }

        private static void WireOnce(Button button, UnityEngine.Events.UnityAction action)
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == "SiguienteCarrera")
                {
                    return;
                }
            }

            UnityEventTools.AddVoidPersistentListener(button.onClick, action);
        }

        private static GameObject FindOrCreate(string name)
        {
            GameObject found = GameObject.Find(name);
            return found != null ? found : new GameObject(name);
        }

        private static GameObject FindOrCreateChild(Transform parent, string name, params System.Type[] components)
        {
            Transform child = parent.Find(name);
            if (child != null)
            {
                return child.gameObject;
            }

            GameObject created = new GameObject(name, components);
            created.transform.SetParent(parent, false);
            return created;
        }

        private static T FindOrAdd<T>(GameObject host) where T : Component
        {
            T existing = host.GetComponent<T>();
            return existing != null ? existing : host.AddComponent<T>();
        }

        private static void Assign(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Text EnsureLabel(Transform parent, string name, string value, Font font, int size, Vector2 position, Vector2 sizeDelta, Color color)
        {
            GameObject textObject = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(Text));
            if (textObject.transform.parent != parent)
            {
                textObject.transform.SetParent(parent, false);
            }

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = value;
            return text;
        }

        private static Button EnsureButton(Transform parent, string name, string label, Font font, Vector2 position, Vector2 size, Color color, out Text labelText)
        {
            GameObject buttonObject = FindOrCreateChild(parent, name, typeof(RectTransform), typeof(Image), typeof(Button));
            if (buttonObject.transform.parent != parent)
            {
                buttonObject.transform.SetParent(parent, false);
            }

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            buttonObject.GetComponent<Image>().color = color;
            labelText = EnsureLabel(buttonObject.transform, "Text", label, font, 22, Vector2.zero, size, Color.white);
            return buttonObject.GetComponent<Button>();
        }
    }
}
