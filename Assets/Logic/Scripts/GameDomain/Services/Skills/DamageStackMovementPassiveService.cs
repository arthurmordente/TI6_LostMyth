using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public interface IDamageStackMovementPassiveService : IDamageStackMovementPassiveState
    {
        void RefreshFromLoadout();
        void OnPlayerDamageTaken();
        void ApplyPlayerTurnStart(NaraTurnMovementController movement);
    }

    public sealed class DamageStackMovementPassiveService : IDamageStackMovementPassiveService
    {
        readonly INewSkillSystemSkillLoadoutService _loadoutService;

        PassiveOnDamageTakenBehaviorSO _behavior;
        bool _enabled;
        int _pendingStacks;
        int _lastConsumedStacks;
        float _currentTurnMovementMultiplierFromStacks = 1f;

        public bool IsEnabled => _enabled;
        public int PendingStackCount => _pendingStacks;
        public int LastConsumedStackCount => _lastConsumedStacks;
        public float CurrentTurnMovementMultiplierFromStacks => _currentTurnMovementMultiplierFromStacks;

        public float MovementRadiusMultiplierPerStack =>
            _behavior != null ? _behavior.MovementRadiusMultiplierPerStack : 1f;

        public float PendingTurnMovementMultiplier =>
            ComputeStackMovementMultiplier(_pendingStacks);

        public DamageStackMovementPassiveService(INewSkillSystemSkillLoadoutService loadoutService)
        {
            _loadoutService = loadoutService;
        }

        public void RefreshFromLoadout()
        {
            _enabled = false;
            _behavior = null;
            _pendingStacks = 0;
            _lastConsumedStacks = 0;
            _currentTurnMovementMultiplierFromStacks = 1f;

            if (_loadoutService == null) return;

            SkillDataSO[] slots = _loadoutService.BuildRuntimeSlotsArray(SkillLoadoutUnitType.Player);
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                SkillDataSO skill = slots[i];
                if (skill == null || skill.SkillType != SkillType.Passive) continue;
                PassiveOnDamageTakenBehaviorSO behavior = skill.PassiveOnDamageTakenBehavior;
                if (behavior == null) continue;

                _enabled = true;
                _behavior = behavior;
                return;
            }
        }

        public void OnPlayerDamageTaken()
        {
            if (!_enabled) return;
            _pendingStacks++;
        }

        public void ApplyPlayerTurnStart(NaraTurnMovementController movement)
        {
            _lastConsumedStacks = 0;
            _currentTurnMovementMultiplierFromStacks = 1f;

            if (!_enabled || _behavior == null) return;

            int stacksToConsume = _pendingStacks;
            _pendingStacks = 0;
            _lastConsumedStacks = stacksToConsume;

            if (stacksToConsume <= 0) return;

            float multiplier = ComputeStackMovementMultiplier(stacksToConsume);
            _currentTurnMovementMultiplierFromStacks = multiplier;
            movement?.ApplyTurnMovementRadiusMultiplierCompound(multiplier);
        }

        float ComputeStackMovementMultiplier(int stackCount)
        {
            if (stackCount <= 0 || _behavior == null) return 1f;

            float perStack = Mathf.Max(1f, _behavior.MovementRadiusMultiplierPerStack);
            return OutgoingDamageApplier.RoundMultiplier(Mathf.Pow(perStack, stackCount));
        }
    }
}
