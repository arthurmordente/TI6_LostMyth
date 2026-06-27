public interface ILobbyTipsUIController
{
    bool IsVisible { get; }
    void InitEntryPoint();
    void Show();
    void Hide();
    void ShowNextTip();
    void ShowPreviousTip();
}
