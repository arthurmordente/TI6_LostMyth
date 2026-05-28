using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui.Shared
{
    public abstract class UguiCanvasViewBase : MonoBehaviour
    {
        [SerializeField] private GameObject _rootPanel;
        [SerializeField] private CanvasGroup _canvasGroup;

        /// <summary>Subclasses may override for runtime UI setup. Do not auto-hide here — use <c>InitEntryPoint</c> or <see cref="HideUntilOpened"/>.</summary>
        protected virtual void Awake() { }

        /// <summary>Oculta o painel até <see cref="Show"/>. Funciona mesmo com o GameObject inativo (sem depender de Awake).</summary>
        public void HideUntilOpened() => Hide();

        public virtual void Show()
        {
            if (_rootPanel != null)
                _rootPanel.SetActive(true);
            else
                gameObject.SetActive(true);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            transform.localScale = Vector3.one;
        }

        public virtual void Hide()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            if (_rootPanel != null)
                _rootPanel.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        protected void HideImmediate() => Hide();

        public virtual bool IsVisible
        {
            get
            {
                if (_rootPanel != null)
                    return _rootPanel.activeInHierarchy;
                return gameObject.activeInHierarchy;
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
