using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.StartLevel;
using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Logic.Scripts.GameDomain.Audio;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Turns;
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
        private IActiveUnitService _activeUnitService;
        private IActionPointsService _actionPointsService;
        private ILevelsDataService _levelsDataService;

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
            _activeUnitService = _diContainer.Resolve<IActiveUnitService>();
            _actionPointsService = _diContainer.Resolve<IActionPointsService>();
            _levelsDataService = _diContainer.Resolve<ILevelsDataService>();
        }

        public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
            _gameInputActionsController.RegisterGameplayInputListeners();
            _naraController.InitEntryPointGamePlay(_gamePlayUiController);
            _naraController.ApplyCombatLoadoutPassivesAndActionPoints(_actionPointsService);
            await _commandFactory.CreateCommandAsync<StartLevelCommand>().StartBoss().Execute(cancellationTokenSource);

            var levelData = _levelsDataService.GetLevelData(_enterData.LevelNumberToEnter) as LevelTurnData;
            _audioService.PlayMusic(AudioMusicResolver.ResolveFightMusic(levelData));

            _gamePlayUiController.InitEntryPoint();
            _activeUnitService.RefreshHudAbilityCosts();
            _gamePlayUiController.SetSkillsSlidableExpanded(false, instant: true);
        }
    }
}
