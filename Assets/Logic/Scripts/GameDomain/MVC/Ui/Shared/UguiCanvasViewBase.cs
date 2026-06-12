using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui.Shared
{
    public abstract class UguiCanvasViewBase : MonoBehaviour
    {
        [SerializeField] private GameObject _rootPanel;
        [SerializeField] private CanvasGroup _canvasGroup;

        void EnsureCanvasGroup()
        {
            if (_canvasGroup != null) return;
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>Subclasses may override for runtime UI setup. Do not auto-hide here — use <c>InitEntryPoint</c> or <see cref="HideUntilOpened"/>.</summary>
        protected virtual void Awake() { }

        /// <summary>Oculta o painel até <see cref="Show"/>. Funciona mesmo com o GameObject inativo (sem depender de Awake).</summary>
        public void HideUntilOpened() => Hide();

        public virtual void Show()
        {
            NormalizeRootPanelReference();
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;

            if (UsesSeparateRootPanel())
                _rootPanel.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }
        }

        public virtual void Hide()
        {
            NormalizeRootPanelReference();

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (UsesSeparateRootPanel())
                _rootPanel.SetActive(false);

            gameObject.SetActive(false);
        }

        protected void HideImmediate() => Hide();

        protected async Awaitable FadeInCanvasAsync(float durationSeconds)
        {
            NormalizeRootPanelReference();
            gameObject.SetActive(true);
            transform.localScale = Vector3.one;

            if (UsesSeparateRootPanel())
                _rootPanel.SetActive(true);

            EnsureCanvasGroup();

            durationSeconds = Mathf.Max(0.01f, durationSeconds);
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            float elapsed = 0f;
            while (elapsed < durationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                _canvasGroup.alpha = Mathf.Clamp01(elapsed / durationSeconds);
                await Awaitable.NextFrameAsync();
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        public virtual bool IsVisible
        {
            get
            {
                if (!gameObject.activeInHierarchy)
                    return false;
                if (UsesSeparateRootPanel())
                    return _rootPanel.activeSelf;
                return true;
            }
        }

        bool UsesSeparateRootPanel() =>
            _rootPanel != null && _rootPanel != gameObject;

        /// <summary>
        /// Prefabs: assign a child "Panel" as Root Panel (not the canvas root).
        /// If Root Panel points at the canvas root, we treat the whole overlay canvas as the panel.
        /// </summary>
        void NormalizeRootPanelReference()
        {
            if (_rootPanel != null && _rootPanel != gameObject)
                return;

            _rootPanel = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                if (child.name == "Panel")
                {
                    _rootPanel = child.gameObject;
                    return;
                }
            }
        }

        protected static Button ResolveButton(Button serialized, Transform root, string childName)
        {
            if (serialized != null) return serialized;
            if (root == null) return null;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (button.name == childName)
                    return button;
            }
            return null;
        }
    }
}
