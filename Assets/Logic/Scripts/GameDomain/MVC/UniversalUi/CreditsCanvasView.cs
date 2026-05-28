using System;
using Logic.Scripts.GameDomain.MVC.Ui.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CreditsCanvasView : UguiCanvasViewBase, ICreditsView
{
    [SerializeField] private Button _closeButton;
    [SerializeField] private TMP_Text _bodyText;
    [TextArea(8, 24)]
    [SerializeField] private string _bodyOverride;

    public void InitEntryPoint()
    {
        if (_bodyText != null)
        {
            var text = string.IsNullOrWhiteSpace(_bodyOverride) ? CreditsContent.Body : _bodyOverride;
            _bodyText.text = text;
        }
        Hide();
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
