using Logic.Scripts.GameDomain.Utilities;
using Logic.Scripts.Services.AudioService;
using UnityEngine;
using Zenject;

public class UniversalUIController : IUniversalUIController {
    private readonly ILoadScreenView _loadView;
    private readonly IGuideScreenView _guideView;
    private readonly ICheatsScreenView _cheatsView;
    private readonly ICreditsView _creditsView;
    private readonly IOptionsView _optionsView;
    private readonly ICheatController _cheatController;
    private readonly IAudioService _audioService;

    public UniversalUIController(
        [InjectOptional] ILoadScreenView loadView,
        [InjectOptional] IGuideScreenView guideView,
        [InjectOptional] ICheatsScreenView cheatsView,
        [InjectOptional] ICreditsView creditsView,
        [InjectOptional] IOptionsView optionsView,
        ICheatController cheatController,
        [InjectOptional] IAudioService audioService = null) {
        _loadView = loadView;
        _guideView = guideView;
        _cheatsView = cheatsView;
        _creditsView = creditsView;
        _optionsView = optionsView;
        _cheatController = cheatController;
        _audioService = audioService;
    }

    public async Awaitable InitEntryPoint() {
        if (_loadView != null) {
            _loadView.InitEntryPoint();
            _loadView.RegisterCallbacks(ShowGuideScreen, ShowCheatsScreen, ShowCreditsScreen, OnClickExit, ShowOptionsScreen);
            _loadView.Hide();
        }
        if (_guideView != null) {
            await _guideView.InitEntryPoint();
            _guideView.RegisterCallbacks();
            _guideView.Hide();
        }
        if (_cheatsView != null) {
            _cheatsView.InitEntryPoint();
            _cheatsView.RegisterCallbacks(ShowGuideScreen, ShowLoadScreen, ShowCreditsScreen, OnClickExit, ShowOptionsScreen,
                _cheatController.SetImortal, _cheatController.SetInfinityCast, _cheatController.SetInifinityMove);
            _cheatsView.Hide();
        }
        if (_creditsView != null) {
            _creditsView.InitEntryPoint();
            _creditsView.RegisterCallbacks(null);
            _creditsView.Hide();
        }
        if (_optionsView != null) {
            _optionsView.InitEntryPoint();
            _optionsView.RegisterCallbacks();
            _optionsView.Hide();
        }
    }

    /// <summary>
    /// Fecha o overlay modal mais recente (créditos, opções, etc.) sem despausar.
    /// </summary>
    public bool TryCloseTopOverlay() {
        if (_creditsView != null && _creditsView.IsVisible) {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            _creditsView.Hide();
            return true;
        }
        if (_optionsView != null && _optionsView.IsVisible) {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            _optionsView.Hide();
            return true;
        }
        if (_cheatsView != null && _cheatsView.IsVisible) {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            _cheatsView.Hide();
            return true;
        }
        if (_guideView != null && _guideView.IsVisible) {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            _guideView.Hide();
            return true;
        }
        if (_loadView != null && _loadView.IsVisible) {
            GeneralSfxFeedback.PlayMenuClick(_audioService);
            _loadView.Hide();
            return true;
        }
        return false;
    }

    public void CloseAllOverlays()
    {
        while (TryCloseTopOverlay()) { }
    }

    public void ShowLoadScreen() {
        GeneralSfxFeedback.PlayMenuClick(_audioService);
        _loadView?.Show();
    }

    public void ShowGuideScreen() {
        GeneralSfxFeedback.PlayMenuClick(_audioService);
        _guideView?.Show();
    }

    public void ShowCheatsScreen() {
        GeneralSfxFeedback.PlayMenuClick(_audioService);
        _cheatsView?.Show();
    }

    public void ShowCreditsScreen() {
        GeneralSfxFeedback.PlayMenuClick(_audioService);
        _creditsView?.Show();
    }

    public void ShowOptionsScreen() {
        GeneralSfxFeedback.PlayMenuClick(_audioService);
        _optionsView?.Show();
    }

    private void OnClickExit() {
        GeneralSfxFeedback.PlayMenuClick(_audioService, secondary: true);
        QuitApplicationUtility.Quit();
    }
}
