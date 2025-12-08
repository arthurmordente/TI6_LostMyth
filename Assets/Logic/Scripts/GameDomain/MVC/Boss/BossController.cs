using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.ResourcesLoaderService;
using Logic.Scripts.Services.UpdateService;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using UnityEngine;
using Zenject;
using System;
using System.Threading.Tasks;
using Object = UnityEngine.Object;
using Logic.Scripts.GameDomain.MVC.Ui;

namespace Logic.Scripts.GameDomain.MVC.Boss {
    [Serializable]
    public class BossController : IBossController, IFixedUpdatable, IInitializable, IEffectable, IDisposable {
        private readonly IUpdateSubscriptionService _updateSubscriptionService;
        private readonly IAudioService _audioService;
        private readonly ICommandFactory _commandFactory;
        private readonly IResourcesLoaderService _resourcesLoaderService;
        private readonly IGamePlayUiController _gamePlayUiController;

        private BossView _bossView;
        private readonly BossView _bossViewPrefab;
        private readonly BossData _bossData;
        private readonly BossConfigurationSO _bossConfiguration;
        private readonly BossPhasesSO _bossPhases;
        private readonly IBossAbilityController _bossAbilityController;
        private ArenaPosReference _arenaReference;
        private BossBehaviorSO _activeBehavior;
        private int _currentPhaseIndex;

        private Rigidbody _bossRigidbody;
        private Transform _bossTransform;

		private enum BossMoveMode { None, Forward, TowardPlayer, Random, TowardArenaCenter }
        private BossMoveMode _moveMode;
        private Vector3 _fixedDirection;
        private float _moveSpeed;
        private float _rotationSpeed;
        private float _randomDirTimeRemaining;
        private float _randomDirDuration;
        private Vector3 _randomDirection;

        private float _turnMoveDistanceBudget;
        private int _executedTurnsCount;

        private BossPlan _currentPlan;
        private bool _isCasting;
        private int _remainingCastTurns;
        private int _loopingAttackAnimId = -1;
        private bool _movingAnimActive = false;
        private bool _movementAllowed = false;
        private bool _movementGatePending = false;
        private Vector3 _plannedMoveDirection = Vector3.zero;
        private bool _hasPlannedMoveDirection = false;
        private struct PendingCast { public BossAttack Attack; public int TurnsRemaining; }
        private System.Collections.Generic.List<PendingCast> _pendingCasts;
        private bool _registeredFixed;

        public bool IsCasting => _isCasting;
        public int RemainingCastTurns => _remainingCastTurns;

        public BossController(IUpdateSubscriptionService updateSubscriptionService,
            IAudioService audioService, ICommandFactory commandFactory,
            IResourcesLoaderService resourcesLoaderService, BossView bossViewPrefab,
            BossConfigurationSO bossConfiguration, BossPhasesSO bossPhases,
            IBossAbilityController bossAbilityController, IGamePlayUiController gamePlayUiController) {
            _updateSubscriptionService = updateSubscriptionService;
            _audioService = audioService;
            _commandFactory = commandFactory;
            _resourcesLoaderService = resourcesLoaderService;
            _bossViewPrefab = bossViewPrefab;
            _bossConfiguration = bossConfiguration;
            _bossPhases = bossPhases;
            _bossAbilityController = bossAbilityController;
            _gamePlayUiController = gamePlayUiController;
            _bossData = new BossData(_bossConfiguration);
        }

        public void Initialize() {
            _isCasting = false;
            _remainingCastTurns = 0;
            _pendingCasts = new System.Collections.Generic.List<PendingCast>();
            _updateSubscriptionService.RegisterFixedUpdatable(this);
            _registeredFixed = true;
            CreateBoss();
            _arenaReference = Object.FindFirstObjectByType<ArenaPosReference>();
            // Initialize UI with correct percentages and absolute values
            int maxHp = _bossConfiguration != null ? _bossConfiguration.MaxHealth : Mathf.Max(1, _bossData.ActualHealth);
            int pct = maxHp > 0 ? Mathf.RoundToInt((float)_bossData.ActualHealth / maxHp * 100f) : 0;
            _gamePlayUiController.OnActualBossHealthChange(pct);
            _gamePlayUiController.OnPreviewBossHealthChange(pct);
            _gamePlayUiController.OnActualBossLifeChange(_bossData.ActualHealth);
            if (_bossView != null) {
                _bossView.SetMoving(false);
            }
            _activeBehavior = GetBehaviorForPhaseIndex(0);
            _currentPhaseIndex = -1;
            _ = EvaluateAndMaybeSwitchPhaseAsync();
            if (_activeBehavior != null) {
                _bossAbilityController.SetBehavior(_activeBehavior);
            }
        }

