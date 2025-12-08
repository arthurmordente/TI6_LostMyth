using System;
using UnityEngine;
using UnityEngine.UIElements;

public class CheatsUIView : MonoBehaviour {
    [SerializeField] private UIDocument _loadUIDocument;
    private VisualElement _root;
    private VisualElement _mainContainer;

    private Toggle _lifeToggle;
    private Toggle _actionPointsToggle;
    private Toggle _movementToggle;

    private Button _closeButton;
    private Button _guideButton;
    private Button _loadButton;
    private Button _creditsButton;
    private Button _exitButton;
    private Button _optionsButton;

    public void InitEntryPoint() {
        _root = _loadUIDocument.rootVisualElement;
        _mainContainer = _root.Q<VisualElement>("main-container");
        _closeButton = _root.Q<Button>("exit-options-button");
        _guideButton = _root.Q<Button>("guide-btn");
        _loadButton = _root.Q<Button>("load-btn");
        _creditsButton = _root.Q<Button>("credits-btn");
        _exitButton = _root.Q<Button>("exit-btn");
        _optionsButton = _root.Q<Button>("options-btn");

        _lifeToggle = _root.Q<Toggle>("life-toggle");
        _actionPointsToggle = _root.Q<Toggle>("pa-toggle");
        _movementToggle = _root.Q<Toggle>("pm-toggle");
    }

    public void RegisterCallbacks(Action OnClikGuide, Action OnClickLoad, Action OnCreditsClick, Action OnExitClick, Action OnOptionsClick,
        Action<bool> OnLifeToggle, Action<bool> OnActionToggle, Action<bool> OnMovementToggle) {
        _closeButton.clicked += Hide;
        _guideButton.clicked += OnClikGuide;
        _loadButton.clicked += OnClickLoad;
        _loadButton.clicked += Hide;
        _creditsButton.clicked += OnCreditsClick;
        _creditsButton.clicked += Hide;
        _exitButton.clicked += OnExitClick;
        _optionsButton.clicked += OnOptionsClick;
        _lifeToggle.RegisterCallback<ChangeEvent<bool>>((evt) => { OnLifeToggle.Invoke(evt.newValue); });
        _actionPointsToggle.RegisterCallback<ChangeEvent<bool>>((evt) => { OnActionToggle.Invoke(evt.newValue); });
        _movementToggle.RegisterCallback<ChangeEvent<bool>>((evt) => { OnMovementToggle.Invoke(evt.newValue); });
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
