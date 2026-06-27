using System;
using Logic.Scripts.Core.Mvc.LoadingScreen;
using UnityEngine;

public interface ILobbyTipsView
{
    bool IsVisible { get; }
    void SetVisible(bool visible);
    void RegisterCallbacks(Action onClose, Action onNext, Action onPrevious);
    void DisplayTip(LoadingTipCanvasView tipPrefab);
    void ClearTipInstance();
    void SetTipIndexLabel(string label);
}