		public void ManagedFixedUpdate() {
			if (_bossRigidbody == null || _bossTransform == null) return;

            Vector3 worldDir = Vector3.zero;
            switch (_moveMode) {
                case BossMoveMode.Forward: {
                        worldDir = _bossTransform.forward;
                        break;
                    }
                case BossMoveMode.TowardPlayer: {
                        GameObject target = FindPlayerTarget();
                        if (target != null) {
                            Vector3 toPlayer = target.transform.position - _bossTransform.position;
                            toPlayer.y = 0f;
                            if (toPlayer.sqrMagnitude > 0.0001f) worldDir = toPlayer.normalized;
                        }
                        break;
                    }
                case BossMoveMode.Random: {
                        if (_randomDirTimeRemaining <= 0f || _randomDirection.sqrMagnitude < 0.0001f) {
                            float angle = UnityEngine.Random.Range(0f, 360f);
                            float rad = angle * Mathf.Deg2Rad;
                            _randomDirection = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
                            _randomDirTimeRemaining = _randomDirDuration;
                        }
                        else {
                            _randomDirTimeRemaining -= Time.fixedDeltaTime;
                        }
                        worldDir = _randomDirection;
                        break;
                    }
				case BossMoveMode.TowardArenaCenter: {
						Vector3 center = _arenaReference != null ? _arenaReference.transform.position : Vector3.zero;
						Vector3 toCenter = center - _bossTransform.position;
						toCenter.y = 0f;
						if (toCenter.sqrMagnitude > 0.0001f) worldDir = toCenter.normalized;
						break;
					}
                case BossMoveMode.None:
                default:
                    break;
            }

			bool hasDir = worldDir.sqrMagnitude > 0.0001f;
			// Desejo de animar movimento depende só do orçamento do turno (evita flicker por variação momentânea de direção)
			bool movingAnimDesired = _turnMoveDistanceBudget > 0f;

			// Movement animation state machine: start prep when movement actually begins; finish when it ends
			if (movingAnimDesired && !_movingAnimActive) {
                if (_bossView != null) { _bossView.PlayMovePrep(); _bossView.SetMoving(true); }
				_movingAnimActive = true;
                _movementAllowed = false;
                if (!_movementGatePending) { _movementGatePending = true; BeginMovementGateAsync(); }
			} else if (!movingAnimDesired && _movingAnimActive) {
                if (_bossView != null) { _bossView.SetMoving(false); _bossView.PlayMoveFinish(); }
				_movingAnimActive = false;
                _movementAllowed = false;
                _movementGatePending = false;
			}

			// Translada apenas quando liberado pelo gate e com direção planejada
			if (_turnMoveDistanceBudget > 0f && _movementAllowed && _hasPlannedMoveDirection) {
				float step = _moveSpeed * Time.fixedDeltaTime;
				if (step > _turnMoveDistanceBudget) step = _turnMoveDistanceBudget;
				_turnMoveDistanceBudget -= step;
				Vector3 fromPos = _bossTransform.position;
				Vector3 toPos = fromPos + _plannedMoveDirection * step;
				Vector3 toPlanar = new Vector3(toPos.x, fromPos.y, toPos.z);
				_bossRigidbody.MovePosition(toPlanar);
			}
        }

        public void DisableCallbacks() {
            _bossView.RemoveAllCallbacks();
        }

        public void CreateBoss() {
			// Instantiate boss directly at configured world position to avoid transient prefab-position frames
			Vector3 spawnPos = (_bossConfiguration != null) ? _bossConfiguration.InitialBossPosition : Vector3.zero;
			Quaternion spawnRot = (_bossViewPrefab != null) ? _bossViewPrefab.transform.rotation : Quaternion.identity;
			_bossView = Object.Instantiate(_bossViewPrefab, spawnPos, spawnRot);
            _bossData.ResetData();
            _bossView.SetupCallbacks(PreviewHeal, PreviewDamage, TakeDamage, Heal);
            _bossRigidbody = _bossView != null ? _bossView.GetRigidbody() : null;
            _bossTransform = _bossView != null ? _bossView.transform : null;
            if (_bossView != null) {
                var relay = _bossView.gameObject.GetComponent<Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay>();
                if (relay == null) relay = _bossView.gameObject.AddComponent<Assets.Logic.Scripts.GameDomain.Effects.EffectableRelay>();
                relay.Init(this);
            }

            if (_bossRigidbody != null) {
                _bossRigidbody.useGravity = false;
                _bossRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                _bossRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                _bossRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            }

            _moveSpeed = _bossConfiguration != null ? _bossConfiguration.MoveSpeed : 5f;
            _rotationSpeed = _bossConfiguration != null ? _bossConfiguration.RotationSpeed : 5f;
            var behavior = GetCurrentBehavior();
            _randomDirDuration = behavior != null ? behavior.RandomChangeDirectionSeconds : 1.5f;
            // Do not move immediately after spawn; movement will be configured by turn flow
            _moveMode = BossMoveMode.None;
        }
        public void PlanNextTurn() { }

