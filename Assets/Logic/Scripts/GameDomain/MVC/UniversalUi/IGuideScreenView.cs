public interface IGuideScreenView
{
    Awaitable InitEntryPoint();
    void RegisterCallbacks();
    void Show();
    void Hide();
}
