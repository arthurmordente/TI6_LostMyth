using UnityEngine;

public interface IGuideScreenView
{
    bool IsVisible { get; }
    Awaitable InitEntryPoint();
    void RegisterCallbacks();
    void Show();
    void Hide();
}