        public void ExecuteTurn() {
            // 1) Resolver pendências: decrementa e executa as que chegaram a 0
            ResolvePendingCasts();

            // 2) Definir movimento deste turno via padrão configurável
            ConfigureTurnMovement();

            _audioService.PlayAudio(AudioClipType.BossMove1SFX, AudioChannelType.Fx, AudioPlayType.OneShot);

            // 3) Preparar ataque deste turno conforme padrão do behavior
            _ = QueuePreparedAttackFromBehaviorAsync();
            _executedTurnsCount++;
        }

        public async Task ExecuteTurnAsync() {
            bool resolvedThisTurn = false;
            bool pauseThisTurn = false;
            // Tentativa de resolução de minigame no início do turno do boss
            if (Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.TryResolveAnyAtBossTurn(
                out Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameResult mgResult,
                out Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.IMinigameResolver mgResolver))
            {
                Debug.Log($"[Laki] Minigame resolved at Boss turn. PlayerWon={mgResult.PlayerWon} Chips: P+={mgResult.PlayerChipsDelta}, B+={mgResult.BossChipsDelta}");
                try { mgResolver?.DestroyMinigameRoot(); } catch { }
                resolvedThisTurn = true;
            }
            pauseThisTurn = Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.ConsumePauseBossThisTurn();
            if (Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.IsActive || resolvedThisTurn || pauseThisTurn) {
                Debug.Log("[Laki] Minigame active/resolved - boss attacks paused this turn");
            }
            ResolvePendingCasts();
            if (!Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.IsActive && !resolvedThisTurn && !pauseThisTurn) {
                await ExecutePreparedActionAsync();
            }
            // After executing the previously prepared action, evaluate phase change for this turn
            bool didTransition = await EvaluateAndMaybeSwitchPhaseAsync();
            await MoveTurnAsync();
            // Garante Idle pós movimento antes de preparar o próximo ataque
            if (_bossView != null) {
                await _bossView.WaitUntilIdleAsync(3f);
            }
            await PrepareNextActionAsync();
            // Espera entrar no estado de AttackLoop do ataque preparado (para o próximo round) antes de encerrar o turno
            if (_bossView != null) {
                await _bossView.WaitUntilAttackLoopAsync(3f);
            }
            _executedTurnsCount++;
        }

		private async Task ExecutePreparedActionAsync() {
			if (_pendingCasts == null || _pendingCasts.Count == 0) return;

            // Fechar loop do ataque anterior e tocar finish no início do novo turno
            if (_loopingAttackAnimId >= 0 && _bossView != null) {
                _bossView.SetAttackLoop(false);
                _bossView.PlayAttackFinish();
                _loopingAttackAnimId = -1;
            }

			// Coleta todos os ataques devidos neste turno
			System.Collections.Generic.List<BossAttack> toExecute = new System.Collections.Generic.List<BossAttack>();
			for (int i = _pendingCasts.Count - 1; i >= 0; i--) {
				if (_pendingCasts[i].TurnsRemaining <= 0) {
					BossAttack castNow = _pendingCasts[i].Attack;
					_pendingCasts.RemoveAt(i);
					if (castNow != null) {
						toExecute.Add(castNow);
					}
				}
			}
			if (toExecute.Count == 0) return;

			// Exclusividade de deslocamento (push/pull): escolher 1 vencedor por prioridade
			BossAttack winner = null;
			int best = int.MinValue;
			for (int k = 0; k < toExecute.Count; k++) {
				BossAttack a = toExecute[k];
				if (a != null && a.HasDisplacementEffect()) {
					int pri = a.GetDisplacementPriority();
					if (pri > best) { best = pri; winner = a; }
				}
			}
			for (int k = 0; k < toExecute.Count; k++) {
				BossAttack a = toExecute[k];
				if (a == null) continue;
				if (winner != null && !object.ReferenceEquals(a, winner) && a.HasDisplacementEffect()) {
					a.SetDisplacementEnabled(false);
				}
			}

            // Executa todos simultaneamente (aplicação deve ocorrer quando a animação de finish toca, ou seja, logo após dispará-la acima)
			System.Collections.Generic.List<System.Threading.Tasks.Task> tasks = new System.Collections.Generic.List<System.Threading.Tasks.Task>(toExecute.Count);
			for (int k = 0; k < toExecute.Count; k++) {
				try {
					tasks.Add(toExecute[k].ExecuteAsync());
				} catch (Exception) { }
			}
            try {
                await System.Threading.Tasks.Task.WhenAll(tasks);
            } catch (Exception) { }
            // Após aplicar, aguarda o término do finish e retorno ao Idle via ResetState
            if (_bossView != null) {
                await _bossView.WaitUntilIdleAsync(4f);
            }
            // Não forçar Idle aqui; confiamos no Animator (finish -> reset -> idle)
		}

