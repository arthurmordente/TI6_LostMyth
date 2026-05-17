using System.Threading.Tasks;

namespace Logic.Scripts.GameDomain.MVC.Boss
{
    public interface IBossController
    {
        void Initialize();
        void PlanNextTurn();
        void ExecuteTurn();
        Task ExecuteTurnAsync();
        Task ExecutePrepareTurnAsync();
        Task ExecuteResolveTurnAsync();
        bool IsCasting { get; }
        int RemainingCastTurns { get; }
    }
}
