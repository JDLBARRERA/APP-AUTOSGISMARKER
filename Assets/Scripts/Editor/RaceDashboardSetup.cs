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
                CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
            }

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject panel = new GameObject("Panel_Dashboard", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(520f, 760f);
            panelRect.anchoredPosition = new Vector2(-680f, 40f);
            panel.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.1f, 0.88f);

            RaceDashboardUIManager manager = panel.AddComponent<RaceDashboardUIManager>();
            manager.CurrentPlayerText = CreateLabel(panel.transform, "Text_CurrentPlayer", "TURNO: Jugador 1", font, 26, new Vector2(0f, 300f), new Vector2(480f, 48f), Color.white);
            manager.NextPlayerText = CreateLabel(panel.transform, "Text_NextPlayer", "SIGUIENTE: Jugador 2", font, 20, new Vector2(0f, 250f), new Vector2(480f, 36f), new Color(0.75f, 0.85f, 0.95f, 1f));

            manager.ShotCards = new Image[3];
            manager.ShotCardTexts = new Text[3];

            for (int i = 0; i < 3; i++)
            {
                GameObject card = new GameObject($"Card_Tiro_{i + 1}", typeof(RectTransform), typeof(Image));
                card.transform.SetParent(panel.transform, false);
                RectTransform cardRect = card.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(140f, 130f);
                cardRect.anchoredPosition = new Vector2(-155f + (i * 155f), 120f);
                manager.ShotCards[i] = card.GetComponent<Image>();
                manager.ShotCards[i].color = Color.white;

                GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
                textObj.transform.SetParent(card.transform, false);
                RectTransform textRect = textObj.GetComponent<RectTransform>();
                textRect.sizeDelta = new Vector2(120f, 40f);
                Text t = textObj.GetComponent<Text>();
                t.font = font;
                t.text = $"TIRO {i + 1}";
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.black;
                t.fontSize = 18;
                manager.ShotCardTexts[i] = t;
            }

            manager.ActionButton = CreateButton(panel.transform, "Button_YaTire", "¡YA TIRÉ!", font, new Vector2(0f, -40f), new Vector2(440f, 72f), new Color(0.15f, 0.45f, 0.95f, 1f), out manager.ActionButtonText);
            manager.PassTurnButton = CreateButton(panel.transform, "Button_PasarTurno", "PASAR TURNO", font, new Vector2(0f, -130f), new Vector2(440f, 52f), new Color(0.12f, 0.12f, 0.12f, 0.9f), out _);
            manager.HistoryText = CreateLabel(panel.transform, "Text_History", "Historial", font, 18, new Vector2(0f, -280f), new Vector2(460f, 160f), Color.white);
            manager.HistoryText.alignment = TextAnchor.UpperLeft;

            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.SaveScene(canvas.gameObject.scene, "Assets/Scenes/DashboardScene.unity");
            EditorUtility.DisplayDialog("Éxito", "UI del Dashboard generada y vinculada en el Canvas.", "OK");
        }

        private static Text CreateLabel(Transform parent, string name, string value, Font font, int size, Vector2 position, Vector2 sizeDelta, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            textObject.GetComponent<RectTransform>().sizeDelta = sizeDelta;
            textObject.GetComponent<RectTransform>().anchoredPosition = position;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = value;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, string label, Font font, Vector2 position, Vector2 size, Color color, out Text labelText)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            buttonObject.GetComponent<Image>().color = color;
            Button button = buttonObject.GetComponent<Button>();

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(buttonObject.transform, false);
            textObject.GetComponent<RectTransform>().sizeDelta = size;
            labelText = textObject.GetComponent<Text>();
            labelText.font = font;
            labelText.text = label;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.fontSize = 22;
            return button;
        }
    }
}
