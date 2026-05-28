using System;

public interface IOptionsView
{
    bool IsVisible { get; }
    void InitEntryPoint();
    void RegisterCallbacks();
    void Show();
    void Hide();
}
