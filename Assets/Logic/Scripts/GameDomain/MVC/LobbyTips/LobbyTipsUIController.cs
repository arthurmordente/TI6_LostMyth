using Logic.Scripts.Core.Mvc.LoadingScreen;
using Logic.Scripts.GameDomain.Exploration;
using UnityEngine;
using Zenject;

public sealed class LobbyTipsUIController : ILobbyTipsUIController
{
    readonly ILobbyTipsView _view;
    readonly LoadingTipPoolSO _tipPool;

    int _currentTipIndex;
    bool _modalGateActive;

    public bool IsVisible => _view != null && _view.IsVisible;

    public LobbyTipsUIController(
        ILobbyTipsView view,
        [InjectOptional] LoadingTipPoolSO tipPool = null)
    {
        _view = view;
        _tipPool = tipPool;
    }

    public void InitEntryPoint()
    {
        if (_view == null)
            return;

        if (_view is LobbyTipsPanelCanvasView canvasView)
            canvasView.InitEntryPoint();

        _view.RegisterCallbacks(Hide, ShowNextTip, ShowPreviousTip);
        _view.SetVisible(false);
    }

    public void Show()
    {
        if (_view == null || _view.IsVisible)
            return;

        if (_tipPool == null || _tipPool.ValidTipCount == 0)
        {
            Debug.LogWarning("[LobbyTips] Tip pool is empty or not assigned.");
            return;
        }

        ExplorationModalInputGate.Push();
        ExplorationInteractInputGate.Push();
        _modalGateActive = true;
        _currentTipIndex = 0;
        ShowTipAt(_currentTipIndex);
        _view.SetVisible(true);
    }

    public void Hide()
    {
        if (_view == null || !_view.IsVisible)
            return;

        _view.ClearTipInstance();
        _view.SetVisible(false);

        if (_modalGateActive)
        {
            ExplorationInteractInputGate.Pop();
            ExplorationModalInputGate.Pop();
            _modalGateActive = false;
        }
    }

    public void ShowNextTip()
    {
        if (_tipPool == null || _tipPool.ValidTipCount == 0)
            return;

        _currentTipIndex = (_currentTipIndex + 1) % _tipPool.ValidTipCount;
        ShowTipAt(_currentTipIndex);
    }

    public void ShowPreviousTip()
    {
        if (_tipPool == null || _tipPool.ValidTipCount == 0)
            return;

        _currentTipIndex = (_currentTipIndex - 1 + _tipPool.ValidTipCount) % _tipPool.ValidTipCount;
        ShowTipAt(_currentTipIndex);
    }

    void ShowTipAt(int index)
    {
        var tipPrefab = _tipPool.GetTipAt(index);
        if (tipPrefab == null)
            return;

        _view.DisplayTip(tipPrefab);
        _view.SetTipIndexLabel($"{index + 1}/{_tipPool.ValidTipCount}");
    }
}
