using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.StartLevel;
using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using System.Threading;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Commands {
    public class StartGamePlayStateCommand : BaseCommand, ICommandAsync {
        private IGamePlayUiController _gamePlayUiController;
        private IAudioService _audioService;
        private INaraController _naraController;
        private ICommandFactory _commandFactory;
        private IWorldCameraController _worldCameraController;
        private IGameInputActionsController _gameInputActionsController;

        private GamePlayInitatorEnterData _enterData;

        public StartGamePlayStateCommand SetEnterData(GamePlayInitatorEnterData enterData) {
            _enterData = enterData;
            return this;
        }

        public override void ResolveDependencies() {
            _audioService = _diContainer.Resolve<IAudioService>();
            _gamePlayUiController = _diContainer.Resolve<IGamePlayUiController>();
            _naraController = _diContainer.Resolve<INaraController>();
            _commandFactory = _diContainer.Resolve<ICommandFactory>();
            _worldCameraController = _diContainer.Resolve<IWorldCameraController>();
            _gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            _gameInputActionsController.RegisterGameplayInputListeners();
            await _commandFactory.CreateCommandAsync<StartLevelCommand>().StartBoss().Execute(cancellationTokenSource);
            _naraController.InitEntryPointGamePlay(_gamePlayUiController);
            _audioService.PlayAudio(AudioClipType.BossTheme, AudioChannelType.Music, AudioPlayType.Loop);
            _gamePlayUiController.InitEntryPoint();
        }
    }
}