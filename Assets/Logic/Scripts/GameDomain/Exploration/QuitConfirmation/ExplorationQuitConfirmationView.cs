using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.Exploration.QuitConfirmation
{
    /// <summary>Overlay uGUI criado em runtime para confirmação de saída na exploração.</summary>
    public sealed class ExplorationQuitConfirmationView : MonoBehaviour, IExplorationQuitConfirmationView
    {
        private IExplorationQuitConfirmationService _service;
        private GameObject _root;
        private bool _built;

        public bool IsVisible => _root != null && _root.activeSelf;

        public void BindService(IExplorationQuitConfirmationService service) => _service = service;

        public void Show()
        {
            EnsureBuilt();
            _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        private void Update()
        {
            if (!IsVisible || _service == null) return;
            _service.ProcessDismissInput();
        }

        private void EnsureBuilt()
        {
            if (_built) return;
            _built = true;
            EnsureEventSystemExists();

            var canvasRoot = new GameObject("ExplorationQuitConfirmationCanvas");
            canvasRoot.transform.SetParent(transform, false);
            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasRoot.AddComponent<GraphicRaycaster>();

            var dimmer = CreateUiObject("Dimmer", canvasRoot.transform);
            var dimmerImage = dimmer.AddComponent<Image>();
            dimmerImage.color = new Color(0f, 0f, 0f, 0.72f);
            dimmerImage.raycastTarget = true;
            StretchFull(dimmer.GetComponent<RectTransform>());

            var panel = CreateUiObject("Panel", dimmer.transform);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.14f, 0.96f);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(720f, 320f);

            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0.85f, 0.85f, 0.95f, 0.45f);
            outline.effectDistance = new Vector2(2f, -2f);

            CreateLabel(panel.transform, "Title", ExplorationQuitConfirmationCopy.Title,
                new Vector2(0.06f, 0.72f), new Vector2(0.94f, 0.94f), 30f, FontStyles.Bold);
            CreateLabel(panel.transform, "Message", ExplorationQuitConfirmationCopy.Message,
                new Vector2(0.06f, 0.38f), new Vector2(0.94f, 0.70f), 22f, FontStyles.Normal);
            CreateLabel(panel.transform, "ConfirmHint", ExplorationQuitConfirmationCopy.ConfirmHint,
                new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.36f), 18f, FontStyles.Italic);
            CreateLabel(panel.transform, "CancelHint", ExplorationQuitConfirmationCopy.CancelHint,
                new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.20f), 18f, FontStyles.Italic);

            _root = canvasRoot;
            _root.SetActive(false);
        }

        private static void EnsureEventSystemExists()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null) return;
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void CreateLabel(Transform parent, string name, string text, Vector2 anchorMin, Vector2 anchorMax,
            float fontSize, FontStyles style)
        {
            var go = CreateUiObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }
    }
}
