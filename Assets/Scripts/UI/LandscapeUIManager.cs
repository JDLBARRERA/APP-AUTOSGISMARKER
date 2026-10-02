using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ARTrackBuilder.AR;

namespace ARTrackBuilder.UI
{
    /// <summary>
    /// Botones de clima y relieve para que el proyector cambie el color de la pista.
    /// </summary>
    public class LandscapeUIManager : MonoBehaviour
    {
        [SerializeField] private Text _statusText;
        [SerializeField] private Transform _buttonRoot;

        private readonly Image[] _backgrounds = new Image[10];
        private readonly Text[] _labels = new Text[10];
        private Font _font;
        private LandscapeDirector _director;

        private void Start()
        {
            _director = LandscapeDirector.Instance != null ? LandscapeDirector.Instance : FindObjectOfType<LandscapeDirector>();
            if (_director != null)
            {
                _director.OnLandscapeChanged += Refresh;
            }

            BuildBar();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_director != null)
            {
                _director.OnLandscapeChanged -= Refresh;
            }
        }

        private void BuildBar()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            Transform canvas = EnsureCanvas();
            if (_statusText == null)
            {
                _statusText = CreateLabel(canvas, "Text_LandscapeStatus", "PAISAJE");
                RectTransform statusRect = _statusText.rectTransform;
                statusRect.anchoredPosition = new Vector2(180f, 86f);
                statusRect.sizeDelta = new Vector2(1280f, 36f);
            }

            if (_buttonRoot == null)
            {
                GameObject root = new GameObject("LandscapeButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                root.transform.SetParent(canvas, false);
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(180f, 16f);
                rect.sizeDelta = new Vector2(1280f, 64f);
                HorizontalLayoutGroup layout = root.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
                _buttonRoot = root.transform;
            }

            Clear(_buttonRoot);
            AddButton(0, "AUTO", () => { if (_director != null) _director.UseAutomatic(); });
            AddButton(1, "ASFALTO", () => { if (_director != null) _director.SetAsphalt(); });
            AddButton(2, "LLUVIA", () => { if (_director != null) _director.SetRain(); });
            AddButton(3, "HIELO", () => { if (_director != null) _director.SetIce(); });
            AddButton(4, "NIEVE", () => { if (_director != null) _director.SetSnow(); });
            AddButton(5, "LAVA", () => { if (_director != null) _director.SetLava(); });
            AddButton(6, "DESIERTO", () => { if (_director != null) _director.SetDesert(); });
            AddButton(7, "BOSQUE", () => { if (_director != null) _director.SetForest(); });
            AddButton(8, "NOCHE", () => { if (_director != null) _director.SetNight(); });
            AddButton(9, "RELIEVE", () => { if (_director != null) _director.CycleRelief(); });
            EnsureEventSystem();
        }

        private void Refresh()
        {
            if (_director == null || _statusText == null)
            {
                return;
            }

            _statusText.text = _director.StatusLine;
            LandscapePalette palette = _director.Palette;
            for (int i = 0; i < _backgrounds.Length; i++)
            {
                if (_backgrounds[i] == null)
                {
                    continue;
                }

                bool active = IsActive(i);
                _backgrounds[i].color = active ? palette.Ribbon : new Color(0.08f, 0.1f, 0.14f, 0.94f);
                if (_labels[i] != null)
                {
                    _labels[i].color = active && palette.Ribbon.grayscale > 0.65f ? Color.black : Color.white;
                }
            }
        }

        private bool IsActive(int index)
        {
            if (index == 0)
            {
                return _director.IsAutomatic;
            }

            if (index == 9)
            {
                return false;
            }

            return !_director.IsAutomatic && (int)_director.Mode == index - 1;
        }

        private void AddButton(int index, string label, UnityEngine.Events.UnityAction action)
        {
            GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            buttonObject.transform.SetParent(_buttonRoot, false);
            LayoutElement element = buttonObject.GetComponent<LayoutElement>();
            element.preferredHeight = 58f;
            element.minWidth = 120f;
            Image image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.08f, 0.1f, 0.14f, 0.94f);
            buttonObject.GetComponent<Button>().onClick.AddListener(action);
            _backgrounds[index] = image;
            _labels[index] = CreateLabel(buttonObject.transform, "Text", label);
            RectTransform textRect = _labels[index].rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;
            _labels[index].fontSize = 18;
        }

        private Text CreateLabel(Transform parent, string name, string value)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 86f);
            rect.sizeDelta = new Vector2(1400f, 36f);
            Text text = textObject.GetComponent<Text>();
            text.font = _font;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = value;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            return text;
        }

        private static void Clear(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Destroy(root.GetChild(i).gameObject);
            }
        }

        private Transform EnsureCanvas()
        {
            Canvas own = GetComponent<Canvas>();
            if (own != null)
            {
                return own.transform;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                return canvas.transform;
            }

            GameObject canvasObject = new GameObject("Canvas_Paisaje", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas created = canvasObject.GetComponent<Canvas>();
            created.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            return canvasObject.transform;
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
