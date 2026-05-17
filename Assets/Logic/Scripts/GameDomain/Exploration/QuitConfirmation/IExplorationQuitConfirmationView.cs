namespace Logic.Scripts.GameDomain.Exploration.QuitConfirmation
{
    public interface IExplorationQuitConfirmationView
    {
        bool IsVisible { get; }
        void Show();
        void Hide();
    }
}
