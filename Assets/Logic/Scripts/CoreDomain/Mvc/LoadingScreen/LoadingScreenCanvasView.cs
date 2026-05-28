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
    }
}
