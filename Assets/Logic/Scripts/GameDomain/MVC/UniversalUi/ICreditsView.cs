using System;

public interface ICreditsView
{
    void InitEntryPoint();
    void RegisterCallbacks(Action onClose);
    void Show();
    void Hide();
}
