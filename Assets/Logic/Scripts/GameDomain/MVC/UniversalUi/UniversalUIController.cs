using UnityEngine;

public class UniversalUIController : IUniversalUIController {
    private readonly LoadUIView _loadView;
    private readonly GuideUIView _guideView;
    private readonly CheatsUIView _cheatsView;
    private readonly CreditsUIView _creditsView;
    private readonly OptionsUIView _optionsView;

    public UniversalUIController(LoadUIView loadView, GuideUIView guideView, CheatsUIView cheatsView, CreditsUIView creditsView, OptionsUIView optionsView) {
        _loadView = loadView;
        _guideView = guideView;
        _cheatsView = cheatsView;
        _creditsView = creditsView;
        _optionsView = optionsView;
    }

    public async Awaitable InitEntryPoint() {
        _loadView.InitEntryPoint();
        _loadView.RegisterCallbacks(ShowGuideScreen, ShowCheatsScreen, ShowCreditsScreen, OnClickExit, ShowOptionsScreen);
        await _guideView.InitiPoint();
        _guideView.RegisterCallbacks();
        _cheatsView.InitEntryPoint();
        _cheatsView.RegisterCallbacks(ShowGuideScreen, ShowLoadScreen, ShowCreditsScreen, OnClickExit, ShowOptionsScreen);
        _creditsView.InitEntryPoint();
        _creditsView.RegisterCallbacks(ShowGuideScreen, ShowLoadScreen, ShowCheatsScreen, OnClickExit, ShowOptionsScreen);
        _optionsView.InitEntryPoint();
        _optionsView.RegisterCallbacks();
    }

    public void ShowLoadScreen() {
        _loadView.Show();
    }

    public void ShowGuideScreen() {
        _guideView.Show();
    }

    public void ShowCheatsScreen() {
        _cheatsView.Show();
    }

    public void ShowCreditsScreen() {
        _creditsView.Show();
    }

    public void ShowOptionsScreen() {
        _optionsView.Show();
    }
    private void OnClickExit() {
        Application.Quit();
    }
}
