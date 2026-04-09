using Zenject;
using Logic.Scripts.Services.Logger.Base;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Book.Divide;

namespace Logic.Scripts.Turns {
    public class TurnFlowController : System.IDisposable {
        private readonly IActionPointsService _actionPointsService;
        private readonly IEchoService _echoService;
        private readonly TurnStateService _turnStateService;
        private readonly ICommandFactory _commandFactory;
		private readonly Logic.Scripts.GameDomain.MVC.Echo.ICloneUseLimiter _cloneUseLimiter;
		private readonly Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.IChipService _chipService;
        private readonly INaraController _naraController;
        private readonly IDivideAbilityHandler _divideAbilityHandler;

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
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips.IChipService chipService,
            IDivideAbilityHandler divideAbilityHandler) {
            _actionPointsService = actionPointsService;
            _echoService = echoService;
            _turnStateService = turnStateService;
            _commandFactory = commandFactory;
            _naraController = naraController;
			_cloneUseLimiter = cloneUseLimiter;
			_chipService = chipService;
            _divideAbilityHandler = divideAbilityHandler;
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
            // Hard lock immediately to avoid a first-frame where animations could run before BossAct begins
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();
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
            // Hard lock player at the beginning of BossAct
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();
            if (Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.IsActive) {
                LogService.Log("[Laki] DiceAttack ativo - aguardando resolução no turno da boss");
                var sp = Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.StatusProvider;
                if (sp != null) LogService.Log("[Laki] " + sp.GetStatus());
                Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackResult tfResult;
                Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.IResolver tfResolver;
                if (Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.TryResolveAnyAtBossTurn(out tfResult, out tfResolver)) {
                    UnityEngine.Debug.Log($"[Laki] DiceAttack resolved at TurnFlow begin. PlayerWon={tfResult.PlayerWon}");
                    try { tfResolver?.DestroyDiceAttackRoot(); } catch { }
                    try { Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.Reset(); } catch { }
                }
            }
            LogService.Log($"Turno {_turnNumber} - Fase: BossAct");
            _waitingBoss = true;
            await _bossActionService.ExecuteBossTurnAsync();
            OnBossCompleted();
        }

        private void OnBossCompleted() {
            if (!_active || !_waitingBoss) return;
            _waitingBoss = false;
            StartPlayerPhase();
        }

        private async void StartPlayerPhase() {
            _actionPointsService.GainTurnPoints();
            _phase = TurnPhase.PlayerAct;
            _turnMovement.ResetMovementArea();
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            // Minigame gates run before player inputs are unlocked, so gate input is never consumed as gameplay action.
            try { await Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.RunPlayerTurnGatesAsync(); } catch { }
            // Unlock player controls and animations on PlayerAct
            _naraController?.UnfreezeInputs();
            _naraController?.Unfreeeze();
            // Garantir que todos os telegraphs preparados estejam visíveis para o jogador
            Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.SetAllVisible(true);
			_cloneUseLimiter?.ResetForPlayerTurn();
            // Tick divide ability cooldown and grant Book its AP for this turn
            _divideAbilityHandler?.OnPlayerTurnStart();
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
            _divideAbilityHandler?.OnPlayerTurnEnd();
            StartEchoPhaseAsync();
        }

        public void CompletePlayerAction() {
            if (!_active || !_waitingPlayer) return;
            _waitingPlayer = false;
            _divideAbilityHandler?.OnPlayerTurnEnd();
            _turnMovement.ActivateNaraGravity();
            StartEchoPhaseAsync();
        }

        private async void StartEchoPhaseAsync() {
            _phase = TurnPhase.EchoesAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            // Lock during Echoes
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();
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
            // Lock during Environment
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();
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
