using System.Threading;
using DG.Tweening;
using Logic.Scripts.Extensions;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.Core.Mvc.LoadingScreen
{
    public sealed class LoadingScreenCanvasView : UguiCanvasViewBase
    {
        private const int ZeroInt = 0;

        [SerializeField] private Image _fillImage;
        [SerializeField] private float _animationDuration = 0.5f;
        [SerializeField] private Ease _animationEase = Ease.OutQuad;

        private Tween _currentAnimationTween;

        protected override void Awake()
        {
            EnsureRuntimeFillIfNeeded();
            base.Awake();
        }

        public void InitPoint()
        {
            if (_fillImage != null)
                _fillImage.fillAmount = 0f;
        }

        public void ResetSlider()
        {
            _currentAnimationTween?.Kill();
            if (_fillImage != null)
                _fillImage.fillAmount = 0f;
        }

        public async Awaitable SetLoadingSlider(float valueBetween0To1, CancellationTokenSource cancellationTokenSource)
        {
            _currentAnimationTween?.Kill();
            if (_fillImage == null) return;

            var target = Mathf.Clamp01(valueBetween0To1);
            _currentAnimationTween = _fillImage
                .DOFillAmount(target, _animationDuration)
                .SetEase(_animationEase);
            await _currentAnimationTween.WithCancellationSafe(cancellationToken: cancellationTokenSource.Token);
        }

        private void EnsureRuntimeFillIfNeeded()
        {
            if (_fillImage != null) return;

            var barRoot = new GameObject("LoadingBar", typeof(RectTransform), typeof(Image));
            barRoot.transform.SetParent(transform, false);
            var rt = barRoot.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.45f);
            rt.anchorMax = new Vector2(0.9f, 0.55f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _fillImage = barRoot.GetComponent<Image>();
            _fillImage.color = new Color(0.4f, 0.15f, 0.6f, 1f);
            _fillImage.type = Image.Type.Filled;
            _fillImage.fillMethod = Image.FillMethod.Horizontal;
            _fillImage.fillAmount = 0f;
        }
    }
}
