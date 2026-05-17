namespace Logic.Scripts.GameDomain.Exploration.QuitConfirmation
{
    public interface IExplorationQuitConfirmationService
    {
        bool IsVisible { get; }
        void HandleEscapePressed();
        void ProcessDismissInput();
        void ForceHide();
    }
}
