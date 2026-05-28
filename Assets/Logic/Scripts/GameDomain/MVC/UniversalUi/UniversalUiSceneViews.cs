using UnityEngine;

/// <summary>
/// Single hook on GameScene for shared overlay canvases (Options, Credits, Load, Guide, Cheats).
/// Assign child views in the Inspector or leave null until prefabs exist.
/// </summary>
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
}
