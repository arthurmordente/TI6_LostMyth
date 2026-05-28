using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single hook on GameScene for shared overlay canvases (Options, Credits, Load, Guide, Cheats).
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class UniversalUiSceneViews : MonoBehaviour
{
    [SerializeField] private OptionsCanvasView _optionsView;
    [SerializeField] private CreditsCanvasView _creditsView;
    [SerializeField] private LoadCanvasView _loadView;
    [SerializeField] private GuideCanvasView _guideView;
    [SerializeField] private CheatsCanvasView _cheatsView;

    public OptionsCanvasView Options => _optionsView;
    public CreditsCanvasView Credits => _creditsView;
    public LoadCanvasView Load => _loadView;
    public GuideCanvasView Guide => _guideView;
    public CheatsCanvasView Cheats => _cheatsView;

    private void Awake()
    {
        EnsureViews();
        HideAllOverlayViews();
    }

    public void EnsureViews()
    {
        _optionsView = EnsureView(_optionsView, nameof(OptionsCanvasView));
        _creditsView = EnsureView(_creditsView, nameof(CreditsCanvasView));
        _loadView = EnsureView(_loadView, nameof(LoadCanvasView));
        _guideView = EnsureView(_guideView, nameof(GuideCanvasView));
        _cheatsView = EnsureView(_cheatsView, nameof(CheatsCanvasView));
        HideAllOverlayViews();
    }

    private void HideAllOverlayViews()
    {
        HideView(_optionsView);
        HideView(_creditsView);
        HideView(_loadView);
        HideView(_guideView);
        HideView(_cheatsView);
    }

    private static void HideView(UguiCanvasViewBase view) => view?.HideUntilOpened();

    private T EnsureView<T>(T existing, string objectName) where T : UguiCanvasViewBase
    {
        if (existing != null)
            return existing;

        existing = GetComponentInChildren<T>(true);
        if (existing != null)
            return existing;

        var root = new GameObject(objectName, typeof(RectTransform));
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.AddComponent<GraphicRaycaster>();
        var view = root.AddComponent<T>();
        view.HideUntilOpened();
        return view;
    }
}
