using System.Threading;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using Logic.Scripts.Services.Logger.Base;
using Logic.Scripts.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Logic.Scripts.Core.Mvc.LoadingScreen
{
    public sealed class LoadingScreenCanvasView : UguiCanvasViewBase
    {
        [SerializeField] private Transform _tipContainer;

        private LoadingTipPoolSO _tipPool;
        private LoadingTipCanvasView _activeTip;
        private GameObject _activeTipInstance;
        private bool _continuePromptEnabled;

        protected override void Awake()
        {
            EnsureTipContainerIfNeeded();
            base.Awake();
        }

        public void InitPoint(LoadingTipPoolSO tipPool)
        {
            _tipPool = tipPool;
        }

        public void ShowTransitionTip()
        {
            ClearActiveTip();
            _continuePromptEnabled = false;
            Show();

            var tipPrefab = _tipPool != null ? _tipPool.PickRandom() : null;
            if (tipPrefab != null)
            {
                _activeTipInstance = Instantiate(tipPrefab.gameObject, _tipContainer);
                _activeTip = _activeTipInstance.GetComponent<LoadingTipCanvasView>();
                StretchToParent(_activeTipInstance.transform as RectTransform);
                _activeTip?.OnSpawned();
                return;
            }

            LogService.LogError("[LoadingScreen] Tip pool is empty or not assigned. Using runtime fallback tip.");
            BuildRuntimeFallbackTip();
        }

        public void EnableContinuePrompt()
        {
            _continuePromptEnabled = true;
            _activeTip?.ShowContinuePrompt();
        }

        public async Awaitable WaitForPlayerContinue(CancellationTokenSource cancellationTokenSource)
        {
            await AwaitableUtils.WaitUntil(
                () => _continuePromptEnabled && IsAnyInputPressed(),
                cancellationTokenSource.Token);
        }

        public override void Hide()
        {
            ClearActiveTip();
            _continuePromptEnabled = false;
            base.Hide();
        }

        void ClearActiveTip()
        {
            if (_activeTipInstance != null)
            {
                Destroy(_activeTipInstance);
                _activeTipInstance = null;
                _activeTip = null;
            }
        }

        static bool IsAnyInputPressed()
        {
            return
                (Keyboard.current?.anyKey.wasPressedThisFrame == true) ||
                (Mouse.current?.leftButton.wasPressedThisFrame == true) ||
                (Mouse.current?.rightButton.wasPressedThisFrame == true) ||
                (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true);
        }

        void BuildRuntimeFallbackTip()
        {
            var root = new GameObject("RuntimeFallbackTip", typeof(RectTransform), typeof(LoadingTipCanvasView));
            root.transform.SetParent(_tipContainer, false);
            StretchToParent(root.GetComponent<RectTransform>());

            var panel = CreateUiObject("Panel", root.transform, typeof(Image));
            StretchToParent(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.75f);

            var tipText = CreateText("txt_Tip", panel.transform, "Dica: explore o lobby para começar a aventura.");
            var tipRect = tipText.GetComponent<RectTransform>();
            tipRect.anchorMin = new Vector2(0.1f, 0.35f);
            tipRect.anchorMax = new Vector2(0.9f, 0.85f);
            tipRect.offsetMin = Vector2.zero;
            tipRect.offsetMax = Vector2.zero;

            var continuePrompt = CreateUiObject("ContinuePrompt", panel.transform);
            continuePrompt.SetActive(false);
            var continueRect = continuePrompt.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.1f, 0.05f);
            continueRect.anchorMax = new Vector2(0.9f, 0.25f);
            continueRect.offsetMin = Vector2.zero;
            continueRect.offsetMax = Vector2.zero;

            CreateText("txt_Continue", continuePrompt.transform, "Pressione qualquer tecla para continuar");

            _activeTipInstance = root;
            _activeTip = root.GetComponent<LoadingTipCanvasView>();
            _activeTip.OnSpawned();
        }

        static GameObject CreateUiObject(string name, Transform parent, System.Type extraComponent = null)
        {
            var components = extraComponent != null
                ? new[] { typeof(RectTransform), extraComponent }
                : new[] { typeof(RectTransform) };
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        static GameObject CreateText(string name, Transform parent, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var uiText = go.GetComponent<Text>();
            uiText.text = text;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.white;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.resizeTextForBestFit = true;
            StretchToParent(go.GetComponent<RectTransform>());
            return go;
        }

        static void StretchToParent(RectTransform rectTransform)
        {
            if (rectTransform == null) return;
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        void EnsureTipContainerIfNeeded()
        {
            if (_tipContainer != null) return;

            var container = transform.Find("TipContainer");
            if (container != null)
            {
                _tipContainer = container;
                return;
            }

            var containerGo = new GameObject("TipContainer", typeof(RectTransform));
            containerGo.transform.SetParent(transform, false);
            StretchToParent(containerGo.GetComponent<RectTransform>());
            _tipContainer = containerGo.transform;
        }
    }
}
