using System.Threading.Tasks;
using Zenject;
using Logic.Scripts.Services.Logger.Base;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Book.Divide;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.GameDomain.MVC.Environment;
using Logic.Scripts.GameDomain.MVC.Environment.Hokari;
using Logic.Scripts.GameDomain.MVC.Environment.Laki;
using Logic.Scripts.GameDomain.Services.Skills;

namespace Logic.Scripts.Turns {
    public class TurnFlowController : System.IDisposable {
        private readonly IActionPointsService _actionPointsService;
        private readonly IEchoService _echoService;
        private readonly TurnStateService _turnStateService;
        private readonly ICommandFactory _commandFactory;
		private readonly Logic.Scripts.GameDomain.MVC.Echo.ICloneUseLimiter _cloneUseLimiter;
        private readonly INaraController _naraController;
        private readonly IDivideAbilityHandler _divideAbilityHandler;
        private readonly IGamePlayUiController _gamePlayUiController;
        private readonly IRandomTurnPassiveService _randomTurnPassiveService;
        private readonly IDamageStackMovementPassiveService _damageStackMovementPassiveService;

        private IBossActionService _bossActionService;
        private IEnviromentActionService _enviromentActionService;
        private NaraTurnMovementController _turnMovement;
        private bool _active;
        private int _turnNumber;
        private bool _waitingBoss;
        private bool _waitingPlayer;
        private TurnPhase _phase;

        static bool UsesHokariArenaHazardTurnOrder =>
            CombatArenaBoundaryRuntime.EnableRingOutLoss && HokariArenaHazardTurnBridge.IsRegistered;

        static bool UsesLakiArenaTurnFlowOrder =>
            LakiArenaTurnFlowBridge.IsRegistered && !UsesHokariArenaHazardTurnOrder;

		public TurnFlowController(
            IActionPointsService actionPointsService,
            IEchoService echoService,
                        TurnStateService turnStateService,
            ICommandFactory commandFactory,
			INaraController naraController,
			Logic.Scripts.GameDomain.MVC.Echo.ICloneUseLimiter cloneUseLimiter,
            IDivideAbilityHandler divideAbilityHandler,
            [InjectOptional] IGamePlayUiController gamePlayUiController,
            [InjectOptional] IRandomTurnPassiveService randomTurnPassiveService,
            [InjectOptional] IDamageStackMovementPassiveService damageStackMovementPassiveService) {
            _actionPointsService = actionPointsService;
            _echoService = echoService;
            _turnStateService = turnStateService;
            _commandFactory = commandFactory;
            _naraController = naraController;
			_cloneUseLimiter = cloneUseLimiter;
            _divideAbilityHandler = divideAbilityHandler;
            _gamePlayUiController = gamePlayUiController;
            _randomTurnPassiveService = randomTurnPassiveService;
            _damageStackMovementPassiveService = damageStackMovementPassiveService;
        }

        public void Initialize(IBossActionService bossActionService,
            IEnviromentActionService enviromentActionService, NaraTurnMovementController naraTurnMovement) {
            _bossActionService = bossActionService;
            _enviromentActionService = enviromentActionService;
            // Must assign before StartTurns: AdvanceTurnAsync may resume synchronously after await and
            // reach StartPlayerPhase before this method would otherwise assign _turnMovement (Laki/dice path).
            _turnMovement = naraTurnMovement;
            StartTurns();
        }

        public void Dispose() {
            StopTurns();
        }

        public void StartTurns() {
            StartTurnsAfterLakiBoardIntroAsync();
        }

        private async void StartTurnsAfterLakiBoardIntroAsync() {
            if (_active) return;
            await Task.Yield();
            await LakiRouletteArenaFightIntro.WaitForBoardIntroIfNeededAsync();
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
            LakiRouletteArenaFightIntro.CancelWait();
            if (!_active) return;
            _active = false;
            _waitingBoss = false;
            _waitingPlayer = false;
            _phase = TurnPhase.None;
            _actionPointsService.Reset();
            _turnStateService.ExitTurnMode();
            _gamePlayUiController?.EndFirstTurnPassTurnHint();
            _gamePlayUiController?.SetSkillsSlidableExpanded(false);
        }

