using Logic.Scripts.GameDomain.Exploration;
using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.States;
using Logic.Scripts.GameDomain.Utilities;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.StateMachineService;
using Zenject;

namespace Logic.Scripts.GameDomain.Exploration.Pause
{
    public sealed class ExplorationPauseController : IExplorationPauseController
    {
        private readonly IPauseMenuView _pauseMenuView;
        private readonly IUniversalUIController _universalUIController;
        private readonly IStateMachineService _stateMachineService;
        private readonly LobbyState.Factory _lobbyStateFactory;
        private readonly ICommandFactory _commandFactory;
        private readonly IAudioService _audioService;
        private readonly IGameInputActionsController _gameInputActionsController;

        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public ExplorationPauseController(
            IPauseMenuView pauseMenuView,
            IUniversalUIController universalUIController,
            IStateMachineService stateMachineService,
            LobbyState.Factory lobbyStateFactory,
            ICommandFactory commandFactory,
            IAudioService audioService,
            IGameInputActionsController gameInputActionsController)
        {
            _pauseMenuView = pauseMenuView;
            _universalUIController = universalUIController;
            _stateMachineService = stateMachineService;
            _lobbyStateFactory = lobbyStateFactory;
            _commandFactory = commandFactory;
            _audioService = audioService;
            _gameInputActionsController = gameInputActionsController;
        }

        public void InitEntryPoint()
        {
            _pauseMenuView.InitEntryPoint();
            _pauseMenuView.RegisterCallbacks(
                OnConfig,
                OnCredits,
                OnResume,
                OnRetreat,
                OnQuit);
        }

        public void ShowPauseScreen()
        {
            if (_isVisible) return;
            _isVisible = true;
            ExplorationModalInputGate.Push();
            ExplorationInteractInputGate.Push();
            _commandFactory.CreateCommandVoid<StopMoveInputCommand>().Execute();
            _gameInputActionsController.EnableUIInputs();
            _gameInputActionsController.RegisterUIExplorationInputListeners();
            _pauseMenuView.Show();
        }

        public void HidePauseScreen()
        {
            if (!_isVisible) return;
            _universalUIController.TryCloseTopOverlay();
            _pauseMenuView.Hide();
            _gameInputActionsController.UnregisterUIExplorationInputListeners();
            _gameInputActionsController.DisableUIInputs();
            ExplorationInteractInputGate.Pop();
            ExplorationModalInputGate.Pop();
            _isVisible = false;
        }

        public void TogglePauseScreen()
        {
            if (_isVisible)
                HandleEscapeWhilePaused();
            else
                ShowPauseScreen();
        }

        public void HandleEscapeWhilePaused()
        {
            if (!_isVisible) return;
            if (_universalUIController.TryCloseTopOverlay())
                return;
            HidePauseScreen();
        }

        public void ForceHide()
        {
            if (_isVisible)
                HidePauseScreen();
        }

        private void OnResume()
        {
            PlayClick();
            HidePauseScreen();
        }

        private void OnRetreat()
        {
            PlayClick();
            HidePauseScreen();
            _stateMachineService.SwitchState(_lobbyStateFactory.Create(new LobbyInitiatorEnterData()));
        }

        private void OnConfig()
        {
            PlayClick();
            _universalUIController.ShowOptionsScreen();
        }

        private void OnCredits()
        {
            PlayClick();
            _universalUIController.ShowCreditsScreen();
        }

        private void OnQuit()
        {
            PlayClick();
            QuitApplicationUtility.Quit();
        }

        private void PlayClick() =>
            _audioService?.PlayAudio(AudioClipType.UIClick1SFX, AudioChannelType.Fx);
    }
}
