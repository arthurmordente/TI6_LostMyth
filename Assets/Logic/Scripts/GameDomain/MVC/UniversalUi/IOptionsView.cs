using System;

public interface IOptionsView
{
    void InitEntryPoint();
    void RegisterCallbacks();
    void Show();
    void Hide();
}
