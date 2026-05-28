using System.Threading;
using Logic.Scripts.Services.Logger.Base;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.Core.Mvc.LoadingScreen {
    public class LoadingScreenController : ILoadingScreenController {
        private readonly LoadingScreenCanvasView _loadingScreenView;

        [Inject]
        public LoadingScreenController(LoadingScreenCanvasView loadingScreenView) {
            _loadingScreenView = loadingScreenView;
        }

        public void SetupLoadingView() {
            _loadingScreenView.InitPoint();
        }

        public void Show() {
            LogService.LogTopic("Show loading screen", LogTopicType.LoadingScreen);
            _loadingScreenView.ResetSlider();
            _loadingScreenView.Show();
        }

        public void Hide() {
            LogService.LogTopic("Hide loading screen", LogTopicType.LoadingScreen);
            _loadingScreenView.Hide();
        }

        public void ResetSlider() {
            _loadingScreenView.ResetSlider();
        }

        public async Awaitable SetLoadingSlider(float valueBetween0To1, CancellationTokenSource cancellationTokenSource) {
            await _loadingScreenView.SetLoadingSlider(valueBetween0To1, cancellationTokenSource);
        }
    }
}