        private async void AdvanceTurnAsync() {
            if (!_active) return;
            _turnNumber += 1;
            _phase = TurnPhase.BossAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            _gamePlayUiController?.SetSkillsSlidableExpanded(false);
            // Hard lock player at the beginning of BossAct
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();
            if (!UsesLakiArenaTurnFlowOrder
                && Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.IsActive) {
                // LogService.Log("[Laki] DiceAttack ativo - aguardando resolução no turno da boss");
                var sp = Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.StatusProvider;
                // if (sp != null) LogService.Log("[Laki] " + sp.GetStatus());
                Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackResult tfResult;
                Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.IResolver tfResolver;
                if (Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.TryResolveAnyAtBossTurn(out tfResult, out tfResolver)) {
                    // UnityEngine.Debug.Log($"[Laki] DiceAttack resolved at TurnFlow begin. PlayerWon={tfResult.PlayerWon}");
                    Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.NotifyPlayerWonDiceOpensShieldWindow(tfResult, _turnNumber);
                    try { tfResolver?.DestroyDiceAttackRoot(); } catch { }
                    try { Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.Reset(); } catch { }
                }
            }
            if (UsesHokariArenaHazardTurnOrder)
            {
                // LogService.Log($"Turno {_turnNumber} - Fase: BossAct (Hokari telegraphs — resolve após jogador)");
                StartPlayerPhase();
                return;
            }

            if (UsesLakiArenaTurnFlowOrder)
            {
                if (Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.TryDismissDeferredScoreboard())
                    try { Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.Reset(); } catch { }

                // LogService.Log($"Turno {_turnNumber} - Fase: BossAct (Laki prepare)");
                _waitingBoss = true;
                await _bossActionService.ExecuteBossPrepareTurnAsync();
                OnBossCompleted();
                return;
            }

            // LogService.Log($"Turno {_turnNumber} - Fase: BossAct");
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
            _turnMovement?.ResetMovementArea();
            _randomTurnPassiveService?.ApplyPlayerTurnStart(_actionPointsService, _turnMovement);
            _damageStackMovementPassiveService?.ApplyPlayerTurnStart(_turnMovement);
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            _naraController?.UnfreezeInputs();
            _naraController?.Unfreeeze();
            _turnMovement?.LineHandlerController.SetVisible(true);
            _turnMovement?.DeactivateNaraGravity();
            _commandFactory.CreateCommandVoid<Logic.Scripts.GameDomain.Commands.RecenterNaraMovementOnPlayerTurnCommand>().Execute();
            // Dice prompt after movement is unlocked (gate may still wait for roll input).
            try { await Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack.DiceAttackRuntimeService.RunPlayerTurnGatesAsync(); } catch { }
            _gamePlayUiController?.SetSkillsSlidableExpanded(true, instant: false);
            _gamePlayUiController?.PlayPlayerTurnAnnouncement(_turnNumber);
            Logic.Scripts.GameDomain.MVC.Boss.Telegraph.TelegraphVisibilityRegistry.SetAllVisible(true);
			_cloneUseLimiter?.ResetForPlayerTurn();
            _gamePlayUiController?.SyncBookCloneActionHud();
            _divideAbilityHandler?.OnPlayerTurnStart();
            _waitingPlayer = true;
            _turnStateService.RequestPlayerAction();

            if (_turnNumber == 1)
                _gamePlayUiController?.BeginFirstTurnPassTurnHint(_turnNumber);
        }

        public void SkipTurn() {
            if (!_active || !_waitingPlayer) return;
            _waitingPlayer = false;
            _gamePlayUiController?.EndFirstTurnPassTurnHint();
            _gamePlayUiController?.SetSkillsSlidableExpanded(false);
            _divideAbilityHandler?.OnPlayerTurnEnd();
            if (UsesHokariArenaHazardTurnOrder)
                StartHokariPostPlayerResolveAsync();
            else
                StartEchoPhaseAsync();
        }

