using System;
using System.Threading.Tasks;
using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.MVC.Boss;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.Services.ActiveUnit;
using Logic.Scripts.Turns;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.Services.Camera
{
    /// <summary>
    /// Baseline combat camera follow by turn phase. Cinematic overrides (dice, tile apply) use leases on top.
    /// Boss resolve/prepare milestones are handled via <see cref="CombatBossTurnCameraRuntime"/>.
    /// </summary>
    public sealed class CombatTurnCameraFocusBridge : IInitializable, IDisposable
    {
        const float MinBossFocusHoldSec = 2f;

        enum TurnFocusSubject
        {
            None = 0,
            Boss = 1,
            ActiveUnit = 2,
            Player = 3
        }

        readonly ICameraFocusService _focus;
        readonly TurnStateService _turnState;
        readonly BossController _bossController;
        readonly IActiveUnitService _activeUnitService;
        readonly INaraController _player;

        float _bossFocusHoldUntil = float.NegativeInfinity;
        int _deferredFocusToken;

        [Inject]
        public CombatTurnCameraFocusBridge(
            ICameraFocusService focus,
            TurnStateService turnState,
            [InjectOptional] BossController bossController = null,
            [InjectOptional] IActiveUnitService activeUnitService = null,
            [InjectOptional] INaraController player = null)
        {
            _focus = focus;
            _turnState = turnState;
            _bossController = bossController;
            _activeUnitService = activeUnitService;
            _player = player;
        }

        public void Initialize()
        {
            _turnState.OnPhaseChanged += OnPhaseChanged;
            CombatBossTurnCameraRuntime.OnBossAttackResolveStarted += OnBossAttackResolveStarted;
            CombatBossTurnCameraRuntime.OnBossAttackResolveCompleted += OnBossAttackResolveCompleted;
            CombatBossTurnCameraRuntime.OnBossPrepareStarted += OnBossPrepareStarted;
        }

        public void Dispose()
        {
            _turnState.OnPhaseChanged -= OnPhaseChanged;
            CombatBossTurnCameraRuntime.OnBossAttackResolveStarted -= OnBossAttackResolveStarted;
            CombatBossTurnCameraRuntime.OnBossAttackResolveCompleted -= OnBossAttackResolveCompleted;
            CombatBossTurnCameraRuntime.OnBossPrepareStarted -= OnBossPrepareStarted;
            CancelPendingFocusOperations();
        }

        void OnPhaseChanged(int turnNumber, TurnPhase phase)
        {
            if (!_turnState.Active) return;
            if (DiceAttackRuntimeService.IsActive) return;

            if (IsBossFocusHoldActive())
            {
                ScheduleDeferredPhaseFocus(phase);
                return;
            }

            ApplyPhaseFocus(phase);
        }

        void OnBossAttackResolveStarted()
        {
            if (!CanApplyBossTurnFocus()) return;
            CancelPendingFocusOperations();
            _bossFocusHoldUntil = float.NegativeInfinity;
            ApplySubject(TurnFocusSubject.Player);
        }

        void OnBossAttackResolveCompleted()
        {
            if (!CanApplyBossTurnFocus()) return;
            ApplyBossFocusWithHold();
        }

        void OnBossPrepareStarted()
        {
            if (!CanApplyBossTurnFocus()) return;
            ApplyBossFocusWithHold();
        }

        void ApplyBossFocusWithHold()
        {
            CancelPendingFocusOperations();
            ApplySubject(TurnFocusSubject.Boss);
            _bossFocusHoldUntil = Time.realtimeSinceStartup + MinBossFocusHoldSec;
        }

        void ScheduleDeferredPhaseFocus(TurnPhase phase)
        {
            float delaySeconds = Mathf.Max(0f, _bossFocusHoldUntil - Time.realtimeSinceStartup);
            _ = RunDeferredPhaseFocusAsync(++_deferredFocusToken, phase, delaySeconds);
        }

        async Task RunDeferredPhaseFocusAsync(int token, TurnPhase phase, float delaySeconds)
        {
            if (delaySeconds > 0f)
                await Task.Delay(Mathf.RoundToInt(delaySeconds * 1000f));
            if (token != _deferredFocusToken || !_turnState.Active || DiceAttackRuntimeService.IsActive) return;

            TurnPhase phaseToApply = _turnState.Phase == phase ? phase : _turnState.Phase;
            ApplyPhaseFocus(phaseToApply);
        }

        void CancelPendingFocusOperations() => _deferredFocusToken++;

        bool IsBossFocusHoldActive() => Time.realtimeSinceStartup < _bossFocusHoldUntil;

        bool CanApplyBossTurnFocus()
        {
            return _turnState.Active && !DiceAttackRuntimeService.IsActive;
        }

        void ApplyPhaseFocus(TurnPhase phase)
        {
            Transform target = ResolveForPhase(phase);
            if (target != null)
                _focus.SetDefaultFollow(target);
        }

        void ApplySubject(TurnFocusSubject subject)
        {
            Transform target = ResolveSubject(subject);
            if (target != null)
                _focus.SetDefaultFollow(target);
        }

        Transform ResolveForPhase(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.PlayerAct:
                    return ResolveSubject(TurnFocusSubject.ActiveUnit);
                case TurnPhase.EnviromentAct:
                    return ResolveSubject(TurnFocusSubject.Player);
                default:
                    return null;
            }
        }

        Transform ResolveSubject(TurnFocusSubject subject)
        {
            switch (subject)
            {
                case TurnFocusSubject.Boss:
                    return CombatCameraFocusTargets.ResolveBoss(_bossController);
                case TurnFocusSubject.ActiveUnit:
                    return CombatCameraFocusTargets.ResolveActiveUnit(_activeUnitService, _player);
                case TurnFocusSubject.Player:
                    return CombatCameraFocusTargets.ResolvePlayer(_player);
                default:
                    return null;
            }
        }
    }
}
