using System;

public interface ICreditsView
{
    bool IsVisible { get; }
    void InitEntryPoint();
    void RegisterCallbacks(Action onClose);
    void Show();
    void Hide();
}
