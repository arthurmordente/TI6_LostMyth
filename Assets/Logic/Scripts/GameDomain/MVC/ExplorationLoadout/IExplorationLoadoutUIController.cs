public interface IExplorationLoadoutUIController
{
    bool IsVisible { get; }
    void InitEntryPoint();
    void Toggle();
    void Show();
    void Hide();
}
