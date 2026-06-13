using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class OptionsCanvasView : UguiCanvasViewBase, IOptionsView
{
    public override bool IsVisible => base.IsVisible;

    [Header("Navigation")]
    [SerializeField] private Button _closeButton;
    [SerializeField] private Button _videoTabButton;
    [SerializeField] private Button _soundTabButton;

    [Header("Panels")]
    [SerializeField] private GameObject _videoPanel;
    [SerializeField] private GameObject _soundPanel;

    [Header("Sound (shell)")]
    [SerializeField] private Slider _generalSlider;
    [SerializeField] private Slider _bgmSlider;
    [SerializeField] private Slider _sfxSlider;

    protected override void Awake()
    {
        EnsureRuntimeUiIfNeeded();
        base.Awake();
    }

    public void InitEntryPoint()
    {
        HideUntilOpened();
    }

    public override void Show()
    {
        EnsureRuntimeUiIfNeeded();
        base.Show();
        ShowSoundPanel();
    }

    private void EnsureRuntimeUiIfNeeded()
    {
        if (_closeButton != null) return;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.2f, 0.15f);
        panelRt.anchorMax = new Vector2(0.8f, 0.85f);
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

        _soundPanel = panel;
        _videoPanel = panel;

        _generalSlider = CreateSlider(panel.transform, "Geral", 0.7f);
        _bgmSlider = CreateSlider(panel.transform, "BGM", 0.55f);
        _sfxSlider = CreateSlider(panel.transform, "SFX", 0.4f);

        var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(panel.transform, false);
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.92f, 0.92f);
        closeRt.anchorMax = new Vector2(0.99f, 0.99f);
        closeRt.offsetMin = Vector2.zero;
        closeRt.offsetMax = Vector2.zero;
        _closeButton = closeGo.GetComponent<Button>();
    }

    private static Slider CreateSlider(Transform parent, string label, float anchorY)
    {
        var row = new GameObject(label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rowRt = row.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0.08f, anchorY - 0.05f);
        rowRt.anchorMax = new Vector2(0.92f, anchorY + 0.05f);
        rowRt.offsetMin = Vector2.zero;
        rowRt.offsetMax = Vector2.zero;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(row.transform, false);
        var text = labelGo.GetComponent<Text>();
        text.text = label;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0);
        labelRt.anchorMax = new Vector2(0.25f, 1);
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;

        var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGo.transform.SetParent(row.transform, false);
        var sliderRt = sliderGo.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.28f, 0.2f);
        sliderRt.anchorMax = new Vector2(0.98f, 0.8f);
        sliderRt.offsetMin = Vector2.zero;
        sliderRt.offsetMax = Vector2.zero;
        var slider = sliderGo.GetComponent<Slider>();
        slider.minValue = 0;
        slider.maxValue = 100;
        slider.value = 50;
        return slider;
    }

    public void RegisterCallbacks()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
        if (_videoTabButton != null) _videoTabButton.onClick.AddListener(ShowVideoPanel);
        if (_soundTabButton != null) _soundTabButton.onClick.AddListener(ShowSoundPanel);
    }

    void Update()
    {
        if (!IsVisible) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Hide();
    }

    private void ShowVideoPanel()
    {
        if (_videoPanel == null) return;
        _videoPanel.SetActive(true);
        if (_soundPanel != null && _soundPanel != _videoPanel)
            _soundPanel.SetActive(false);
    }

    private void ShowSoundPanel()
    {
        if (_soundPanel == null) return;
        _soundPanel.SetActive(true);
        if (_videoPanel != null && _videoPanel != _soundPanel)
            _videoPanel.SetActive(false);
    }
}
