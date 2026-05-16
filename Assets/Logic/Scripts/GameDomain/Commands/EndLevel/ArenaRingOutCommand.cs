using Logic.Scripts.Services.CommandFactory;

public class ArenaRingOutCommand : BaseCommand, ICommandVoid
{
    private ICommandFactory _commandFactory;

    public override void ResolveDependencies()
    {
        _commandFactory = _diContainer.Resolve<ICommandFactory>();
    }

    public void Execute()
    {
        _commandFactory.CreateCommandVoid<GameOverCommand>()
            .SetData(new GameOverCommandData(false))
            .Execute();
    }
}
