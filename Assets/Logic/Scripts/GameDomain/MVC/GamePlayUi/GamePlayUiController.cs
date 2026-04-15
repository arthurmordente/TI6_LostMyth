using Logic.Scripts.Core.Mvc.UICamera;
using Logic.Scripts.GameDomain.Commands;
using Logic.Scripts.GameDomain.States;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.StateMachineService;
using System.Threading;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Ui {
    public class GamePlayUiController : IGamePlayUiController {
        private readonly IStateMachineService _stateMachineService;
        private readonly ExplorationState.Factory _explorationStateFactory;
        private readonly IUICameraController _uiCameraController;
        private readonly IAudioService _audioService;
        private readonly IGamePlayHudView _gamePlayHud;
        private readonly PauseUiView _pauseUiView;
        private readonly GameOverUIView _gameOverUIView;
        private readonly IUniversalUIController _universalUIController;
        private readonly ICommandFactory _commandFactory;

        public GamePlayUiController(IStateMachineService stateMachineService, ExplorationState.Factory explorationStateFactory,
            IUICameraController uiCameraController, IGamePlayHudView gamePlayHud, IAudioService audioService, PauseUiView pauseUiView,
            IUniversalUIController universalUIController, ICommandFactory commandFactory, GameOverUIView gameOverUIView) {
            _stateMachineService = stateMachineService;
            _explorationStateFactory = explorationStateFactory;
            _uiCameraController = uiCameraController;
            _gamePlayHud = gamePlayHud;
            _audioService = audioService;
            _pauseUiView = pauseUiView;
            _universalUIController = universalUIController;
            _commandFactory = commandFactory;
            _gameOverUIView = gameOverUIView;
        }

        public void InitEntryPoint() {
            _pauseUiView.InitEntryPoint();
            _pauseUiView.RegisterCallbacks(_universalUIController.ShowGuideScreen, _universalUIController.ShowOptionsScreen,
                _universalUIController.ShowLoadScreen, _universalUIController.ShowCheatsScreen, ResumeGame, BackToLobby);
            _gamePlayHud.InitStartPoint();
            _gamePlayHud.RegisterCallbacks(OnClickNextTurn, OnClickAbility1, OnClickAbility2, OnClickAbility3, OnClickAbility4);
            _gameOverUIView.InitEntryPoint();
            _gameOverUIView.RegisterCallbacks(OnClickPlayAgain, OnClickPlayAgain, BackToLobby);
        }
        #region GameplayUiInputs
        public void OnClickNextTurn() {
            _commandFactory.CreateCommandVoid<CompletePlayerActionCommand>().Execute();
        }

        public void OnClickAbility1() {
            _commandFactory.CreateCommandVoid<UseAbility1InputCommand>().Execute();
        }

        public void OnClickAbility2() {
            _commandFactory.CreateCommandVoid<UseAbility2InputCommand>().Execute();
        }

        public void OnClickAbility3() {
            _commandFactory.CreateCommandVoid<UseAbility3InputCommand>().Execute();
        }

        public void OnClickAbility4() {
            _commandFactory.CreateCommandVoid<UseAbility4InputCommand>().Execute();
        }

        #endregion
        public void InitExitPoint() {

        }

        public Transform GameplayHudRoot() => _gamePlayHud.GetGameplayHudRoot();

        #region GameOver
        public void ShowGameOver(bool IsWin) {
            _gameOverUIView.Show(IsWin);
        }
        public async void OnClickPlayAgain() {
            _commandFactory.CreateCommandVoid<ResumeGameplayInputCommand>().Execute();
            await _commandFactory.CreateCommandAsync<ReloadLevelCommand>().Execute(CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken));
            _gameOverUIView.Hide();
        }
        public void OnLoad() {
            Debug.LogWarning("Clicou no load");
        }
        #endregion

        #region Pause
        public void ShowPauseScreen() {
            _pauseUiView.Show();
        }
        public void HidePauseScreen() {
            _pauseUiView.Hide();
        }
        private void ResumeGame() {
            _commandFactory.CreateCommandVoid<ResumeGameplayInputCommand>().Execute();
        }

        private void BackToLobby() {
            _commandFactory.CreateCommandVoid<ResumeGameplayInputCommand>().Execute();
            _stateMachineService.SwitchState(_explorationStateFactory.Create(new ExplorationInitiatorEnterData(0)));
        }
        #endregion

        public void SetPlayerValues(int previewHp, int actualHp, int maxHp) {
            _gamePlayHud.SnapPlayerHealth(previewHp, actualHp, maxHp);
        }

        public void SetAbilityManaCosts(int c1, int c2, int c3, int c4) {
            _gamePlayHud.OnSkill1CostChange(c1);
            _gamePlayHud.OnSkill2CostChange(c2);
            _gamePlayHud.OnSkill3CostChange(c3);
            _gamePlayHud.OnSkill4CostChange(c4);
        }

        public void OnBossDisplayNameChange(string displayName) => _gamePlayHud.OnBossDisplayNameChange(displayName);

        public void ShowWinPanel(CancellationTokenSource cancellationTokenSource) {

        }

        public void ShowGameOverPanel(CancellationTokenSource cancellationTokenSource) {

        }

        public void SnapBossHealth(int hp, int maxHp) => _gamePlayHud.SnapBossHealth(hp, maxHp);

        public void OnBossHealthUpdate(int hp, int maxHp) => _gamePlayHud.OnBossHealthUpdate(hp, maxHp);

        public void OnPreviewBossHealthChange(int newValue) => _gamePlayHud.OnPreviewBossHealthChange(newValue);

        public void OnPlayerHealthUpdate(int hp, int maxHp) => _gamePlayHud.OnPlayerHealthUpdate(hp, maxHp);

        public void OnPreviewPlayerHealthUpdate(int previewHp, int maxHp) => _gamePlayHud.OnPreviewPlayerHealthUpdate(previewHp, maxHp);

        public void SnapPlayerActionPoints(int current, int max) => _gamePlayHud.SnapPlayerActionPoints(current, max);

        public void OnPlayerActionPointsChange(int current, int max) => _gamePlayHud.OnPlayerActionPointsChange(current, max);

        public void OnSkill1CostChange(int newValue) => _gamePlayHud.OnSkill1CostChange(newValue);

        public void OnSkill2CostChange(int newValue) => _gamePlayHud.OnSkill2CostChange(newValue);

        public void OnSkill3CostChange(int newValue) => _gamePlayHud.OnSkill3CostChange(newValue);

        public void OnSkill4CostChange(int newValue) => _gamePlayHud.OnSkill4CostChange(newValue);

        public void OnSkill1NameChange(string newValue) => _gamePlayHud.OnSkill1NameChange(newValue);

        public void OnSkill2NameChange(string newValue) => _gamePlayHud.OnSkill2NameChange(newValue);

    }
}
