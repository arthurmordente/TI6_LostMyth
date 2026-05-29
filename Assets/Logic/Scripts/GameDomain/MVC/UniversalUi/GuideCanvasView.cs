using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

public sealed class GuideCanvasView : UguiCanvasViewBase, IGuideScreenView
{
    public override bool IsVisible => base.IsVisible;

    [SerializeField] private string _guideLabel = "Guides";
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _titleText;
    [SerializeField] private TMP_Text _descriptionText;
    [SerializeField] private Image _pageImage;
    [SerializeField] private Transform _guideListRoot;
    [SerializeField] private Button _guideButtonPrefab;
    [SerializeField] private Button _nextPageButton;
    [SerializeField] private Button _previousPageButton;

    private GuideSO _currentGuide;
    private int _currentPageIndex;
    private readonly List<Button> _guideButtons = new List<Button>();

    protected override void Awake()
    {
        base.Awake();
    }

    public async Awaitable InitEntryPoint()
    {
        HideUntilOpened();
        if (_guideListRoot == null) return;

        foreach (Transform child in _guideListRoot)
            Destroy(child.gameObject);
        _guideButtons.Clear();

        var loadHandle = Addressables.LoadAssetsAsync<GuideSO>(_guideLabel, null);
        await loadHandle.Task;

        if (loadHandle.Status != AsyncOperationStatus.Succeeded || loadHandle.Result == null || loadHandle.Result.Count == 0)
            return;

        foreach (var guide in loadHandle.Result)
        {
            var btn = _guideButtonPrefab != null
                ? Instantiate(_guideButtonPrefab, _guideListRoot)
                : CreateDefaultGuideButton(_guideListRoot);
            btn.gameObject.SetActive(true);
            var label = btn.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = guide.guideTitle;
            else
            {
                var legacy = btn.GetComponentInChildren<Text>();
                if (legacy != null) legacy.text = guide.guideTitle;
            }

            var captured = guide;
            btn.onClick.AddListener(() => SelectGuide(captured));
            _guideButtons.Add(btn);
        }

        SelectGuide(loadHandle.Result[0]);

        if (_nextPageButton != null) _nextPageButton.onClick.AddListener(() => NavigatePage(1));
        if (_previousPageButton != null) _previousPageButton.onClick.AddListener(() => NavigatePage(-1));
    }

    public void RegisterCallbacks()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
    }

    private static Button CreateDefaultGuideButton(Transform parent)
    {
        var go = new GameObject("GuideEntry", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        var text = textGo.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        var rt = textGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(8, 4);
        rt.offsetMax = new Vector2(-8, -4);
        return go.GetComponent<Button>();
    }

    private void SelectGuide(GuideSO guide)
    {
        _currentGuide = guide;
        _currentPageIndex = 0;
        if (_titleText != null) _titleText.text = guide.guideTitle;

        if (guide.Pages.Count > 0) UpdatePage(guide.Pages[0]);
        else UpdatePage(new Page { descriptionText = "Este guia está vazio.", pageSprite = null });

        UpdateNavigationState();
    }

    private void NavigatePage(int delta)
    {
        if (_currentGuide == null) return;
        var index = _currentPageIndex + delta;
        if (index < 0 || index >= _currentGuide.Pages.Count) return;
        _currentPageIndex = index;
        UpdatePage(_currentGuide.Pages[_currentPageIndex]);
        UpdateNavigationState();
    }

    private void UpdatePage(Page page)
    {
        if (_descriptionText != null) _descriptionText.text = page.descriptionText;
        if (_pageImage != null)
        {
            _pageImage.enabled = page.pageSprite != null;
            _pageImage.sprite = page.pageSprite;
        }
    }

    private void UpdateNavigationState()
    {
        if (_currentGuide == null) return;
        var total = _currentGuide.Pages.Count;
        if (_nextPageButton != null) _nextPageButton.interactable = _currentPageIndex < total - 1;
        if (_previousPageButton != null) _previousPageButton.interactable = _currentPageIndex > 0;
    }
}
