using System.Threading;
using System;
using UnityEngine;
using Zenject;
using Logic.Scripts.Services.SceneServices;
using Logic.Scripts.Services.Logger.Base;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Core.Mvc.LoadingScreen;

namespace Logic.Scripts.Core.CoreInitiator {
    public class CoreInitiator : MonoBehaviour {
        private GameInputActions _gameInputActions;
        private ISceneLoaderService _sceneLoaderService;
        private IAudioService _audioService;
        private ILoadingScreenController _loadingScreenController;
        private LoadingTipPoolSO _loadingTipPool;

        [Inject]
        private void Setup(GameInputActions gameInputActions, ISceneLoaderService sceneLoaderService, IAudioService audioService,
            ILoadingScreenController loadingScreenController, [InjectOptional] LoadingTipPoolSO loadingTipPool = null) {
            _gameInputActions = gameInputActions;
            _sceneLoaderService = sceneLoaderService;
            _audioService = audioService;
            _loadingScreenController = loadingScreenController;
            _loadingTipPool = loadingTipPool;
        }

        private void Start() {
            _ = InitEntryPoint(CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken));
        }

        private async Awaitable InitEntryPoint(CancellationTokenSource cancellationTokenSource) {
            try {
                UpdateApplicationSettings();
                _loadingScreenController.SetupLoadingView(_loadingTipPool);
                InitializeServices();
                await LoadGameScene(cancellationTokenSource);
            }
            catch (OperationCanceledException) {
                LogService.Log("Operation init core was cancelled");
            }
            catch (Exception e) {
                LogService.LogError(e.Message);
                throw;
            }
        }

        private void UpdateApplicationSettings() {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
        }

        private void InitializeServices() {
            _gameInputActions.Enable();
            _audioService.InitEntryPoint();
            _sceneLoaderService.InitEntryPoint();
        }

        private async Awaitable LoadGameScene(CancellationTokenSource cancellationTokenSource) {
            await _sceneLoaderService.TryLoadScene(SceneType.GameScene, new GameInitiatorEnterData(), cancellationTokenSource);
        }
    }
}
