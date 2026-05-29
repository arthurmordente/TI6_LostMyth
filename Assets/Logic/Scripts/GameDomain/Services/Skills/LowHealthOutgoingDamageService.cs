using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public interface ILowHealthOutgoingDamageService : ILowHealthOutgoingDamageState
    {
        void RefreshFromLoadout();
    }

    public sealed class LowHealthOutgoingDamageService : ILowHealthOutgoingDamageService
    {
        readonly INewSkillSystemSkillLoadoutService _loadoutService;
        readonly INaraController _naraController;

        PassiveCombatBehaviorSO _behavior;
        bool _enabled;

        public bool IsEnabled => _enabled;

        public float CurrentMultiplier
        {
            get
            {
                if (!_enabled || _behavior == null) return 1f;
                if (!TryGetPlayerHealthRatio(out float ratio)) return 1f;
                return OutgoingDamageApplier.RoundMultiplier(_behavior.ComputeOutgoingDamageMultiplier(ratio));
            }
        }

        public LowHealthOutgoingDamageService(
            INewSkillSystemSkillLoadoutService loadoutService,
            [InjectOptional] INaraController naraController)
        {
            _loadoutService = loadoutService;
            _naraController = naraController;
            OutgoingDamageApplier.BindLowHealthOutgoingDamageState(this);
        }

        public void RefreshFromLoadout()
        {
            _enabled = false;
            _behavior = null;

            if (_loadoutService == null) return;

            SkillDataSO[] slots = _loadoutService.BuildRuntimeSlotsArray(SkillLoadoutUnitType.Player);
            if (slots == null) return;

            for (int i = 0; i < slots.Length; i++)
            {
                SkillDataSO skill = slots[i];
                if (skill == null || skill.SkillType != SkillType.Passive) continue;
                PassiveCombatBehaviorSO behavior = skill.PassiveCombatBehavior;
                if (behavior == null) continue;

                _enabled = true;
                _behavior = behavior;
                return;
            }
        }

        bool TryGetPlayerHealthRatio(out float ratio)
        {
            ratio = 1f;
            if (_naraController == null) return false;

            int max = _naraController.MaxHealth;
            if (max <= 0) return false;

            ratio = Mathf.Clamp01((float)_naraController.CurrentHealth / max);
            return true;
        }
    }
}
