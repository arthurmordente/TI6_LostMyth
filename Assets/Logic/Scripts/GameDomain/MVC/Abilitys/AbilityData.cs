using Logic.Scripts.Services.UpdateService;
using UnityEngine;
using System;
using Logic.Scripts.Services.CommandFactory;

namespace Logic.Scripts.GameDomain.MVC.Abilitys {
    [CreateAssetMenu(fileName = "AbilityData", menuName = "Scriptable Objects/Ability Data")]
    public class AbilityData : ScriptableObject {
        public string Name;
        public string Description;
        public Sprite Icon;

        [HideInInspector] public int Damage;
        [HideInInspector] public int Cooldown;
        [HideInInspector] public int Cost;
        [HideInInspector] public int Range;

        public int AnimatorAttackType;

        [SerializeField] private int _baseDamage;
        [SerializeField] private int _baseCost;
        [SerializeField] private int _baseCooldown;
        [SerializeField] private int _baseRange;

        [SerializeReference] public TargetingStrategy TargetingStrategy;

        [PlotTwistDataSelector]
        public ScriptableObject PlotData;

        public void SetUp(IUpdateSubscriptionService updateSubscriptionService, ICommandFactory commandFactory) {
            TargetingStrategy.SetUp(updateSubscriptionService, commandFactory);
        }

        public void Aim(IEffectable caster) {
            TargetingStrategy.Initialize(this, caster);
        }
        public void Cast(IEffectable caster) {
            IEffectable[] targets;
            Vector3 aimPoint = TargetingStrategy.LockAim(out targets);
            IPlotTwistData plotTwist = PlotData as IPlotTwistData;
            if (plotTwist != null) {
                foreach (var effect in plotTwist.Effects) {
                    effect.SetUp(aimPoint);
                    if (targets != null && targets.Length > 0) {
                        foreach (var target in targets) {
                            effect.Execute(this, caster, target);
                        }
                    }
                    else {
                        effect.Execute(this, caster);
                    }
                }
            }
        }
        public void Cancel() {
            TargetingStrategy.Cancel();
        }

        #region GettersFinalValues
        public int GetDamage() {
            return _baseDamage + Damage;
        }

        public float GetRange() {
            return _baseRange + (Range / 2.0f);
        }

        public int GetCost() {
            return Math.Max(1, (_baseCost - Cost));
        }

        public int GetCooldown() {
            return Math.Max(1, (_baseCooldown - Cooldown));
        }
        #endregion

        #region SuportMethods
        public void ResetModifiers() {
            Damage = 0;
            Cooldown = 0;
            Cost = 0;
            Range = 0;
        }

        public int GetPointsSpent() {
            int total = 0;
            total += Math.Max(0, Damage);
            total += Math.Max(0, Cooldown);
            total += Math.Max(0, Cost);
            total += Math.Max(0, Range);
            return total;
        }

        public int GetBaseStatValue(AbilityStat stat) {
            switch (stat) {
                case AbilityStat.Damage: return _baseDamage;
                case AbilityStat.Cooldown: return _baseCooldown;
                case AbilityStat.Cost: return _baseCost;
                case AbilityStat.Range: return _baseRange;
                default: return 0;
            }
        }

        public float GetCurrentStatValue(AbilityStat stat) {
            switch (stat) {
                case AbilityStat.Damage: return GetDamage();
                case AbilityStat.Cooldown: return GetCooldown();
                case AbilityStat.Cost: return GetCost();
                case AbilityStat.Range: return (_baseRange + Range);
                default: return 0;
            }
        }

        public int GetModifierStatValue(AbilityStat stat) {
            switch (stat) {
                case AbilityStat.Damage: return Damage;
                case AbilityStat.Cooldown: return Cooldown;
                case AbilityStat.Cost: return Cost;
                case AbilityStat.Range: return Range;
                default: return 0;
            }
        }

        public void SetModifierStatValue(AbilityStat stat, int newValue) {
            switch (stat) {
                case AbilityStat.Damage: Damage = newValue; break;
                case AbilityStat.Cooldown: Cooldown = newValue; break;
                case AbilityStat.Cost: Cost = newValue; break;
                case AbilityStat.Range: Range = newValue; break;
            }
        }
        #endregion
    }
}