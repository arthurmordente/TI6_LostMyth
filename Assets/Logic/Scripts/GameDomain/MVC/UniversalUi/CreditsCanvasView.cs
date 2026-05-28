using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CreditsCanvasView : UguiCanvasViewBase, ICreditsView
{
    public override bool IsVisible => base.IsVisible;

    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _bodyText;
    [TextArea(8, 24)]
    [SerializeField] private string _bodyOverride;

    protected override void Awake()
    {
        EnsureRuntimeUiIfNeeded();
        base.Awake();
        HideUntilOpened();
    }

    public void InitEntryPoint()
    {
        if (_bodyText != null)
        {
            var text = string.IsNullOrWhiteSpace(_bodyOverride) ? CreditsContent.Body : _bodyOverride;
            _bodyText.text = text;
        }
        HideUntilOpened();
    }

    private void EnsureRuntimeUiIfNeeded()
    {
        if (_bodyText != null && _closeButton != null) return;

        var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        var panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero;
        panelRt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollGo.transform.SetParent(panel.transform, false);
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.1f, 0.15f);
        scrollRt.anchorMax = new Vector2(0.9f, 0.85f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(scrollGo.transform, false);
        var contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 1200);

        var textGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(contentGo.transform, false);
        _bodyText = textGo.GetComponent<TextMeshProUGUI>();
        var textRt = _bodyText.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(16, 16);
        textRt.offsetMax = new Vector2(-16, -16);
        _bodyText.fontSize = 28;
        _bodyText.alignment = TextAlignmentOptions.TopLeft;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = contentRt;
        scroll.viewport = scrollRt;
        scroll.horizontal = false;

        var closeGo = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
        closeGo.transform.SetParent(panel.transform, false);
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.9f, 0.92f);
        closeRt.anchorMax = new Vector2(0.98f, 0.98f);
        closeRt.offsetMin = Vector2.zero;
        closeRt.offsetMax = Vector2.zero;
        _closeButton = closeGo.GetComponent<Button>();
        var closeLabel = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeLabel.transform.SetParent(closeGo.transform, false);
        var tmp = closeLabel.GetComponent<TextMeshProUGUI>();
        tmp.text = "X";
        tmp.alignment = TextAlignmentOptions.Center;
        var labelRt = tmp.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
    }

    public void RegisterCallbacks(Action onClose)
    {
        if (_closeButton != null)
            _closeButton.onClick.AddListener(() =>
            {
                Hide();
                onClose?.Invoke();
            });
    }
}