        private async Task MoveTurnAsync() {
            ConfigureTurnMovement();
            if (_turnMoveDistanceBudget <= 0f) return;
            float elapsed = 0f;
            const float moveTimeoutSeconds = 5f;
            const float stopDistance = 1.2f;
            while (_turnMoveDistanceBudget > 0f) {
                if (_moveMode == BossMoveMode.TowardPlayer) {
                    GameObject target = FindPlayerTarget();
                    if (target != null && _bossTransform != null) {
                        Vector3 delta = target.transform.position - _bossTransform.position;
                        delta.y = 0f;
                        float dist = delta.magnitude;
                        if (dist <= stopDistance) {
                            _turnMoveDistanceBudget = 0f;
                            break;
                        }
                    }
                }
                // Timeout de segurança para não ficar preso em loop de movimento
                elapsed += Time.deltaTime;
                if (elapsed >= moveTimeoutSeconds) {
                    Debug.LogWarning($"[BossMove] Movement timeout reached ({moveTimeoutSeconds:0.##}s). Forcing finish.");
                    _turnMoveDistanceBudget = 0f;
                    break;
                }
                await Task.Yield();
            }
        }

        // Habilita a locomoção física somente quando a animação de MovePrep já avançou (ou entrou no loop)
        private async void BeginMovementGateAsync()
        {
            try {
                if (_bossView != null) {
                    // Espera entrar no loop OU atingir 90% do prep antes de permitir deslocamento
                    await _bossView.WaitUntilMoveLoopOrPrepAsync(0.9f, 3.5f);
                } else {
                    // fallback: pequeno atraso
                    await Task.Delay(450);
                }
            } catch { }
            // Planeja direção e aplica rotação inicial uma única vez
            Vector3 planned = ComputePlannedDirection();
            if (planned.sqrMagnitude > 0.0001f) {
                _plannedMoveDirection = planned.normalized;
                _hasPlannedMoveDirection = true;
                try {
                    Quaternion targetRot = Quaternion.LookRotation(_plannedMoveDirection, Vector3.up);
                    _bossRigidbody.MoveRotation(targetRot);
                } catch { }
                _movementAllowed = true;
            } else {
                // Sem direção válida: encerra movimento deste turno
                _plannedMoveDirection = Vector3.zero;
                _hasPlannedMoveDirection = false;
                _movementAllowed = false;
                _turnMoveDistanceBudget = 0f;
                if (_movingAnimActive && _bossView != null) {
                    _bossView.SetMoving(false); _bossView.PlayMoveFinish();
                    _movingAnimActive = false;
                }
            }
            _movementAllowed = true;
            _movementGatePending = false;
        }

        private Vector3 ComputePlannedDirection()
        {
            try {
                switch (_moveMode) {
                    case BossMoveMode.Forward:
                        {
                            Vector3 fwd = _bossTransform.forward; fwd.y = 0f;
                            return fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.zero;
                        }
                    case BossMoveMode.TowardPlayer:
                        {
                            GameObject target = FindPlayerTarget();
                            if (target == null) return Vector3.zero;
                            Vector3 dir = target.transform.position - _bossTransform.position; dir.y = 0f;
                            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
                        }
                    case BossMoveMode.Random:
                        {
                            Vector3 d = _randomDirection; d.y = 0f;
                            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.zero;
                        }
                    case BossMoveMode.TowardArenaCenter:
                        {
                            Vector3 center = _arenaReference != null ? _arenaReference.transform.position : Vector3.zero;
                            Vector3 dir = center - _bossTransform.position; dir.y = 0f;
                            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.zero;
                        }
                    default: return Vector3.zero;
                }
            } catch { return Vector3.zero; }
        }

