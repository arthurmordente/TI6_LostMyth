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

        public void SetupLoadingView(LoadingTipPoolSO tipPool) {
            _loadingScreenView.InitPoint(tipPool);
        }

        public void ShowTransitionTip() {
            LogService.LogTopic("Show transition tip", LogTopicType.LoadingScreen);
            _loadingScreenView.ShowTransitionTip();
        }

        public void EnableContinuePrompt() {
            LogService.LogTopic("Enable continue prompt", LogTopicType.LoadingScreen);
            _loadingScreenView.EnableContinuePrompt();
        }

        public Awaitable WaitForPlayerContinue(CancellationTokenSource cancellationTokenSource) {
            return _loadingScreenView.WaitForPlayerContinue(cancellationTokenSource);
        }

        public void Hide() {
            LogService.LogTopic("Hide loading screen", LogTopicType.LoadingScreen);
            _loadingScreenView.Hide();
        }
    }
}
