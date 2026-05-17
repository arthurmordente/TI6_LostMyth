using System.Threading.Tasks;

namespace Logic.Scripts.Turns
{
    public interface IBossActionService
    {
        void ExecuteBossTurn();
        Task ExecuteBossTurnAsync();
        Task ExecuteBossPrepareTurnAsync();
        Task ExecuteBossResolveTurnAsync();
    }
}



