using System;
using Logic.Scripts.Turns;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public interface IRandomTurnPassiveService : IRandomTurnPassiveState
    {
        event Action OnTurnEffectRolled;

        void RefreshFromLoadout();
        void ApplyPlayerTurnStart(IActionPointsService actionPoints, NaraTurnMovementController movement);
    }

    public sealed class RandomTurnPassiveService : IRandomTurnPassiveService
    {
        readonly INewSkillSystemSkillLoadoutService _loadoutService;

        PassiveTurnBehaviorSO _behavior;
        SkillDataSO _activePassiveSkill;
        bool _enabled;

        public event Action OnTurnEffectRolled;

        public bool IsEnabled => _enabled;
        public RandomTurnPassiveEffectKind ActiveEffect { get; private set; } = RandomTurnPassiveEffectKind.None;
        public float ActiveEffectValue { get; private set; }
        public string ActiveRollDisplayText { get; private set; } = string.Empty;
        public SkillDataSO ActivePassiveSkill => _activePassiveSkill;
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
            _activePassiveSkill = null;
            ActiveEffect = RandomTurnPassiveEffectKind.None;
            ActiveEffectValue = 0f;
            ActiveRollDisplayText = string.Empty;

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
                _activePassiveSkill = skill;
                return;
            }
        }

        public void ApplyPlayerTurnStart(IActionPointsService actionPoints, NaraTurnMovementController movement)
        {
            ActiveEffect = RandomTurnPassiveEffectKind.None;
            ActiveEffectValue = 0f;
            ActiveRollDisplayText = string.Empty;

            if (!_enabled || _behavior == null) return;
            if (!_behavior.TryRollTurnEffect(out RandomTurnPassiveEffectKind kind, out float value, out string displayText))
                return;

            ActiveEffect = kind;
            ActiveEffectValue = value;
            ActiveRollDisplayText = displayText ?? string.Empty;
            OnTurnEffectRolled?.Invoke();

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
