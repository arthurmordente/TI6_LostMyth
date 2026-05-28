using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui.Shared
{
    public abstract class UguiCanvasViewBase : MonoBehaviour
    {
        [SerializeField] private GameObject _rootPanel;
        [SerializeField] private CanvasGroup _canvasGroup;

        protected virtual void Awake()
        {
            HideImmediate();
        }

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

        protected static Button ResolveButton(Button serialized, Transform root, string childName)
        {
            if (serialized != null) return serialized;
            if (root == null) return null;
            var t = root.Find(childName);
            return t != null ? t.GetComponent<Button>() : null;
        }
    }
}
