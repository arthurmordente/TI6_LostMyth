using System;
using Logic.Scripts.Core.Mvc.LoadingScreen;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LobbyTipsPanelCanvasView : UguiCanvasViewBase, ILobbyTipsView
{
    [SerializeField] private Transform _tipContainer;
    [SerializeField] private Button _btnClose;
    [SerializeField] private Button _btnPrevious;
    [SerializeField] private Button _btnNext;
    [SerializeField] private TMP_Text _tipIndexLabel;

    private Action _onClose;
    private Action _onNext;
    private Action _onPrevious;
    private GameObject _activeTipInstance;

    protected override void Awake()
    {
        EnsureLayoutIfNeeded();
        base.Awake();
    }

    public void InitEntryPoint()
    {
        EnsureLayoutIfNeeded();
        HideUntilOpened();

        _btnClose = ResolveButton(_btnClose, transform, "btn_Close");
        _btnPrevious = ResolveButton(_btnPrevious, transform, "btn_Previous");
        _btnNext = ResolveButton(_btnNext, transform, "btn_Next");

        if (_btnClose != null)
        {
            _btnClose.onClick.RemoveAllListeners();
            _btnClose.onClick.AddListener(() => _onClose?.Invoke());
        }

        if (_btnPrevious != null)
        {
            _btnPrevious.onClick.RemoveAllListeners();
            _btnPrevious.onClick.AddListener(() => _onPrevious?.Invoke());
        }

        if (_btnNext != null)
        {
            _btnNext.onClick.RemoveAllListeners();
            _btnNext.onClick.AddListener(() => _onNext?.Invoke());
        }
    }

    public void RegisterCallbacks(Action onClose, Action onNext, Action onPrevious)
    {
        _onClose = onClose;
        _onNext = onNext;
        _onPrevious = onPrevious;
    }

    public void SetVisible(bool visible)
    {
        if (visible)
            Show();
        else
            Hide();
    }

    public void DisplayTip(LoadingTipCanvasView tipPrefab)
    {
        ClearTipInstance();
        var container = ResolveTipContainer();
        if (tipPrefab == null || container == null)
            return;

        _activeTipInstance = Instantiate(tipPrefab.gameObject, container);
        Stretch(_activeTipInstance.transform as RectTransform);
        _activeTipInstance.GetComponent<LoadingTipCanvasView>()?.OnSpawned();
    }

    public void ClearTipInstance()
    {
        if (_activeTipInstance == null)
            return;

        Destroy(_activeTipInstance);
        _activeTipInstance = null;
    }

    public void SetTipIndexLabel(string label)
    {
        if (_tipIndexLabel == null)
            return;

        _tipIndexLabel.text = label ?? string.Empty;
        _tipIndexLabel.gameObject.SetActive(!string.IsNullOrEmpty(label));
    }

    public override void Hide()
    {
        ClearTipInstance();
        base.Hide();
    }

    void EnsureLayoutIfNeeded()
    {
        if (_tipContainer != null && !_tipContainer.gameObject.scene.IsValid())
            _tipContainer = null;

        if (_tipContainer != null && _btnClose != null && _btnPrevious != null && _btnNext != null)
            return;

        var panel = transform.Find("Panel");
        if (panel == null)
        {
            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(transform, false);
            Stretch(panelGo.GetComponent<RectTransform>());
            panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            panel = panelGo.transform;
        }

        if (_tipContainer == null)
        {
            var container = panel.Find("TipContainer");
            if (container == null)
            {
                var containerGo = new GameObject("TipContainer", typeof(RectTransform));
                containerGo.transform.SetParent(panel, false);
                var rect = containerGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.08f, 0.18f);
                rect.anchorMax = new Vector2(0.92f, 0.92f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                container = containerGo.transform;
            }

            _tipContainer = container;
        }

        if (_btnClose == null)
            _btnClose = CreateChromeButton(panel, "btn_Close", "Fechar", new Vector2(0.08f, 0.05f), new Vector2(0.24f, 0.14f));

        if (_btnPrevious == null)
            _btnPrevious = CreateChromeButton(panel, "btn_Previous", "<", new Vector2(0.68f, 0.05f), new Vector2(0.76f, 0.14f));

        if (_btnNext == null)
            _btnNext = CreateChromeButton(panel, "btn_Next", ">", new Vector2(0.78f, 0.05f), new Vector2(0.86f, 0.14f));

        if (_tipIndexLabel == null)
        {
            var labelGo = new GameObject("txt_TipIndex", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(panel, false);
            var rect = labelGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.05f);
            rect.anchorMax = new Vector2(0.65f, 0.14f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _tipIndexLabel = labelGo.GetComponent<TextMeshProUGUI>();
            _tipIndexLabel.alignment = TextAlignmentOptions.Center;
            _tipIndexLabel.fontSize = 24f;
            _tipIndexLabel.color = Color.white;
        }
    }

    Transform ResolveTipContainer()
    {
        EnsureLayoutIfNeeded();

        if (_tipContainer != null && _tipContainer.gameObject.scene.IsValid())
            return _tipContainer;

        var container = transform.Find("Panel/TipContainer");
        if (container != null)
            _tipContainer = container;

        return _tipContainer;
    }

    static Button CreateChromeButton(Transform panel, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(panel, false);
        var rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        buttonGo.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buttonGo.transform, false);
        Stretch(textGo.GetComponent<RectTransform>());
        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 28f;
        text.color = Color.white;

        return buttonGo.GetComponent<Button>();
    }

    static void Stretch(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }
}
