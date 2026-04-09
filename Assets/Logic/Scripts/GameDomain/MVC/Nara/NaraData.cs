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