        public void PlayPhaseTransitionAnimation() {
            _bossView?.PlayPhaseTransition();
        }

        private async System.Threading.Tasks.Task PrepareNextActionAsync() {
            // Se há minigame ativo, não agendar novo ataque
            if (Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.IsActive) return;
            // Se a última rodada de um minigame acabou de sinalizar resolução no turno da Laki,
            // pulamos UMA preparação neste turno
            if (Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.ConsumeSkipOnBossTurn()) return;
            await QueuePreparedAttackFromBehaviorAsync();
        }

		private async System.Threading.Tasks.Task QueuePreparedAttackFromBehaviorAsync() {
            var behavior = GetCurrentBehavior();
            if (behavior == null) return;
            BossBehaviorSO.BossTurnConfig[] pattern = behavior.TurnPattern;
            BossAttack[] pool = behavior.AvailableAttacks;
            if (pattern == null || pattern.Length == 0 || pool == null || pool.Length == 0) return;
            int indexInPattern = _executedTurnsCount % pattern.Length;
            BossBehaviorSO.BossTurnConfig entry = pattern[indexInPattern];

			// Coleta múltiplos índices (modelo atual). Se vier vazio, não agenda ataques neste turno.
			int[] indices = entry.AttackIndices;
			if (indices == null || indices.Length == 0) {
				Debug.LogWarning("[BossBehavior] TurnPattern entry has empty AttackIndices; no attacks queued this turn.");
				return;
			}

			// Primeiro criamos todos, depois escolhemos um vencedor de movimento por prioridade e configuramos telegraph/efeitos
			var created = new System.Collections.Generic.List<(BossAttack atk, int idx, bool hasMove, int pri)>(indices.Length);
			for (int n = 0; n < indices.Length; n++) {
				int attackIndex = Mathf.Clamp(indices[n], 0, pool.Length - 1);
				BossAttack inst = _bossAbilityController?.CreateAttackAtIndex(attackIndex, _bossView.transform);
				if (inst == null) { Debug.LogWarning("Boss attack instantiated: null"); continue; }
				bool hasMove = inst.HasDisplacementEffect();
				int pri = hasMove ? inst.GetDisplacementPriority() : int.MinValue;
				created.Add((inst, attackIndex, hasMove, pri));
			}

			// Seleciona vencedor por maior prioridade entre os que movem
			BossAttack winner = null;
			int bestPri = int.MinValue;
			for (int i = 0; i < created.Count; i++) {
				if (created[i].hasMove && created[i].pri > bestPri) {
					bestPri = created[i].pri;
					winner = created[i].atk;
				}
			}

			for (int i = 0; i < created.Count; i++) {
				BossAttack attackInstance = created[i].atk;
				int attackIndex = created[i].idx;

				if (created[i].hasMove) {
					if (winner != null && !object.ReferenceEquals(attackInstance, winner)) {
						// Não-vencedores: remover efeitos de movimento e ocultar indicação de deslocamento
						attackInstance.StripDisplacementForTelegraph();
						attackInstance.ConfigureTelegraphDisplacementEnabled(false);
					} else {
						// Vencedor mantém indicação
						attackInstance.ConfigureTelegraphDisplacementEnabled(true);
					}
				} else {
					attackInstance.ConfigureTelegraphDisplacementEnabled(false);
				}

				if (_arenaReference != null) {
					attackInstance.Setup(_arenaReference, this);
				}
				Debug.Log($"Boss attack instantiated: {attackInstance.name} | index={attackIndex}");
				int delay = attackInstance != null && attackInstance.IsMinigameAttack() ? 0 : 1;
				_pendingCasts.Add(new PendingCast { Attack = attackInstance, TurnsRemaining = delay });
				if (delay == 0) Debug.Log($"[Laki] Minigame queued to execute this turn");
				else Debug.Log($"Boss prepared attack index: {attackIndex} (executes next turn)");
			}

            // Escolhe o PRIMEIRO da lista como primário para animar (independente do winner)
            if (created.Count > 0 && _bossView != null) {
                BossAttack primary = created[0].atk;
                if (primary != null) {
                    int animId = ResolveAnimationIdFor(primary);
                    if (animId >= 0) {
                        _bossView.PlayAttackPrep(animId);
                        _bossView.SetAttackLoop(true);
                        _loopingAttackAnimId = animId;
                        // Exibir telegraph no MEIO do prep
                        try {
                            await _bossView.WaitUntilStateTagNormalizedAsync("AttackPrep", 0.5f, 2.5f);
                            primary.TrySetTelegraphVisible(true);
                        } catch { }
                    }
                }
            }
        }

