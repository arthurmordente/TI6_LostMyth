using Logic.Scripts.GameDomain.Exploration;
using Logic.Scripts.GameDomain.States;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.StateMachineService;
using UnityEngine;
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

        private bool _isVisible;

        public bool IsVisible => _isVisible;

        public ExplorationPauseController(
            IPauseMenuView pauseMenuView,
            IUniversalUIController universalUIController,
            IStateMachineService stateMachineService,
            LobbyState.Factory lobbyStateFactory,
            ICommandFactory commandFactory,
            IAudioService audioService)
        {
            _pauseMenuView = pauseMenuView;
            _universalUIController = universalUIController;
            _stateMachineService = stateMachineService;
            _lobbyStateFactory = lobbyStateFactory;
            _commandFactory = commandFactory;
            _audioService = audioService;
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
            _pauseMenuView.Show();
        }

        public void HidePauseScreen()
        {
            if (!_isVisible) return;
            _pauseMenuView.Hide();
            ExplorationInteractInputGate.Pop();
            ExplorationModalInputGate.Pop();
            _isVisible = false;
        }

        public void TogglePauseScreen()
        {
            if (_isVisible) HidePauseScreen();
            else ShowPauseScreen();
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
            Application.Quit();
        }

        private void PlayClick() =>
            _audioService?.PlayAudio(AudioClipType.UIClick1SFX, AudioChannelType.Fx);
    }
}
