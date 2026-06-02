using CoreDomain.GameDomain.GameStateDomain.GamePlayDomain.Scripts.Commands.StartLevel;
using Logic.Scripts.GameDomain.Audio;
using Logic.Scripts.GameDomain.Commands;
using Logic.Scripts.GameDomain.MVC.Echo;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Turns;
using System.Threading;
using UnityEngine;

public class ReloadLevelCommand : BaseCommand, ICommandAsync {
    private ICommandFactory _commandFactory;
    private IGamePlayDataService _gamePlayDataService;
    private INaraController _naraController;
    private IGamePlayUiController _gamePlayUiController;
    private IActionPointsService _actionPointsService;
    private IActiveUnitService _activeUnitService;
    private ILevelsDataService _levelsDataService;
    private IAudioService _audioService;
    private ICloneUseLimiter _cloneUseLimiter;
    private IRandomTurnPassiveService _randomTurnPassiveService;
    private ILowHealthOutgoingDamageService _lowHealthOutgoingDamageService;
    private IDamageStackMovementPassiveService _damageStackMovementPassiveService;

    public override void ResolveDependencies() {
        _commandFactory = _diContainer.Resolve<ICommandFactory>();
        _gamePlayDataService = _diContainer.Resolve<IGamePlayDataService>();
        _naraController = _diContainer.Resolve<INaraController>();
        _gamePlayUiController = _diContainer.Resolve<IGamePlayUiController>();
        _actionPointsService = _diContainer.Resolve<IActionPointsService>();
        _activeUnitService = _diContainer.Resolve<IActiveUnitService>();
        _levelsDataService = _diContainer.Resolve<ILevelsDataService>();
        _audioService = _diContainer.TryResolve<IAudioService>();
        _cloneUseLimiter = _diContainer.TryResolve<ICloneUseLimiter>();
        _randomTurnPassiveService = _diContainer.TryResolve<IRandomTurnPassiveService>();
        _lowHealthOutgoingDamageService = _diContainer.TryResolve<ILowHealthOutgoingDamageService>();
        _damageStackMovementPassiveService = _diContainer.TryResolve<IDamageStackMovementPassiveService>();
    }

    public async Awaitable Execute(CancellationTokenSource cancellationTokenSource) {
        int levelNumber = _gamePlayDataService.CurrentLevelNumber;

        _commandFactory.CreateCommandVoid<ExitTurnModeCommand>().Execute();
        _commandFactory.CreateCommandVoid<DisposeLevelCommand>().Execute();

        await _commandFactory.CreateCommandAsync<LoadLevelCommand>()
            .SetEnterData(new LoadLevelCommandData(levelNumber))
            .Execute(cancellationTokenSource);

        // Same fight bootstrap as StartGamePlayStateCommand (without re-entering gameplay state).
        _naraController.InitEntryPointGamePlay(_gamePlayUiController);
        _naraController.ApplyCombatLoadoutPassivesAndActionPoints(_actionPointsService);
        _randomTurnPassiveService?.RefreshFromLoadout();
        _lowHealthOutgoingDamageService?.RefreshFromLoadout();
        _damageStackMovementPassiveService?.RefreshFromLoadout();
        _cloneUseLimiter?.ResetForPlayerTurn();

        await _commandFactory.CreateCommandAsync<StartLevelCommand>()
            .StartBoss()
            .Execute(cancellationTokenSource);

        if (_levelsDataService.GetLevelData(levelNumber) is LevelTurnData levelData)
            _audioService?.PlayMusic(AudioMusicResolver.ResolveFightMusic(levelData));

        _activeUnitService.RefreshHudAbilityCosts();
        _gamePlayUiController.SetSkillsSlidableExpanded(false, instant: true);
        _gamePlayUiController.SyncBookCloneActionHud();
    }
}
