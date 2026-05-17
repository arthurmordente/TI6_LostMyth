using Zenject;
using Logic.Scripts.GameDomain.MVC.Boss;
using System.Threading.Tasks;

namespace Logic.Scripts.Turns
{
    public class BossActionService : IBossActionService
    {
        private readonly IBossController _bossController;
        //private bool _isFirstBossTurn = true;

        public BossActionService(IBossController bossController)
        {
            _bossController = bossController;
        }

        public async void ExecuteBossTurn()
        {
            await ExecuteBossTurnAsync();
        }

        public async Task ExecuteBossTurnAsync()
        {
            _bossController.PlanNextTurn();
            await _bossController.ExecuteTurnAsync();
        }

        public async Task ExecuteBossPrepareTurnAsync()
        {
            _bossController.PlanNextTurn();
            await _bossController.ExecutePrepareTurnAsync();
        }

        public async Task ExecuteBossResolveTurnAsync()
        {
            await _bossController.ExecuteResolveTurnAsync();
        }
    }
}


