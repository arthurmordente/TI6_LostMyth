using Logic.Scripts.Services.CommandFactory;

/// <summary>
/// Previously: create slow echo. Clone-2 slot is no longer used in the Book system.
/// Kept as a no-op so existing input bindings in the Unity asset don't break.
/// Can be repurposed in a future feature.
/// </summary>
public class CreateCopy2InputCommand : BaseCommand, ICommandVoid {
    public override void ResolveDependencies() { }
    public void Execute() { }
}
