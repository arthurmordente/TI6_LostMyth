using UnityEngine;

namespace Assets.Logic.Scripts.GameDomain.Effects
{
    public class EffectableRelay : MonoBehaviour, IEffectable
    {
        private IEffectable _target;

        public void Init(IEffectable target)
        {
            _target = target;
        }

        public Transform GetReferenceTransform()
        {
            return _target != null ? _target.GetReferenceTransform() : transform;
        }

        public void PreviewHeal(int healAmound)
        {
            _target?.PreviewHeal(healAmound);
        }

        public void PreviewDamage(int damageAmound)
        {
            _target?.PreviewDamage(damageAmound);
        }

        public void ResetPreview()
        {
            _target?.ResetPreview();
        }

        public void TakeDamage(int damageAmount)
        {
            Debug.Log($"[EffectableRelay] Forward TakeDamage {damageAmount} to {_target}");
            _target?.TakeDamage(damageAmount);
        }

        public void TakeDamagePerTurn(int damageAmount, int duration)
        {
            _target?.TakeDamagePerTurn(damageAmount, duration);
        }

        public void Heal(int healAmount)
        {
            Debug.Log($"[EffectableRelay] Forward Heal {healAmount} to {_target}");
            _target?.Heal(healAmount);
        }

        public void HealPerTurn(int healAmount, int duration)
        {
            _target?.HealPerTurn(healAmount, duration);
        }

        public Transform GetTransformCastPoint() {
            return _target != null ? _target.GetTransformCastPoint() : transform;
        }

        public LineRenderer GetPointLineRenderer() => _target?.GetPointLineRenderer();

        public GameObject GetReferenceTargetPrefab() {
            return _target != null ? _target.GetReferenceTargetPrefab() : gameObject;
        }
    }
}


