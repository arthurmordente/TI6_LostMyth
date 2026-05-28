using Logic.Scripts.GameDomain.Utilities;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.StateMachineService;

public class LobbyUiController : ILobbyController {
    private readonly ILobbyMenuView _lobbyView;
    private readonly IStateMachineService _stateMachineService;
    private readonly ExplorationState.Factory _explorationStateFactory;
    private readonly IAudioService _audioService;
    private readonly IUniversalUIController _universalUIController;

    public LobbyUiController(ILobbyMenuView lobbyView, IStateMachineService stateMachineService, ExplorationState.Factory explorationStateFactory,
        IAudioService audioService, IUniversalUIController universalUIController) {
        _lobbyView = lobbyView;
        _stateMachineService = stateMachineService;
        _explorationStateFactory = explorationStateFactory;
        _audioService = audioService;
        _universalUIController = universalUIController;
    }

    public void InitEntryPoint() {
        _lobbyView.RegisterCallbacks(OnClickPlay, OnClickConfig, OnExitPlay);
        _lobbyView.Show();
        _audioService.PlayAudio(AudioClipType.MenuTheme, AudioChannelType.Music, AudioPlayType.Loop);
    }

    public void HideMenu() => _lobbyView.Hide();

    public void OnClickPlay() {
        _stateMachineService.SwitchState(_explorationStateFactory.Create(new ExplorationInitiatorEnterData(0)));
        _audioService.PlayAudio(AudioClipType.UIClick1SFX, AudioChannelType.Fx);
    }

    public void OnClickConfig() {
        _universalUIController.ShowOptionsScreen();
        _audioService.PlayAudio(AudioClipType.UIClick1SFX, AudioChannelType.Fx);
    }

    public void OnExitPlay() {
        _audioService.PlayAudio(AudioClipType.UIClick2SFX, AudioChannelType.Fx);
        QuitApplicationUtility.Quit();
    }
}
