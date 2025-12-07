using UnityEngine;
using UnityEngine.UIElements;

public class OptionsUIView : MonoBehaviour {
    [SerializeField] private UIDocument _loadUIDocument;
    private VisualElement _root;
    private VisualElement _mainContainer;
    private Button _closeSoundButton;
    private Button _closeVideoButton;
    private Button _videoButton;
    private Button _soundButton;
    private VisualElement _videoContainer;
    private VisualElement _soundContainer;

    public void InitEntryPoint() {
        _root = _loadUIDocument.rootVisualElement;
        _mainContainer = _root.Q<VisualElement>("main-container");
        _closeSoundButton = _root.Q<Button>("exit-options-sound-button");
        _closeVideoButton = _root.Q<Button>("exit-options-video-button");
        _videoButton = _root.Q<Button>("video-btn");
        _soundButton = _root.Q<Button>("sound-btn");
        _videoContainer = _root.Q<VisualElement>("video-container");
        _soundContainer = _root.Q<VisualElement>("sound-container");
    }

    public void RegisterCallbacks() {
        _closeSoundButton.clicked += Hide;
        _closeVideoButton.clicked += Hide;
        _videoButton.clicked += ShowVideoOptions;
        _soundButton.clicked += ShowAudioOptions;
    }

    private void ShowVideoOptions() {
        _videoContainer.style.display = DisplayStyle.Flex;
        _soundContainer.style.display = DisplayStyle.None;
    }

    private void ShowAudioOptions() {
        _videoContainer.style.display = DisplayStyle.None;
        _soundContainer.style.display = DisplayStyle.Flex;
    }

    public void Show() {
        _mainContainer.RemoveFromClassList("close-container");
        _mainContainer.AddToClassList("open-container");
        _root.BringToFront();
    }

    public void Hide() {
        _mainContainer.AddToClassList("close-container");
        _mainContainer.RemoveFromClassList("open-container");
    }
}