        private int ResolveAnimationIdFor(BossAttack attack)
        {
            if (attack == null) return -1;
            string kind = attack.GetAttackTypeName();
            // Map: Protean=0, Circle=1, FeatherLines=2, WingSlashLeft=3, WingSlashRight=4
            if (string.Equals(kind, "ProteanCones", StringComparison.OrdinalIgnoreCase)) return 0;
            if (string.Equals(kind, "Circle", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(kind, "FeatherLines", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(kind, "WingSlash", StringComparison.OrdinalIgnoreCase)) {
                // Decide left/right relative to boss forward and player position
                Vector3 player = Vector3.zero;
                if (_arenaReference != null) {
                    player = _arenaReference.RelativeArenaPositionToRealPosition(_arenaReference.GetPlayerArenaPosition());
                } else {
                    var nara = Object.FindFirstObjectByType<Nara.NaraView>(FindObjectsInactive.Exclude);
                    if (nara != null) player = nara.transform.position;
                }
                if (_bossTransform != null) {
                    Vector3 toPlayer = player - _bossTransform.position; toPlayer.y = 0f;
                    Vector3 fwd = _bossTransform.forward; fwd.y = 0f;
                    if (toPlayer.sqrMagnitude < 1e-6f || fwd.sqrMagnitude < 1e-6f) return 3; // default to Left
                    toPlayer.Normalize(); fwd.Normalize();
                    float crossY = Vector3.Cross(fwd, toPlayer).y;
                    return (crossY >= 0f) ? 3 : 4;
                }
                return 3;
            }
            // Fallbacks for other attacks: prefer Circle (1) look
            return 1;
        }

        private void ConfigureTurnMovement() {
            if (Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameRuntimeService.IsActive) {
                _turnMoveDistanceBudget = 0f;
                Debug.Log("[Laki] Minigame active - boss movement disabled this turn");
                return;
            }
            var behavior = GetCurrentBehavior();
            if (behavior == null) return;
            BossBehaviorSO.BossTurnConfig[] pattern = behavior.TurnPattern;
            float baseStep = behavior.StepDistance > 0f ? behavior.StepDistance : (behavior.FallbackStepDistance > 0f ? behavior.FallbackStepDistance : 1f);
            if (pattern == null || pattern.Length == 0) {
                SetMoveModeTowardPlayer(_bossConfiguration.MoveSpeed, _bossConfiguration.RotationSpeed);
                _turnMoveDistanceBudget = baseStep;
                Debug.Log($"Boss turn move: Mode=TowardPlayer | distance={_turnMoveDistanceBudget:0.###}");
                if (_turnMoveDistanceBudget > 0f) StartMovementPrepIfNeeded();
                return;
            }
            int indexInPattern = _executedTurnsCount % pattern.Length;
            BossBehaviorSO.BossTurnConfig entry = pattern[indexInPattern];
			float multiplier = Mathf.Max(0f, entry.DistanceMultiplier);
            _turnMoveDistanceBudget = baseStep * multiplier;
			switch (entry.Mode) {
                case BossBehaviorSO.TurnMoveMode.Forward:
                    SetMoveModeForward(_bossConfiguration.MoveSpeed, _bossConfiguration.RotationSpeed);
                    Debug.Log($"Boss turn move: Mode=Forward | distance={_turnMoveDistanceBudget:0.###}");
                    break;
                case BossBehaviorSO.TurnMoveMode.TowardPlayer:
                    SetMoveModeTowardPlayer(_bossConfiguration.MoveSpeed, _bossConfiguration.RotationSpeed);
                    Debug.Log($"Boss turn move: Mode=TowardPlayer | distance={_turnMoveDistanceBudget:0.###}");
                    break;
                case BossBehaviorSO.TurnMoveMode.Random:
                    SetMoveModeRandom(_bossConfiguration.MoveSpeed, _bossConfiguration.RotationSpeed, behavior.RandomChangeDirectionSeconds);
                    Debug.Log($"Boss turn move: Mode=Random | distance={_turnMoveDistanceBudget:0.###}");
                    break;
				case BossBehaviorSO.TurnMoveMode.TowardArenaCenter:
					SetMoveModeTowardArenaCenter(_bossConfiguration.MoveSpeed, _bossConfiguration.RotationSpeed);
					Debug.Log($"Boss turn move: Mode=TowardArenaCenter | distance={_turnMoveDistanceBudget:0.###}");
					break;
            }
            if (_turnMoveDistanceBudget > 0f) StartMovementPrepIfNeeded();
        }

        public void SetMoveModeForward(float moveSpeed, float rotationSpeed) {
            _moveSpeed = moveSpeed;
            _rotationSpeed = rotationSpeed;
            _moveMode = BossMoveMode.Forward;
        }

        public void SetMoveModeTowardPlayer(float moveSpeed, float rotationSpeed) {
            _moveSpeed = moveSpeed;
            _rotationSpeed = rotationSpeed;
            _moveMode = BossMoveMode.TowardPlayer;
        }

		public void SetMoveModeTowardArenaCenter(float moveSpeed, float rotationSpeed) {
			_moveSpeed = moveSpeed;
			_rotationSpeed = rotationSpeed;
			_moveMode = BossMoveMode.TowardArenaCenter;
		}

        public void SetMoveModeRandom(float moveSpeed, float rotationSpeed, float changeDirectionSeconds = 1.5f) {
            _moveSpeed = moveSpeed;
            _rotationSpeed = rotationSpeed;
            _randomDirDuration = Mathf.Max(0.1f, changeDirectionSeconds);
            _randomDirTimeRemaining = 0f;
            _moveMode = BossMoveMode.Random;
        }



        private void StartMovementPrepIfNeeded()
        {
            if (_movingAnimActive) return;
            if (_bossView != null) { _bossView.PlayMovePrep(); _bossView.SetMoving(true); }
            _movingAnimActive = true;
            _movementAllowed = false;
            if (!_movementGatePending) { _movementGatePending = true; BeginMovementGateAsync(); }
        }
        private AbilityData SelectAbilityByHealth(out int selectedIndex) { selectedIndex = 0; return null; }
        private int GetAbilityDefaultDelay(AbilityData ability) { return 0; }

        private void ResolvePendingCasts() {
            if (_pendingCasts == null || _pendingCasts.Count == 0) return;
            for (int i = 0; i < _pendingCasts.Count; i++) {
                PendingCast pc = _pendingCasts[i];
                pc.TurnsRemaining -= 1;
                _pendingCasts[i] = pc;
            }
            // Do not execute here; execution happens in ExecutePreparedActionAsync
        }

        private GameObject FindPlayerTarget() {
            // Simplificação: procurar por NaraView na cena
            Nara.NaraView nara = Object.FindFirstObjectByType<Nara.NaraView>();
            return nara != null ? nara.gameObject : null;
        }





        public void TakeDamage(int amount) {
            _bossData.TakeDamage(amount);
            Debug.Log("Tomou dano!!!!!!!!!!!!!!!!");
            int maxHp = _bossConfiguration != null ? _bossConfiguration.MaxHealth : Mathf.Max(1, _bossData.ActualHealth);
            Debug.Log($"[Boss] Damage: -{amount} -> HP={_bossData.ActualHealth}/{maxHp}");
            // Update UI with percentage and absolute
            int pct = maxHp > 0 ? Mathf.RoundToInt((float)_bossData.ActualHealth / maxHp * 100f) : 0;
            _gamePlayUiController.OnActualBossHealthChange(pct);
            _gamePlayUiController.OnActualBossLifeChange(_bossData.ActualHealth);
            _gamePlayUiController.OnPreviewBossHealthChange(pct);
            // Handle death immediately
            if (_bossData.ActualHealth <= 0) {
                Debug.Log("[Boss] Died");
                // Ensure movement/animations are stopped and gates cleared to avoid being stuck in MoveLoop/idle
                _turnMoveDistanceBudget = 0f;
                _movementAllowed = false;
                _movementGatePending = false;
                if (_movingAnimActive) {
                    try { _bossView?.SetMoving(false); } catch { }
                    _movingAnimActive = false;
                }
                try { _updateSubscriptionService.UnregisterFixedUpdatable(this); } catch {}
                if (_bossView != null) {
                    // Optional: play death animation here if available
                }
                return;
            }
            // Evaluate phase transition immediately after damage
            _ = EvaluateAndMaybeSwitchPhaseAsync();
        }

        public void Heal(int amount) {
            _bossData?.Heal(amount);
            int maxHp = _bossConfiguration != null ? _bossConfiguration.MaxHealth : Mathf.Max(1, _bossData.ActualHealth);
            Debug.Log($"[Boss] Heal: +{amount} -> HP={_bossData.ActualHealth}/{maxHp}");
            int pct = maxHp > 0 ? Mathf.RoundToInt((float)_bossData.ActualHealth / maxHp * 100f) : 0;
            _gamePlayUiController.OnActualBossHealthChange(pct);
            _gamePlayUiController.OnActualBossLifeChange(_bossData.ActualHealth);
            _gamePlayUiController.OnPreviewBossHealthChange(pct);
        }

        public void AddShield(int amount) {
            _bossData?.AddShield(amount);
        }

        public bool IsAlive() {
            return _bossData != null && _bossData.IsAlive();
        }

        public void TakeDamagePerTurn(int damageAmount, int duration) {
            throw new NotImplementedException();
        }

        public void HealPerTurn(int healAmount, int duration) {
            throw new NotImplementedException();
        }

        public Transform GetReferenceTransform() {
            return _bossView.transform;
        }

        public void PreviewHeal(int healAmound) {

        }

        public void PreviewDamage(int damageAmound) {

        }

        public void ResetPreview() {

        }

        public void Dispose() {
            TryUnregisterFromUpdate();
        }

        private void TryUnregisterFromUpdate() {
            if (_registeredFixed) {
                try { _updateSubscriptionService.UnregisterFixedUpdatable(this); } catch { }
                _registeredFixed = false;
            }
        }

        private BossBehaviorSO GetCurrentBehavior() {
            return _activeBehavior;
        }

        private async System.Threading.Tasks.Task<bool> EvaluateAndMaybeSwitchPhaseAsync() {
            if (_bossPhases == null || _bossConfiguration == null) return false;
            int newIndex = _bossPhases.GetPhaseIndexByHealth(_bossData.ActualHealth, _bossConfiguration.MaxHealth);
            if (newIndex == _currentPhaseIndex || newIndex < 0) {
                Debug.Log($"[Boss] Phase unchanged (current={_currentPhaseIndex}) at HP={_bossData.ActualHealth}/{_bossConfiguration.MaxHealth}");
                return false;
            }
            var phases = _bossPhases.Phases;
            string prevName = (_currentPhaseIndex >= 0 && phases != null && _currentPhaseIndex < phases.Length) ? phases[_currentPhaseIndex].Name : "(none)";
            BossBehaviorSO newBehavior = GetBehaviorForPhaseIndex(newIndex);
            string newName = (phases != null && newIndex < phases.Length) ? phases[newIndex].Name : "(unknown)";
            Debug.Log($"[Boss] Phase change: {_currentPhaseIndex}:{prevName} -> {newIndex}:{newName} at HP={_bossData.ActualHealth}/{_bossConfiguration.MaxHealth}");
            if (newBehavior != null && newBehavior != _activeBehavior) {
                _activeBehavior = newBehavior;
                _bossAbilityController.SetBehavior(_activeBehavior);
                // Próxima preparação deve começar no primeiro passo do novo comportamento
                _executedTurnsCount = 0;
                // NÃO limpar _pendingCasts: ataques já preparados devem ser completados antes da nova fase iniciar seu padrão
                Debug.Log("[Boss] Pending prepared attacks preserved to complete before new behavior pattern starts.");
            }
            else if (newBehavior == null) {
                Debug.LogWarning($"[Boss] Phase '{newName}' has no Behavior assigned. Keeping current behavior.");
            }
            PlayPhaseTransitionAnimation();
            _currentPhaseIndex = newIndex;
            // wait for the phase transition animation before continuing turn
            if (_bossView != null) {
                float wait = Mathf.Max(0f, _bossView.GetPhaseTransitionDuration());
                if (wait > 0f) {
                    float elapsed = 0f;
                    while (elapsed < wait) {
                        elapsed += Time.deltaTime;
                        await System.Threading.Tasks.Task.Yield();
                    }
                }
            }
            return true;
        }

        private BossBehaviorSO GetBehaviorForPhaseIndex(int index) {
            if (_bossPhases == null) return null;
            var phases = _bossPhases.Phases;
            if (phases == null || index < 0 || index >= phases.Length) return null;
            return phases[index].Behavior;
        }

        public Transform GetTransformCastPoint() {
            return _bossView.GetTransformCastPoint();
        }

        public GameObject GetReferenceTargetPrefab() {
            return _bossView.GetReferenceTargetPrefab();
        }
    }
}
