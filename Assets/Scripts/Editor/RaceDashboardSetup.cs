using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using ARTrackBuilder.UI;

namespace ARTrackBuilder.Editor
{
    public class RaceDashboardSetup
    {
        [MenuItem("ARTrackBuilder/Generar UI Dashboard")]
        public static void BuildDashboardUI()
        {
            Canvas canvas = FindOrCreateDashboardCanvas();

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            Transform previous = canvas.transform.Find("Panel_Dashboard");
            if (previous != null)
            {
                Object.DestroyImmediate(previous.gameObject);
            }

            GameObject panel = new GameObject("Panel_Dashboard", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = new Vector2(0.3f, 1f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            panelRect.pivot = new Vector2(0f, 0.5f);
            panel.GetComponent<Image>().color = new Color(0.102f, 0.102f, 0.102f, 1f);

            VerticalLayoutGroup column = panel.GetComponent<VerticalLayoutGroup>();
            column.childAlignment = TextAnchor.UpperCenter;
            column.spacing = 30f;
            column.padding = new RectOffset(20, 20, 100, 20);
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            RaceDashboardUIManager manager = panel.AddComponent<RaceDashboardUIManager>();
            manager.CurrentPlayerText = CreateLabel(panel.transform, "Text_CurrentPlayer", "TURNO: Jugador 1", font, 36, new Vector2(480f, 56f), Color.white);
            manager.NextPlayerText = CreateLabel(panel.transform, "Text_NextPlayer", "SIGUIENTE: Jugador 2", font, 30, new Vector2(480f, 48f), new Color(0.75f, 0.85f, 0.95f, 1f));

            GameObject shotRow = new GameObject("ShotCards", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            shotRow.transform.SetParent(panel.transform, false);
            HorizontalLayoutGroup row = shotRow.GetComponent<HorizontalLayoutGroup>();
            row.childAlignment = TextAnchor.MiddleCenter;
            row.spacing = 16f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = false;
            LayoutElement shotLayout = shotRow.GetComponent<LayoutElement>();
            shotLayout.minWidth = 120f;
            shotLayout.minHeight = 80f;
            shotLayout.preferredHeight = 150f;

            manager.ShotCards = new Image[3];
            manager.ShotCardTexts = new Text[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject card = new GameObject($"Card_Tiro_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                card.transform.SetParent(shotRow.transform, false);
                LayoutElement cardLayout = card.GetComponent<LayoutElement>();
                cardLayout.minWidth = 120f;
                cardLayout.minHeight = 80f;
                cardLayout.preferredHeight = 140f;
                manager.ShotCards[i] = card.GetComponent<Image>();
                manager.ShotCards[i].color = Color.white;
                Button cardButton = card.GetComponent<Button>();
                cardButton.targetGraphic = manager.ShotCards[i];
                cardButton.transition = Selectable.Transition.None;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(card.transform, false);
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                Text t = textObj.GetComponent<Text>();
                t.font = font;
                t.text = $"TIRO {i + 1}";
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.black;
                t.fontSize = 30;
                t.resizeTextForBestFit = true;
                t.resizeTextMinSize = 10;
                t.resizeTextMaxSize = 30;
                manager.ShotCardTexts[i] = t;
            }

            manager.ActionButton = CreateButton(panel.transform, "Button_YaTire", "¡YA TIRÉ!", font, new Vector2(440f, 80f), new Color(0.15f, 0.45f, 0.95f, 1f), out manager.ActionButtonText);
            manager.PassTurnButton = CreateButton(panel.transform, "Button_PasarTurno", "PASAR TURNO", font, new Vector2(440f, 64f), new Color(0.12f, 0.12f, 0.12f, 0.9f), out _);
            CreateRefereePanel(panel.transform, font, manager);
            manager.HistoryText = CreateLabel(panel.transform, "Text_History", "Historial", font, 30, new Vector2(460f, 180f), Color.white);
            manager.HistoryText.alignment = TextAnchor.UpperLeft;
            manager.HistoryText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            Transform trackName = FindChildOnAnyCanvas("Text_TrackName");
            if (trackName != null)
            {
                manager.TrackNameText = trackName.GetComponent<Text>();
            }

            Transform ruleRoot = FindChildOnAnyCanvas("RuleButtons");
            if (ruleRoot != null)
            {
                manager.RuleButtonRoot = ruleRoot;
            }

            DisableCanvasesWithoutDashboard();

            UnityEngine.SceneManagement.Scene scene = canvas.gameObject.scene;
            if (string.IsNullOrEmpty(scene.path))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                {
                    AssetDatabase.CreateFolder("Assets", "Scenes");
                }

                EditorSceneManager.SaveScene(scene, "Assets/Scenes/DashboardScene.unity");
            }
            else
            {
                EditorSceneManager.SaveScene(scene);
            }

            EditorUtility.DisplayDialog("Éxito", "UI del Dashboard generada y vinculada en el Canvas.", "OK");
        }

        private static void CreateRefereePanel(Transform parent, Font font, RaceDashboardUIManager manager)
        {
            CreateLabel(parent, "Text_Arbitro", "PANEL DE ÁRBITRO", font, 20, new Vector2(440f, 32f), Color.white);

            GameObject gridObject = new GameObject("Panel_Arbitro", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
            gridObject.transform.SetParent(parent, false);
            LayoutElement gridLayout = gridObject.GetComponent<LayoutElement>();
            gridLayout.preferredWidth = 330f;
            gridLayout.preferredHeight = 110f;
            gridLayout.minHeight = 110f;

            GridLayoutGroup grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(160f, 50f);
            grid.spacing = new Vector2(10f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperCenter;

            WireRefereeButton(gridObject.transform, "Button_Turbo", "🔥 TURBO", font, new Color(0.15f, 0.9f, 0.25f, 1f), manager.ApplyTurbo, Color.white);
            WireRefereeButton(gridObject.transform, "Button_Exacto", "🎯 EXACTO", font, new Color(0.95f, 0.82f, 0.1f, 1f), manager.ApplyExactShot, Color.black);
            WireRefereeButton(gridObject.transform, "Button_Trampa", "🧊 TRAMPA", font, new Color(0.85f, 0.12f, 0.12f, 1f), manager.ApplyTrap, Color.white);
            WireRefereeButton(gridObject.transform, "Button_Choque", "💥 CHOQUE", font, new Color(0.95f, 0.45f, 0.08f, 1f), manager.ApplyCrash, Color.white);
        }

        private static void WireRefereeButton(Transform parent, string name, string label, Font font, Color color, UnityEngine.Events.UnityAction action, Color textColor)
        {
            Button button = CreateButton(parent, name, label, font, new Vector2(160f, 50f), color, out Text labelText);
            labelText.fontSize = 18;
            labelText.color = textColor;
            labelText.resizeTextForBestFit = true;
            labelText.resizeTextMinSize = 10;
            labelText.resizeTextMaxSize = 18;
            UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static Text CreateLabel(Transform parent, string name, string value, Font font, int size, Vector2 sizeDelta, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            textObject.transform.SetParent(parent, false);
            textObject.GetComponent<LayoutElement>().preferredHeight = sizeDelta.y;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 size, Color color, out Text labelText)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<LayoutElement>().preferredHeight = size.y;
            buttonObject.GetComponent<Image>().color = color;
            Button button = buttonObject.GetComponent<Button>();

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            labelText = textObject.GetComponent<Text>();
            labelText.font = font;
            labelText.text = label;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.fontSize = 32;
            return button;
        }

        private static Canvas FindOrCreateDashboardCanvas()
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].gameObject.name.Contains("Dashboard"))
                {
                    canvases[i].gameObject.SetActive(true);
                    return canvases[i];
                }
            }

            GameObject canvasObject = new GameObject("Canvas_Dashboard", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        private static void DisableCanvasesWithoutDashboard()
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                if (KeepCanvas(canvases[i].gameObject.name))
                {
                    continue;
                }

                canvases[i].gameObject.SetActive(false);
            }
        }

        private static bool KeepCanvas(string canvasName)
        {
            if (canvasName.Contains("Dashboard"))
            {
                return true;
            }

            return canvasName == "Canvas" || canvasName == "MainCanvas" || canvasName == "Main_Dashboard_Canvas";
        }

        /// <summary>
        /// Quita el gestor vacío, apunta la UI al tablero configurado y crea la rejilla del árbitro si falta.
        /// </summary>
        public static void RepairDashboardFindings()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            RaceDashboardUIManager[] dashboards = Object.FindObjectsOfType<RaceDashboardUIManager>(true);
            RaceDashboardUIManager active = null;
            for (int i = 0; i < dashboards.Length; i++)
            {
                if (dashboards[i].gameObject.name == "Panel_Dashboard")
                {
                    active = dashboards[i];
                    break;
                }
            }

            for (int i = 0; i < dashboards.Length; i++)
            {
                if (active != null && dashboards[i] != active && dashboards[i].gameObject.name == "AR_Managers")
                {
                    Object.DestroyImmediate(dashboards[i]);
                }
            }

            if (active != null)
            {
                RaceUIManager[] raceUis = Object.FindObjectsOfType<RaceUIManager>(true);
                for (int i = 0; i < raceUis.Length; i++)
                {
                    SerializedObject serialized = new SerializedObject(raceUis[i]);
                    serialized.FindProperty("_dashboard").objectReferenceValue = active;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                if (active.transform.Find("Panel_Arbitro") == null)
                {
                    Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (font == null)
                    {
                        font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    }

                    CreateRefereePanel(active.transform, font, active);
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        private static Transform FindChildOnAnyCanvas(string childName)
        {
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Transform child = canvases[i].transform.Find(childName);
                if (child != null)
                {
                    return child;
                }
            }

            return null;
        }
    }
}
