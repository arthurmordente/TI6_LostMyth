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
        _audioService.PlayMusic(MusicIds.Menu);
    }

    public void HideMenu() => _lobbyView.Hide();

    public void OnClickPlay() {
        _stateMachineService.SwitchState(_explorationStateFactory.Create(new ExplorationInitiatorEnterData(0)));
        _audioService.PlaySfx(SfxIds.UI_Clique, AudioChannelType.SfxUi);
    }

    public void OnClickConfig() {
        _universalUIController.ShowOptionsScreen();
        _audioService.PlaySfx(SfxIds.UI_Clique, AudioChannelType.SfxUi);
    }

    public void OnExitPlay() {
        _audioService.PlaySfx(SfxIds.UI_Clique2, AudioChannelType.SfxUi);
        QuitApplicationUtility.Quit();
    }
}
