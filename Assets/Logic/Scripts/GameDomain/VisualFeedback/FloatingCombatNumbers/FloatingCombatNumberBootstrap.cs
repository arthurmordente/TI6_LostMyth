namespace Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers
{
    public class FloatingCombatNumberBootstrap : IFloatingCombatNumberBootstrap
    {
        public FloatingCombatNumberBootstrap([Zenject.InjectOptional] IFloatingCombatNumberService service = null)
        {
            FloatingCombatNumberBridge.Bind(service);
        }
    }

    public interface IFloatingCombatNumberBootstrap { }
}