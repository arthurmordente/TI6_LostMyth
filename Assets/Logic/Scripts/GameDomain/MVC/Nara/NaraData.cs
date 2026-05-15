using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara {
    public class NaraData {
        private readonly NaraConfigurationSO _naraSO;

        public int ActualHealth { get; private set; }
        public int PreviewHealth { get; private set; }
        public int RemainningMovementDistance { get; private set; }

        public NaraData(NaraConfigurationSO naraSO) {
            _naraSO = naraSO;
            ResetData();
        }

        public void ResetData() {
            ActualHealth = _naraSO.MaxHealth;
            PreviewHealth = _naraSO.MaxHealth;
            RemainningMovementDistance = _naraSO.InitialMovementDistance;
        }

        public void ResetPreview() {
            PreviewHealth = ActualHealth;
        }

        public void ApplyPreviewHeal(int healAmount) {
            PreviewHealth = Mathf.Min(_naraSO.MaxHealth, ActualHealth + healAmount);
        }

        /// <summary>Reduces <see cref="PreviewHealth"/> from its current value (stacks after heal preview).</summary>
        public void ApplyPreviewSubtractDamage(int amount) {
            if (amount <= 0) return;
            PreviewHealth = Mathf.Max(0, PreviewHealth - amount);
        }

        public void TakeDamage(int damageAmound) {
            //if (_naraSO.Defense > damageAmound) return;
            //else if (_naraSO.Defense < 0) ActualHealth -= damageAmound;
            //else (ActualHealth -= (damageAmound - _naraSO.Defense));
            ActualHealth -= damageAmound;
        }

        public void Heal(int healAmount) {
            ActualHealth += healAmount;
            if (ActualHealth > _naraSO.MaxHealth) {
                ActualHealth = _naraSO.MaxHealth;
            }
        }

        public bool IsAlive() {
            return ActualHealth <= 0f;
        }
    }
}