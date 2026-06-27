using Logic.Scripts.Services.CommandFactory;

public sealed class OnLobbyTipsInteractionCommand : BaseCommand, ICommandVoid
{
    private ILobbyTipsUIController _lobbyTipsUIController;

    public override void ResolveDependencies()
    {
        _lobbyTipsUIController = _diContainer.TryResolve<ILobbyTipsUIController>();
    }

    public void Execute()
    {
        _lobbyTipsUIController?.Show();
    }
}
