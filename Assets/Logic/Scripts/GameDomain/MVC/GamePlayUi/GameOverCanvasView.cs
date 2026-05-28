using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class GameOverCanvasView : UguiCanvasViewBase, IGameOverView
{
    [SerializeField] private TMP_Text _resultText;
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _loadButton;
    [SerializeField] private Button _exitButton;

    protected override void Awake()
    {
        EnsureRuntimeUiIfNeeded();
        base.Awake();
    }

    public void InitEntryPoint() => Hide();

    private void EnsureRuntimeUiIfNeeded()
    {
        if (_playButton != null) return;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

        var titleGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGo.transform.SetParent(panel.transform, false);
        _resultText = titleGo.GetComponent<TextMeshProUGUI>();
        var titleRt = _resultText.rectTransform;
        titleRt.anchorMin = new Vector2(0.2f, 0.55f);
        titleRt.anchorMax = new Vector2(0.8f, 0.75f);
        titleRt.offsetMin = Vector2.zero;
        titleRt.offsetMax = Vector2.zero;
        _resultText.fontSize = 48;
        _resultText.alignment = TextAlignmentOptions.Center;

        _playButton = CreateButton(panel.transform, "Jogar novamente", 0.42f);
        _exitButton = CreateButton(panel.transform, "Sair", 0.32f);
    }

    private static Button CreateButton(Transform parent, string label, float anchorY)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.35f, anchorY);
        rt.anchorMax = new Vector2(0.65f, anchorY + 0.08f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);
        var tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.alignment = TextAlignmentOptions.Center;
        var textRt = tmp.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        return go.GetComponent<Button>();
    }

    public void RegisterCallbacks(Action onPlay, Action onLoad, Action onExit)
    {
        if (_playButton != null) _playButton.onClick.AddListener(() => onPlay?.Invoke());
        if (_loadButton != null) _loadButton.onClick.AddListener(() => onLoad?.Invoke());
        if (_exitButton != null) _exitButton.onClick.AddListener(() => onExit?.Invoke());
    }

    public void Show(bool isWin)
    {
        if (_resultText != null)
            _resultText.text = isWin ? "Você Ganhou" : "Derrotado";
        Show();
    }
}
