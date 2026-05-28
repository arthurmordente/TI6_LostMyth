using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using UnityEngine;
using UnityEngine.UI;

public sealed class OptionsCanvasView : UguiCanvasViewBase, IOptionsView
{
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

    public void InitEntryPoint()
    {
        Hide();
        ShowSoundPanel();
    }

    public void RegisterCallbacks()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
        if (_videoTabButton != null) _videoTabButton.onClick.AddListener(ShowVideoPanel);
        if (_soundTabButton != null) _soundTabButton.onClick.AddListener(ShowSoundPanel);
    }

    private void ShowVideoPanel()
    {
        if (_videoPanel != null) _videoPanel.SetActive(true);
        if (_soundPanel != null) _soundPanel.SetActive(false);
    }

    private void ShowSoundPanel()
    {
        if (_videoPanel != null) _videoPanel.SetActive(false);
        if (_soundPanel != null) _soundPanel.SetActive(true);
    }
}
