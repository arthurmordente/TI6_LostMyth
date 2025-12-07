using System;
using UnityEngine;
using UnityEngine.UIElements;

public class CreditsUIView : MonoBehaviour {
    [SerializeField] private UIDocument _loadUIDocument;
    private VisualElement _root;
    private VisualElement _mainContainer;
    private Button _closeButton;
    private Button _guideButton;
    private Button _loadButton;
    private Button _cheatsButton;
    private Button _exitButton;
    private Button _optionsButton;

    public void InitEntryPoint() {
        _root = _loadUIDocument.rootVisualElement;
        _mainContainer = _root.Q<VisualElement>("main-container");
        _closeButton = _root.Q<Button>("exit-options-button");
        _guideButton = _root.Q<Button>("guide-btn");
        _loadButton = _root.Q<Button>("load-btn");
        _cheatsButton = _root.Q<Button>("cheat-btn");
        _exitButton = _root.Q<Button>("exit-btn");
        _optionsButton = _root.Q<Button>("options-btn");
    }

    public void RegisterCallbacks(Action OnClikGuide, Action OnClickLoad, Action OnCheatsClick, Action OnExitClick, Action OnOptionsClick) {
        _closeButton.clicked += Hide;
        _guideButton.clicked += OnClikGuide;
        _loadButton.clicked += OnClickLoad;
        _loadButton.clicked += Hide;
        _cheatsButton.clicked += OnCheatsClick;
        _cheatsButton.clicked += Hide;
        _exitButton.clicked += OnExitClick;
        _optionsButton.clicked += OnOptionsClick;
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
