using TMPro;
using UnityEngine;

namespace Logic.Scripts.Core.Mvc.LoadingScreen
{
    public sealed class LoadingTipCanvasView : MonoBehaviour
    {
        [SerializeField] private GameObject _continuePrompt;
        [SerializeField] private TextMeshProUGUI _tipText;

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
            if (_continuePrompt == null)
            {
                var promptTransform = transform.Find("Panel/ContinuePrompt");
                if (promptTransform != null)
                    _continuePrompt = promptTransform.gameObject;
            }

            if (_tipText == null)
            {
                var tipTransform = transform.Find("Panel/txt_Tip");
                if (tipTransform != null)
                    _tipText = tipTransform.GetComponent<TextMeshProUGUI>();
            }
        }
    }
}
