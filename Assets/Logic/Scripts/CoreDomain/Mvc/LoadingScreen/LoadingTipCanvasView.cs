using UnityEngine;

namespace Logic.Scripts.Core.Mvc.LoadingScreen
{
    /// <summary>
    /// Root view for a loading-tip prefab. Visual content is built from arbitrary uGUI children
    /// under <see cref="_panelRoot"/> and <see cref="_tipContentRoot"/> (text, images, layout groups, etc.).
    /// Only <see cref="_continuePrompt"/> is toggled at runtime when the destination scene is ready.
    /// </summary>
    public sealed class LoadingTipCanvasView : MonoBehaviour
    {
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private GameObject _tipContentRoot;
        [SerializeField] private GameObject _continuePrompt;

        public GameObject PanelRoot => _panelRoot;
        public GameObject TipContentRoot => _tipContentRoot;
        public GameObject ContinuePrompt => _continuePrompt;

        public void OnSpawned()
        {
            ResolveReferencesIfNeeded();
            if (_continuePrompt != null)
                _continuePrompt.SetActive(false);
        }

        public void ShowContinuePrompt()
        {
            ResolveReferencesIfNeeded();
            if (_continuePrompt != null)
                _continuePrompt.SetActive(true);
        }

        void ResolveReferencesIfNeeded()
        {
            if (_panelRoot == null)
            {
                var panelTransform = transform.Find("Panel");
                if (panelTransform != null)
                    _panelRoot = panelTransform.gameObject;
            }

            if (_tipContentRoot == null)
            {
                var contentTransform = transform.Find("Panel/TipContent");
                if (contentTransform != null)
                    _tipContentRoot = contentTransform.gameObject;
            }

            if (_continuePrompt == null)
            {
                var promptTransform = transform.Find("Panel/ContinuePrompt");
                if (promptTransform != null)
                    _continuePrompt = promptTransform.gameObject;
            }
        }
    }
}
