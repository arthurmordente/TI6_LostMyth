using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UIElements;

public class GuideUIView : MonoBehaviour {
    [SerializeField] private UIDocument _uiDocument;
    private VisualElement _mainContainer;
    private Button _closeButton;

    [SerializeField] private string _guideLabel = "Guides";

    private VisualElement _root;
    private GuideSO _currentGuide;
    private int _currentPageIndex = 0;

    private Label _titleLabel;
    private Label _descriptionLabel;
    private VisualElement _pageImageContainer;
    private VisualElement _buttonsContainer;
    private VisualElement _pagesButtonContainer;
    private VisualElement _guidesListContainer;
    private Button _nextPageButton;
    private Button _previousPageButton;

    private readonly List<Button> _pageButtons = new List<Button>();
    private readonly List<Button> _guideButtons = new List<Button>();


    public async Awaitable InitiPoint() {
        _root = _uiDocument.rootVisualElement;
        _mainContainer = _root.Q<VisualElement>("main-container");
        _closeButton = _root.Q<Button>("exit-options-button");
        _titleLabel = _root.Q<Label>("title-label");
        _descriptionLabel = _root.Q<Label>("description-label");
        _pageImageContainer = _root.Q<VisualElement>("page-image-container");
        _buttonsContainer = _root.Q<VisualElement>("buttons-container");
        _pagesButtonContainer = _root.Q<VisualElement>("pages-button");
        _nextPageButton = _root.Q<Button>("next-page-button");
        _previousPageButton = _root.Q<Button>("previous-page-button");
        VisualElement guidesListRoot = _root.Q<VisualElement>("guide-list-scroll");
        _guidesListContainer = guidesListRoot.Q<VisualElement>("unity-content-container");


        if (_nextPageButton != null) {
            _nextPageButton.clickable.clicked += () => NavigatePage(1);
        }

        if (_previousPageButton != null) {
            _previousPageButton.clickable.clicked += () => NavigatePage(-1);
        }

        await SetUp();
    }

    private async Awaitable SetUp() {

        _guidesListContainer.Clear();
        _guideButtons.Clear();

        AsyncOperationHandle<IList<GuideSO>> loadHandle = Addressables.LoadAssetsAsync<GuideSO>(_guideLabel, null);

        await loadHandle.Task;

        if (loadHandle.Status == AsyncOperationStatus.Succeeded) {
            IList<GuideSO> allGuides = loadHandle.Result;

            foreach (GuideSO guide in allGuides) {
                Button guideButton = new Button();
                guideButton.AddToClassList("simple-button");
                guideButton.text = guide.guideTitle;

                guideButton.clickable.clicked += () => OnClickGuideButton(guide);

                _guideButtons.Add(guideButton);
                _guidesListContainer.Add(guideButton);
            }
            OnClickGuideButton(allGuides[0]);
        }
    }

    public void RegisterCallbacks() {
        _closeButton.clicked += Hide;
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

    public void OnClickGuideButton(GuideSO guide) {
        _currentGuide = guide;
        _currentPageIndex = 0;

        _titleLabel.text = guide.guideTitle;

        if (guide.Pages.Count > 0) {
            UpdatePage(guide.Pages[_currentPageIndex]);
        }
        else {
            UpdatePage(new Page { descriptionText = "Este guia está vazio.", pageSprite = null });
        }

        bool hasMultiplePages = guide.Pages.Count > 1;
        _buttonsContainer.style.display = hasMultiplePages ? DisplayStyle.Flex : DisplayStyle.None;

        ClearPageButtons();

        if (hasMultiplePages) {
            CreatePageButtons();
            UpdateNavigationState();
        }
    }

    private void ClearPageButtons() {
        foreach (var button in _pageButtons) {
            button.RemoveFromHierarchy();
        }
        _pageButtons.Clear();
    }

    private void CreatePageButtons() {
        for (int i = 0; i < _currentGuide.Pages.Count; i++) {
            Button pageButton = new Button();

            pageButton.AddToClassList("page-non-selected-button");

            int index = i;
            pageButton.clickable.clicked += () => NavigateToPage(index);

            _pagesButtonContainer.Add(pageButton);
            _pageButtons.Add(pageButton);
        }
    }

    private void NavigatePage(int delta) {
        NavigateToPage(_currentPageIndex + delta);
    }

    private void NavigateToPage(int index) {
        if (_currentGuide == null || index < 0 || index >= _currentGuide.Pages.Count) {
            return;
        }

        _currentPageIndex = index;
        UpdatePage(_currentGuide.Pages[_currentPageIndex]);

        UpdateNavigationState();
    }

    private void UpdateNavigationState() {
        if (_currentGuide == null) return;

        int totalPages = _currentGuide.Pages.Count;

        _nextPageButton.SetEnabled(_currentPageIndex < totalPages - 1);
        _previousPageButton.SetEnabled(_currentPageIndex > 0);

        for (int i = 0; i < _pageButtons.Count; i++) {
            if (i == _currentPageIndex) {
                _pageButtons[i].RemoveFromClassList("page-non-selected-button");
                _pageButtons[i].AddToClassList("page-selected-button");
            }
            else {
                _pageButtons[i].RemoveFromClassList("page-selected-button");
                _pageButtons[i].AddToClassList("page-non-selected-button");
            }
        }
    }

    public void UpdatePage(Page page) {
        _descriptionLabel.text = page.descriptionText;

        if (_pageImageContainer != null) {
            if (page.pageSprite != null) {
                _pageImageContainer.style.backgroundImage = new StyleBackground(page.pageSprite);
            }
            else {
                _pageImageContainer.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            }
        }
    }
}