using Logic.Scripts.GameDomain.MVC.Abilitys;
using System;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbyUiView : MonoBehaviour {
    [SerializeField] private UIDocument _uIDocument;
    private VisualElement _root;
    private Button _playButton;
    private Button _loadButton;
    private Button _optionsButton;
    private Button _exitButton;

    public void Initialize(AbilityData data) {
        _root = _uIDocument.rootVisualElement;
        _playButton = _root.Q<Button>("play-btn");
        _loadButton = _root.Q<Button>("load-btn");
        _optionsButton = _root.Q<Button>("options-btn");
        _exitButton = _root.Q<Button>("exit-btn");

    }

    public void RegisterCallbacks(Action OnPlayButtonPressed, Action OnLoadButtonPressed,
        Action OnOptionsButtonPressed, Action OnExitButtonPressed) {
        _playButton.clicked += OnPlayButtonPressed;
        _loadButton.clicked += OnLoadButtonPressed;
        _optionsButton.clicked += OnOptionsButtonPressed;
        _exitButton.clicked += OnExitButtonPressed;
    }
}