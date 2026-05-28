using Logic.Scripts.Services.CommandFactory;

/// <summary>
/// ESC durante pause no combate: fecha overlay (créditos/opções) primeiro; só depois despausa.
/// </summary>
public class DismissGameplayPauseEscapeInputCommand : BaseCommand, ICommandVoid
{
    private IUniversalUIController _universalUIController;
    private ICommandFactory _commandFactory;

    public override void ResolveDependencies()
    {
        _universalUIController = _diContainer.Resolve<IUniversalUIController>();
        _commandFactory = _diContainer.Resolve<ICommandFactory>();
    }

    public void Execute()
    {
        if (_universalUIController.TryCloseTopOverlay())
            return;

        _commandFactory.CreateCommandVoid<ResumeGameplayInputCommand>().Execute();
    }
}
