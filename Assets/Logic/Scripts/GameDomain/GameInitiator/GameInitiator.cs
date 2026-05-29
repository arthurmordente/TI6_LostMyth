using Logic.Scripts.Core.CoreInitiator;
using Logic.Scripts.Core.CoreInitiator.Base;
using Logic.Scripts.Core.Mvc.LoadingScreen;
using Logic.Scripts.GameDomain.States;
using Logic.Scripts.Services.InitiatorInvokerService;
using Logic.Scripts.Services.Logger.Base;
using Logic.Scripts.Services.StateMachineService;
using Logic.Scripts.Utils;
using System.Threading;
using UnityEngine;
using Zenject;
using Logic.Scripts.Services.AudioService;

namespace Logic.Scripts.GameDomain.GameInitiator {
    public class GameInitiator : ISceneInitiator, IGameInitiator {
        private readonly IStateMachineService _stateMachine;
        private readonly ILoadingScreenController _loadingScreenController;
        private readonly LobbyState.Factory _lobbyStateFactory;
        private readonly ILevelsDataService _levelsDataService;
        private readonly ISceneInitiatorsService _sceneInitiatorsService;
        private readonly IUniversalUIController _universalUIController;

        private readonly IAudioService _audio;
        private readonly MusicClipsScriptableObject _gameplayMusicClips;
        private readonly SfxClipsScriptableObject _gameplaySfxClips;

        public SceneType SceneType => SceneType.GameScene;

        public GameInitiator(IStateMachineService stateMachine, LobbyState.Factory LobbyStateFactory, ILoadingScreenController loadingScreenController,
            ILevelsDataService levelsDataService, ISceneInitiatorsService sceneInitiatorsService, IAudioService audio,
            IUniversalUIController universalUIController,
            [InjectOptional] MusicClipsScriptableObject gameplayMusicClips = null,
            [InjectOptional] SfxClipsScriptableObject gameplaySfxClips = null) {
            _stateMachine = stateMachine;
            _lobbyStateFactory = LobbyStateFactory;
            _loadingScreenController = loadingScreenController;
            _levelsDataService = levelsDataService;
            _sceneInitiatorsService = sceneInitiatorsService;
            _audio = audio;
            _gameplayMusicClips = gameplayMusicClips;
            _gameplaySfxClips = gameplaySfxClips;
            _universalUIController = universalUIController;

            _sceneInitiatorsService.RegisterInitiator(this);
        }

        public async Awaitable LoadEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            await _universalUIController.InitEntryPoint();

            if (_gameplayMusicClips == null)
                LogService.LogError("[Audio] GameplayMusicClips is not assigned on GameInstaller (GameScene).");
            else
                _audio.AddMusicClips(_gameplayMusicClips);

            if (_gameplaySfxClips == null)
                LogService.LogError("[Audio] GameplaySfxClips is not assigned on GameInstaller (GameScene).");
            else
                _audio.AddSfxClips(_gameplaySfxClips);

            _ = _loadingScreenController.SetLoadingSlider(0.5f, cancellationTokenSource);
            await _levelsDataService.LoadLevelsData(cancellationTokenSource);
            await _stateMachine.EnterInitialGameState(_lobbyStateFactory.Create(new LobbyInitiatorEnterData()), cancellationTokenSource);
        }

        public Awaitable StartEntryPoint(IInitiatorEnterData enterDataObject, CancellationTokenSource cancellationTokenSource) {
            return AwaitableUtils.CompletedTask;
        }

        public Awaitable InitExitPoint(CancellationTokenSource cancellationTokenSource) {
            _sceneInitiatorsService.UnregisterInitiator(this);
            return AwaitableUtils.CompletedTask;
        }
    }
}
