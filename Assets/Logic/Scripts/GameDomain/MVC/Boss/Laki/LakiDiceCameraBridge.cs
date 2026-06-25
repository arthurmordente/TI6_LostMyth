using System;
using System.Threading.Tasks;
using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.Services.Camera;
using UnityEngine;
using Zenject;
using BossController = Logic.Scripts.GameDomain.MVC.Boss.BossController;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki
{
    /// <summary>
    /// Cinematic camera for Laki dice:
    /// boss focus → boss die → player (before player turn) → player focus → player die → player (+0.3s after land).
    /// </summary>
    public sealed class LakiDiceCameraBridge : IInitializable, IDisposable
    {
        const float PlayerLandRestoreDelaySec = 0.3f;

        readonly ICameraFocusService _focus;
        readonly BossController _bossController;
        readonly INaraController _player;

        CameraFocusHandle _handle;
        bool _sessionActive;
        int _bossDicePendingLand;
        int _playerDicePendingLand;
        int _restoreDelayToken;

        [Inject]
        public LakiDiceCameraBridge(
            ICameraFocusService focus,
            [InjectOptional] BossController bossController = null,
            [InjectOptional] INaraController player = null)
        {
            _focus = focus;
            _bossController = bossController;
            _player = player;
        }

        public void Initialize()
        {
            DiceAttackRuntimeService.OnDiceAttackBegan += OnDiceAttackBegan;
            DiceAttackRuntimeService.OnBossDieSpawned += OnBossDieSpawned;
            DiceAttackRuntimeService.OnPlayerDieSpawned += OnPlayerDieSpawned;
            DiceAttackRuntimeService.OnDieLanded += OnDieLanded;
            DiceAttackRuntimeService.OnDicePlayerTurnOpening += OnDicePlayerTurnOpening;
            DiceAttackRuntimeService.OnPlayerRollPhaseStarted += OnPlayerRollPhaseStarted;
            DiceAttackRuntimeService.OnDiceAttackEnded += OnDiceAttackEnded;
            DiceAttackRuntimeService.OnDeferredScoreboardDismiss += OnDiceAttackEnded;
        }

        public void Dispose()
        {
            DiceAttackRuntimeService.OnDiceAttackBegan -= OnDiceAttackBegan;
            DiceAttackRuntimeService.OnBossDieSpawned -= OnBossDieSpawned;
            DiceAttackRuntimeService.OnPlayerDieSpawned -= OnPlayerDieSpawned;
            DiceAttackRuntimeService.OnDieLanded -= OnDieLanded;
            DiceAttackRuntimeService.OnDicePlayerTurnOpening -= OnDicePlayerTurnOpening;
            DiceAttackRuntimeService.OnPlayerRollPhaseStarted -= OnPlayerRollPhaseStarted;
            DiceAttackRuntimeService.OnDiceAttackEnded -= OnDiceAttackEnded;
            DiceAttackRuntimeService.OnDeferredScoreboardDismiss -= OnDiceAttackEnded;
            CancelPendingRestore();
            ReleaseHandle();
        }

        void OnDiceAttackBegan()
        {
            Transform laki = CombatCameraFocusTargets.ResolveBoss(_bossController);
            if (laki == null) return;

            _sessionActive = true;
            _bossDicePendingLand = Mathf.Max(1, LakiDiceAttackState.BossDiceCount);
            _playerDicePendingLand = 0;
            CancelPendingRestore();
            ReleaseHandle();
            _handle = _focus.Follow(laki, CameraFocusOptions.Cinematic(0.5f));
        }

        void OnBossDieSpawned(DiceActor die) => FollowDie(die);

        void OnPlayerDieSpawned(DiceActor die) => FollowDie(die);

        void OnPlayerRollPhaseStarted()
        {
            if (!_sessionActive) return;
            _playerDicePendingLand = Mathf.Max(1, LakiDiceAttackState.PlayerDiceCount);
        }

        void OnDicePlayerTurnOpening()
        {
            if (!_sessionActive) return;
            CancelPendingRestore();
            FocusOnPlayer(0.45f);
        }

        void OnDieLanded(bool isBoss, int rollSlotIndex)
        {
            if (!_sessionActive) return;

            if (isBoss)
            {
                _bossDicePendingLand = Mathf.Max(0, _bossDicePendingLand - 1);
                if (_bossDicePendingLand > 0) return;
                CancelPendingRestore();
                RestorePlayerFollow();
                return;
            }

            _playerDicePendingLand = Mathf.Max(0, _playerDicePendingLand - 1);
            if (_playerDicePendingLand > 0) return;
            ScheduleRestorePlayerFollow(PlayerLandRestoreDelaySec);
        }

        void OnDiceAttackEnded()
        {
            if (!_sessionActive) return;
            _sessionActive = false;
            CancelPendingRestore();
            ReleaseHandle();
            _focus.RestoreDefaultFollow();
        }

        void FollowDie(DiceActor die)
        {
            if (!_sessionActive || die == null) return;
            CancelPendingRestore();
            ReleaseHandle();
            _handle = _focus.Follow(die.transform, CameraFocusOptions.Cinematic(0.35f));
        }

        void FocusOnPlayer(float blendSeconds)
        {
            Transform player = CombatCameraFocusTargets.ResolvePlayer(_player);
            ReleaseHandle();
            if (player != null)
                _handle = _focus.Follow(player, CameraFocusOptions.Cinematic(blendSeconds));
        }

        void RestorePlayerFollow()
        {
            ReleaseHandle();
        }

        void ScheduleRestorePlayerFollow(float delaySeconds)
        {
            CancelPendingRestore();
            _ = RunRestoreAfterDelayAsync(++_restoreDelayToken, delaySeconds);
        }

        async Task RunRestoreAfterDelayAsync(int token, float delaySeconds)
        {
            await Task.Delay(Mathf.RoundToInt(delaySeconds * 1000f));
            if (token != _restoreDelayToken || !_sessionActive) return;
            RestorePlayerFollow();
        }

        void CancelPendingRestore() => _restoreDelayToken++;

        void ReleaseHandle()
        {
            if (!_handle.IsValid) return;
            _focus.Release(_handle);
            _handle = CameraFocusHandle.Invalid;
        }
    }
}
