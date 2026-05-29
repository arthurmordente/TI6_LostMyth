using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>Central place to apply combat passives, buffs and lifesteal before dealing skill damage.</summary>
    public static class OutgoingDamageApplier
    {
        static IRandomTurnPassiveState _turnPassiveState;
        static ILowHealthOutgoingDamageState _lowHealthOutgoingDamageState;

        public static void BindTurnPassiveState(IRandomTurnPassiveState state) => _turnPassiveState = state;

        public static void BindLowHealthOutgoingDamageState(ILowHealthOutgoingDamageState state) =>
            _lowHealthOutgoingDamageState = state;

        public static float RoundMultiplier(float multiplier) =>
            Mathf.Round(multiplier * 100f) / 100f;

        public static int Resolve(IEffectable caster, int baseDamage)
        {
            if (baseDamage <= 0 || caster == null) return baseDamage;

            float multiplier = BuildMultiplier(caster);
            if (Mathf.Approximately(multiplier, 1f)) return baseDamage;
            return Mathf.Max(0, Mathf.RoundToInt(baseDamage * multiplier));
        }

        public static int Apply(IEffectable caster, IEffectable target, int baseDamage)
        {
            int resolved = Resolve(caster, baseDamage);
            if (resolved <= 0 || target == null) return resolved;

            target.TakeDamage(resolved);
            target.PreviewDamage(resolved);
            TryHealCasterFromLifesteal(caster, resolved);
            return resolved;
        }

        static float BuildMultiplier(IEffectable caster)
        {
            float multiplier = 1f;

            if (_lowHealthOutgoingDamageState != null && _lowHealthOutgoingDamageState.IsEnabled)
                multiplier *= Mathf.Max(0f, _lowHealthOutgoingDamageState.CurrentMultiplier);

            if (_turnPassiveState != null
                && _turnPassiveState.IsEnabled
                && _turnPassiveState.ActiveEffect == RandomTurnPassiveEffectKind.OutgoingDamageMultiplier)
            {
                multiplier *= Mathf.Max(0f, _turnPassiveState.ActiveEffectValue);
            }

            if (caster is IOutgoingDamageModifier outgoingModifier)
                outgoingModifier.TryConsumeNextOutgoingDamageMultiplier(ref multiplier);

            return multiplier;
        }

        static void TryHealCasterFromLifesteal(IEffectable caster, int damageDealt)
        {
            if (damageDealt <= 0 || caster == null) return;
            if (caster is not IOutgoingDamageLifesteal lifesteal) return;

            float percent = lifesteal.OutgoingLifestealPercent;
            if (percent <= 0f) return;

            int healAmount = Mathf.Max(0, Mathf.RoundToInt(damageDealt * percent));
            if (healAmount <= 0) return;
            caster.Heal(healAmount);
        }
    }
}
