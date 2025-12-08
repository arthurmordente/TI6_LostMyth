using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Turns;

namespace Logic.Scripts.GameDomain.Commands {
    public class CompletePlayerActionCommand : BaseCommand, ICommandVoid {
        private TurnFlowController _turnFlowController;
        private INaraController _naraController;

        public override void ResolveDependencies() {
            _turnFlowController = _diContainer.Resolve<TurnFlowController>();
            _naraController = _diContainer.Resolve<INaraController>();
        }

        public void Execute() {
            _turnFlowController.CompletePlayerAction();
            _naraController.FreezeInputs();
        }
    }
}
