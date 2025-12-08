using System;
using UnityEngine;
using UnityEngine.UIElements;

public class GameOverUIView : MonoBehaviour {
    [SerializeField] private UIDocument _loadUIDocument;
    private VisualElement _root;
    private VisualElement _mainContainer;
    private Label _mainText;
    private Button _playButton;
    private Button _loadButton;
    private Button _exitButton;

    public void InitEntryPoint() {
        _root = _loadUIDocument.rootVisualElement;
        _mainContainer = _root.Q<VisualElement>("main-container");
        _mainText = _root.Q<Label>("final-txt");
        _playButton = _root.Q<Button>("play-btn");
        _loadButton = _root.Q<Button>("load-btn");
        _exitButton = _root.Q<Button>("exit-btn");
    }

    public void RegisterCallbacks(Action OnClickPlay, Action OnClickLoad, Action OnClickExit) {
        _playButton.clicked += OnClickPlay;
        _loadButton.clicked += OnClickLoad;
        _exitButton.clicked += OnClickExit;
    }
    public void Show(bool IsWin) {
        if (IsWin) _mainText.text = "Você Ganhou";
        else _mainText.text = "Derrotado";
        _mainContainer.RemoveFromClassList("close-container");
        _mainContainer.AddToClassList("open-container");
        _root.BringToFront();
    }

    public void Hide() {
        _mainContainer.AddToClassList("close-container");
        _mainContainer.RemoveFromClassList("open-container");
    }
}
