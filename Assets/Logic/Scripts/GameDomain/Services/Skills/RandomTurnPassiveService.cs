using Logic.Scripts.Turns;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public interface IRandomTurnPassiveService : IRandomTurnPassiveState
    {
        void RefreshFromLoadout();
        void ApplyPlayerTurnStart(IActionPointsService actionPoints, NaraTurnMovementController movement);
    }

    public sealed class RandomTurnPassiveService : IRandomTurnPassiveService
    {
        readonly INewSkillSystemSkillLoadoutService _loadoutService;

        PassiveTurnBehaviorSO _behavior;
        bool _enabled;

        public bool IsEnabled => _enabled;
        public RandomTurnPassiveEffectKind ActiveEffect { get; private set; } = RandomTurnPassiveEffectKind.None;
        public float ActiveEffectValue { get; private set; }
        public float TurnOutgoingDamageMultiplier =>
            ActiveEffect == RandomTurnPassiveEffectKind.OutgoingDamageMultiplier
                ? Mathf.Max(0f, ActiveEffectValue)
                : 1f;

        public RandomTurnPassiveService(INewSkillSystemSkillLoadoutService loadoutService)
        {
            _loadoutService = loadoutService;
            OutgoingDamageApplier.BindTurnPassiveState(this);
        }

        public void RefreshFromLoadout()
        {
            _enabled = false;
            _behavior = null;
            ActiveEffect = RandomTurnPassiveEffectKind.None;
            ActiveEffectValue = 0f;

            if (_loadoutService == null) return;

            SkillDataSO[] slots = _loadoutService.BuildRuntimeSlotsArray(SkillLoadoutUnitType.Player);
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                SkillDataSO skill = slots[i];
                if (skill == null || skill.SkillType != SkillType.Passive) continue;
                PassiveTurnBehaviorSO behavior = skill.PassiveTurnBehavior;
                if (behavior == null) continue;

                _enabled = true;
                _behavior = behavior;
                return;
            }
        }

        public void ApplyPlayerTurnStart(IActionPointsService actionPoints, NaraTurnMovementController movement)
        {
            ActiveEffect = RandomTurnPassiveEffectKind.None;
            ActiveEffectValue = 0f;

            if (!_enabled || _behavior == null) return;
            if (!_behavior.TryRollTurnEffect(out RandomTurnPassiveEffectKind kind, out float value)) return;

            ActiveEffect = kind;
            ActiveEffectValue = value;

            switch (kind)
            {
                case RandomTurnPassiveEffectKind.ActionPointsBonus:
                    if (actionPoints != null && value > 0f)
                        actionPoints.Add(Mathf.RoundToInt(value));
                    break;
                case RandomTurnPassiveEffectKind.MovementRadiusMultiplier:
                    movement?.ApplyTurnMovementRadiusMultiplier(value);
                    break;
            }
        }
    }
}