        public void CompletePlayerAction() {
            if (!_active || !_waitingPlayer) return;
            _waitingPlayer = false;
            _gamePlayUiController?.EndFirstTurnPassTurnHint();
            _gamePlayUiController?.SetSkillsSlidableExpanded(false);
            _divideAbilityHandler?.OnPlayerTurnEnd();
            _turnMovement?.ActivateNaraGravity();
            if (UsesHokariArenaHazardTurnOrder)
                StartHokariPostPlayerResolveAsync();
            else
                StartEchoPhaseAsync();
        }

        async void StartHokariPostPlayerResolveAsync()
        {
            _gamePlayUiController?.SetSkillsSlidableExpanded(false);
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();

            // LogService.Log($"Turno {_turnNumber} - Fase: Arena hazard resolve (Hokari)");
            await HokariArenaHazardTurnBridge.ExecuteScheduledForTurnAsync(_turnNumber);
            StartEchoPhaseAsync();
        }

        private async void StartEchoPhaseAsync() {
            _gamePlayUiController?.SetSkillsSlidableExpanded(false);
            _phase = TurnPhase.EchoesAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            // Lock during Echoes
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();
            // LogService.Log($"Turno {_turnNumber} - Fase: EchoesAct");
            await _echoService.ResolveDueEchoesAsync();
            OnEchoesCompleted();
        }

        private void OnEchoesCompleted() {
            if (UsesHokariArenaHazardTurnOrder)
                StartHokariEndOfTurnAsync();
            else if (UsesLakiArenaTurnFlowOrder)
                StartLakiEndOfTurnAsync();
            else
                StartEnviromentPhaseAsync();
        }

        async void StartLakiEndOfTurnAsync()
        {
            _gamePlayUiController?.SetSkillsSlidableExpanded(false);
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();
            _turnMovement?.LineHandlerController.SetVisible(false);

            // LogService.Log($"Turno {_turnNumber} - Fase: EnviromentAct (Laki apply)");
            _phase = TurnPhase.EnviromentAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            await LakiArenaTurnFlowBridge.ExecuteApplyPhaseAsync();
            await LakiArenaTurnFlowBridge.DelayPostApplyAsync();

            // LogService.Log($"Turno {_turnNumber} - Fase: BossAct (Laki resolve)");
            _phase = TurnPhase.BossAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            await _bossActionService.ExecuteBossResolveTurnAsync();
            await LakiArenaTurnFlowBridge.DelayPostBossAsync();

            // LogService.Log($"Turno {_turnNumber} - Fase: EnviromentAct (Laki reroll)");
            await LakiArenaTurnFlowBridge.ExecuteRerollPhaseAsync();

            AdvanceTurnAsync();
        }

        async void StartHokariEndOfTurnAsync()
        {
            _gamePlayUiController?.SetSkillsSlidableExpanded(false);
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();
            _turnMovement?.LineHandlerController.SetVisible(false);

            // LogService.Log($"Turno {_turnNumber} - Fase: EnviromentAct (Hokari — preparar telegraph arena)");
            _phase = TurnPhase.EnviromentAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            await _enviromentActionService.ExecuteEnviromentTurnAsync();

            // LogService.Log($"Turno {_turnNumber} - Fase: BossAct resolve (Hokari)");
            _phase = TurnPhase.BossAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            _waitingBoss = true;
            await _bossActionService.ExecuteBossTurnAsync();
            _waitingBoss = false;

            AdvanceTurnAsync();
        }

        private async void StartEnviromentPhaseAsync() {
            _gamePlayUiController?.SetSkillsSlidableExpanded(false);
            _phase = TurnPhase.EnviromentAct;
            _turnStateService.AdvanceTurn(_turnNumber, _phase);
            // Lock during Environment
            _naraController?.FreezeInputs();
            _naraController?.Freeeze();
            _naraController?.StopMovingAnim();
            // LogService.Log($"Turno {_turnNumber} - Fase: EnviromentAct");
            _turnMovement?.LineHandlerController.SetVisible(false);
            await _enviromentActionService.ExecuteEnviromentTurnAsync();
            OnEnviromentCompleted();
        }

        private void OnEnviromentCompleted() {
            AdvanceTurnAsync();
        }
    }
}
