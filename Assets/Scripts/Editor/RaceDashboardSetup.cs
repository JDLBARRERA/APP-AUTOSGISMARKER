using UnityEditor;
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
            Canvas canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

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
            column.padding = new RectOffset(20, 20, 20, 20);
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
            shotRow.GetComponent<LayoutElement>().preferredHeight = 150f;

            manager.ShotCards = new Image[3];
            manager.ShotCardTexts = new Text[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject card = new GameObject($"Card_Tiro_{i + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                card.transform.SetParent(shotRow.transform, false);
                card.GetComponent<LayoutElement>().preferredHeight = 140f;
                manager.ShotCards[i] = card.GetComponent<Image>();
                manager.ShotCards[i].color = Color.white;

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
                t.fontSize = 32;
                manager.ShotCardTexts[i] = t;
            }

            manager.ActionButton = CreateButton(panel.transform, "Button_YaTire", "¡YA TIRÉ!", font, new Vector2(440f, 80f), new Color(0.15f, 0.45f, 0.95f, 1f), out manager.ActionButtonText);
            manager.PassTurnButton = CreateButton(panel.transform, "Button_PasarTurno", "PASAR TURNO", font, new Vector2(440f, 64f), new Color(0.12f, 0.12f, 0.12f, 0.9f), out _);
            manager.HistoryText = CreateLabel(panel.transform, "Text_History", "Historial", font, 30, new Vector2(460f, 180f), Color.white);
            manager.HistoryText.alignment = TextAnchor.UpperLeft;
            manager.HistoryText.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            Transform trackName = canvas.transform.Find("Text_TrackName");
            if (trackName != null)
            {
                manager.TrackNameText = trackName.GetComponent<Text>();
            }

            Transform ruleRoot = canvas.transform.Find("RuleButtons");
            if (ruleRoot != null)
            {
                manager.RuleButtonRoot = ruleRoot;
            }

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
    }
}
