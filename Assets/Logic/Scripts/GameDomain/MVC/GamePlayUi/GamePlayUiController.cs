using Logic.Scripts.Core.Mvc.UICamera;
using Logic.Scripts.GameDomain.Commands;
using Logic.Scripts.GameDomain.States;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.StateMachineService;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;

namespace Logic.Scripts.GameDomain.MVC.Ui {
    public class GamePlayUiController : IGamePlayUiController {
        private readonly IStateMachineService _stateMachineService;
        private readonly ExplorationState.Factory _explorationStateFactory;
        private readonly IUICameraController _uiCameraController;
        private readonly IAudioService _audioService;
        private readonly GamePlayUiView _gamePlayView;
        private readonly GamePlayUiBindSO _gamePlayUiBindSO;
        private readonly PauseUiView _pauseUiView;
        private readonly GameOverUIView _gameOverUIView;
        private readonly IUniversalUIController _universalUIController;
        private readonly ICommandFactory _commandFactory;

        public GamePlayUiController(IStateMachineService stateMachineService, ExplorationState.Factory explorationStateFactory,
            IUICameraController uiCameraController, GamePlayUiView gamePlayView, IAudioService audioService, PauseUiView pauseUiView,
            IUniversalUIController universalUIController, ICommandFactory commandFactory, GameOverUIView gameOverUIView) {
            _stateMachineService = stateMachineService;
            _explorationStateFactory = explorationStateFactory;
            _uiCameraController = uiCameraController;
            _gamePlayView = gamePlayView;
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
            _gamePlayView.InitStartPoint();
            _gamePlayView.RegisterCallbacks(OnClickNextTurn, OnClickAbility1, OnClickAbility2, OnClickAbility3,
                OnClickAbility4, OnClickAbility5, OnClickClone1, OnClickClone2);
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

        public void OnClickAbility5() {
            _commandFactory.CreateCommandVoid<UseAbility5InputCommand>().Execute();
        }

        public void OnClickClone1() {
            _commandFactory.CreateCommandVoid<CreateCopy1InputCommand>().Execute();
        }

        public void OnClickClone2() {
            _commandFactory.CreateCommandVoid<CreateCopy2InputCommand>().Execute();
        }
        #endregion
        public void InitExitPoint() {

        }

        public VisualElement GameplayMainContainer() {
            return _gamePlayView.GetMainContainer();
        }

        #region GameOver
        public void ShowGameOver(bool IsWin) {
            _gameOverUIView.Show(IsWin);
        }
        public async void OnClickPlayAgain() {
            _commandFactory.CreateCommandVoid<ResumeGameplayInputCommand>().Execute();
            await _commandFactory.CreateCommandAsync<ReloadLevelCommand>().Execute(CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken));
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

        public void SetBossValues(int newValue) {
            _gamePlayView.OnActualBossHealthChange(newValue);

            _gamePlayView.OnPreviewBossHealthChange(newValue);

            _gamePlayView.OnActualBossLifeChange(newValue);
        }

        public void SetBossValues(int newPreviewValue, int newActualValue) {
            _gamePlayView.OnActualBossHealthChange(newActualValue);

            _gamePlayView.OnPreviewBossHealthChange(newPreviewValue);

            _gamePlayView.OnActualBossLifeChange(newActualValue);
        }

        public void SetPlayerValues(int newValue) {
            _gamePlayView.OnActualPlayerLifePercentChange(newValue);

            _gamePlayView.OnPreviewPlayerLifePercentChange(newValue);

            _gamePlayView.OnActualPlayerHealthChange(newValue);
        }

        public void SetPlayerValues(int newPreviewValue, int newActualValue) {
            _gamePlayView.OnActualPlayerLifePercentChange(newActualValue);

            _gamePlayView.OnPreviewPlayerLifePercentChange(newPreviewValue);

            _gamePlayView.OnActualPlayerHealthChange(newActualValue);
        }

        public void SetAbilityValues(int ability1Cost, string ability1Name,
            int ability2Cost, string ability2Name) {
            UnityEngine.Debug.Log("GameplayView: ");
            _gamePlayView.OnSkill1CostChange(ability1Cost);

            _gamePlayView.OnSkill2CostChange(ability2Cost);

            _gamePlayView.OnSkill1NameChange(ability1Name);

            _gamePlayView.OnSkill2NameChange(ability2Name);
        }

        public void ShowWinPanel(CancellationTokenSource cancellationTokenSource) {

        }

        public void ShowGameOverPanel(CancellationTokenSource cancellationTokenSource) {

        }

        public void OnActualBossHealthChange(int newValue) => _gamePlayView.OnActualBossHealthChange(newValue);

        public void OnPreviewBossHealthChange(int newValue) => _gamePlayView.OnPreviewBossHealthChange(newValue);

        public void OnActualBossLifeChange(int newValue) => _gamePlayView.OnActualBossLifeChange(newValue);

        public void OnActualPlayerLifePercentChange(int newValue) => _gamePlayView.OnActualPlayerLifePercentChange(newValue);

        public void OnPreviewPlayerLifePercentChange(int newValue) => _gamePlayView.OnPreviewPlayerLifePercentChange(newValue);

        public void OnActualPlayerHealthChange(int newValue) => _gamePlayView.OnActualPlayerHealthChange(newValue);

        public void OnPlayerActionPointsChange(int newValue) => _gamePlayView.OnPlayerActionPointsChange(newValue);

        public void OnSkill1CostChange(int newValue) => _gamePlayView.OnSkill1CostChange(newValue);

        public void OnSkill2CostChange(int newValue) => _gamePlayView.OnSkill2CostChange(newValue);

        public void OnSkill1NameChange(string newValue) => _gamePlayView.OnSkill1NameChange(newValue);

        public void OnSkill2NameChange(string newValue) => _gamePlayView.OnSkill2NameChange(newValue);

    }
}