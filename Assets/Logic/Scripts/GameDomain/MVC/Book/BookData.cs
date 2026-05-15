using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Book
{
    public class BookData
    {
        private readonly NaraConfigurationSO _config;

        public int ActualHealth { get; private set; }
        public int PreviewHealth { get; private set; }

        public BookData(NaraConfigurationSO config)
        {
            _config = config;
            ResetData();
        }

        public void ResetData()
        {
            ActualHealth = _config.MaxHealth;
            PreviewHealth = _config.MaxHealth;
        }

        public void ResetPreview()
        {
            PreviewHealth = ActualHealth;
        }

        public void ApplyPreviewSubtractDamage(int amount)
        {
            if (amount <= 0) return;
            PreviewHealth = Mathf.Max(0, PreviewHealth - amount);
        }

        public void TakeDamage(int amount)
        {
            ActualHealth -= amount;
        }

        public void Heal(int amount)
        {
            ActualHealth += amount;
            if (ActualHealth > _config.MaxHealth)
                ActualHealth = _config.MaxHealth;
        }

        public bool IsAlive() => ActualHealth <= 0;
    }
}
