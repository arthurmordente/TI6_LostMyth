using Zenject;
using Logic.Scripts.Services.Logger.Base;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.GameDomain.MVC.Nara;

namespace Logic.Scripts.Turns {
    public class TurnFlowController : System.IDisposable {
        private readonly IActionPointsService _actionPointsService;
        private readonly IEchoService _echoService;
        private readonly TurnStateService _turnStateService;
        private readonly ICommandFactory _commandFactory;
		private readonly Logic.Scripts.GameDomain.MVC.Echo.ICloneUseLimiter _cloneUseLimiter;
		private readonly Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.IChipService _chipService;

        private IBossActionService _bossActionService;
        private IEnviromentActionService _enviromentActionService;
        private NaraTurnMovementController _turnMovement;
        private bool _active;
        private int _turnNumber;
        private bool _waitingBoss;
        private bool _waitingPlayer;
        private TurnPhase _phase;

		public TurnFlowController(
            IActionPointsService actionPointsService,
            IEchoService echoService,
                        TurnStateService turnStateService,
            ICommandFactory commandFactory,
			INaraController naraController,
			Logic.Scripts.GameDomain.MVC.Echo.ICloneUseLimiter cloneUseLimiter,
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.IChipService chipService) {
            _actionPointsService = actionPointsService;
            _echoService = echoService;
            _turnStateService = turnStateService;
            _commandFactory = commandFactory;
			_cloneUseLimiter = cloneUseLimiter;
			_chipService = chipService;
        }

        public void Initialize(IBossActionService bossActionService,
            IEnviromentActionService enviromentActionService, NaraTurnMovementController naraTurnMovement) {
            _bossActionService = bossActionService;
            _enviromentActionService = enviromentActionService;
            StartTurns();
            _turnMovement = naraTurnMovement;
        }

        public void Dispose() {
            StopTurns();
        }

        public void StartTurns() {
            if (_active) return;
            _active = true;
            _turnNumber = 0;
            _phase = TurnPhase.None;
            _actionPointsService.Reset();
            _turnStateService.EnterTurnMode();
            AdvanceTurnAsync();
        }

        public void StopTurns() {
            if (!_active) return;
            _active = false;
            _waitingBoss = false;
            _waitingPlayer = false;
            _phase = TurnPhase.None;
            _actionPointsService.Reset();
            _turnStateService.ExitTurnMode();
        }

        private async void AdvanceTurnAsync() {
            if (!_active) return;
            _turnNumber += 1;
            _phase = TurnPhase.BossAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            try { _chipService?.Refresh(); UnityEngine.Debug.Log("[Laki][ChipsUI] Turn start -> Refresh chips"); } catch { }
            if (Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.IsActive) {
                LogService.Log("[Laki] Minigame ativo - boss pausado (apenas resolução/arena)");
                var sp = Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.StatusProvider;
                if (sp != null) LogService.Log("[Laki] " + sp.GetStatus());
                Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameResult tfResult;
                Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.IMinigameResolver tfResolver;
                if (Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.TryResolveAnyAtBossTurn(out tfResult, out tfResolver)) {
                    UnityEngine.Debug.Log($"[Laki] Resolved at TurnFlow begin. PlayerWon={tfResult.PlayerWon} Chips: P+={tfResult.PlayerChipsDelta}, B+={tfResult.BossChipsDelta}");
                    try {
                        _chipService?.ApplyMinigameResult(tfResult);
                        _chipService?.Refresh();
                        UnityEngine.Debug.Log("[Laki][ChipsUI] TurnFlow applied result and refreshed UI via injected service");
                    } catch { }
                    try { tfResolver?.DestroyMinigameRoot(); } catch { }
                }
            }
            LogService.Log($"Turno {_turnNumber} - Fase: BossAct");
            _waitingBoss = true;
            await _bossActionService.ExecuteBossTurnAsync();
            await System.Threading.Tasks.Task.Delay(1500);
            OnBossCompleted();
        }

        private void OnBossCompleted() {
            if (!_active || !_waitingBoss) return;
            _waitingBoss = false;
            StartPlayerPhase();
        }

        private void StartPlayerPhase() {
            _actionPointsService.GainTurnPoints();
            _phase = TurnPhase.PlayerAct;
            _turnMovement.ResetMovementArea();
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
			_cloneUseLimiter?.ResetForPlayerTurn();
            LogService.Log($"Turno {_turnNumber} - Fase: PlayerAct");
            _waitingPlayer = true;
            _commandFactory.CreateCommandVoid<Logic.Scripts.GameDomain.Commands.RecenterNaraMovementOnPlayerTurnCommand>().Execute();
            _turnMovement?.LineHandlerController.SetVisible(true);
            _turnMovement.DeactivateNaraGravity();
            _turnStateService.RequestPlayerAction();
        }

        public void SkipTurn() {
            if (!_active || !_waitingPlayer) return;
            _waitingPlayer = false;
            StartEchoPhaseAsync();
        }

        public void CompletePlayerAction() {
            if (!_active || !_waitingPlayer) return;
            _waitingPlayer = false;
            _turnMovement.ActivateNaraGravity();
            StartEchoPhaseAsync();
        }

        private async void StartEchoPhaseAsync() {
            _phase = TurnPhase.EchoesAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            LogService.Log($"Turno {_turnNumber} - Fase: EchoesAct");
            await _echoService.ResolveDueEchoesAsync();
            OnEchoesCompleted();
        }

        private void OnEchoesCompleted() {
            StartEnviromentPhaseAsync();
        }

        private async void StartEnviromentPhaseAsync() {
            _phase = TurnPhase.EnviromentAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            LogService.Log($"Turno {_turnNumber} - Fase: EnviromentAct");
            _turnMovement?.LineHandlerController.SetVisible(false);
            await _enviromentActionService.ExecuteEnviromentTurnAsync();
            OnEnviromentCompleted();
        }

        private void OnEnviromentCompleted() {
            AdvanceTurnAsync();
        }
    }
}
