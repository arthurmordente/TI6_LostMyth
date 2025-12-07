using System;
using UnityEngine;
using UnityEngine.UIElements;

public class PauseUiView : MonoBehaviour
{
    [SerializeField] private UIDocument _loadUIDocument;
    private VisualElement _root;
    private VisualElement _mainContainer;
    private Button _guideButton;
    private Button _optionsButton;
    private Button _loadButton;
    private Button _cheatsButton;
    private Button _resumeButton;
    private Button _libraryButton;


    public void InitEntryPoint() {
        _root = _loadUIDocument.rootVisualElement;
        _mainContainer = _root.Q<VisualElement>("main-container");
        _guideButton = _root.Q<Button>("guide-btn");
        _optionsButton = _root.Q<Button>("options-btn");
        _loadButton = _root.Q<Button>("load-btn");
        _cheatsButton = _root.Q<Button>("cheat-btn");
        _resumeButton = _root.Q<Button>("return-btn");
        _libraryButton = _root.Q<Button>("lobby-btn");
    }

    public void RegisterCallbacks(Action OnClikGuide, Action OnOptionsClick, Action OnLoadClick, Action OnCheatsClick, Action OnResumeClick, Action OnLobbyClick) {
        _guideButton.clicked += OnClikGuide;
        _optionsButton.clicked += OnOptionsClick;
        _loadButton.clicked += OnLoadClick;
        _cheatsButton.clicked += OnCheatsClick;
        _resumeButton.clicked += OnResumeClick;
        _libraryButton.clicked += OnLobbyClick;
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
