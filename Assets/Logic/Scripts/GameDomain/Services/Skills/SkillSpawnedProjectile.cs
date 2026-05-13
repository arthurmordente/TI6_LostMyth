using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Snapshot at cast time — no ScriptableObject on the instance.
    /// </summary>
    public struct SkillProjectileSpawnArgs
    {
        public float Speed;
        public float MaxRange;
        public int MaxTargets;
        public int Damage;
        public IEffectable Caster;
    }

    /// <summary>
    /// Runtime motor added by <see cref="SkillProjectileSpawn"/> to <see cref="SkillDataSO.ProjectilePrefab"/>.
    /// Destroyed after <see cref="SkillProjectileSpawnArgs.MaxTargets"/> distinct hits or max travel range.
    /// </summary>
    [DisallowMultipleComponent]
    public class SkillSpawnedProjectile : MonoBehaviour
    {
        bool _configured;
        float _speed;
        float _maxRange;
        int _maxTargets;
        int _damage;
        IEffectable _caster;
        Vector3 _spawnPos;
        int _hitCount;

        public void Initialize(in SkillProjectileSpawnArgs args)
        {
            _speed = args.Speed;
            _maxRange = args.MaxRange;
            _maxTargets = Mathf.Max(1, args.MaxTargets);
            _damage = args.Damage;
            _caster = args.Caster;
            _spawnPos = transform.position;
            _hitCount = 0;
            _configured = true;
        }

        void Update()
        {
            if (!_configured || _maxRange <= 0f) return;
            transform.position += transform.forward * (_speed * Time.deltaTime);
            if ((transform.position - _spawnPos).sqrMagnitude > _maxRange * _maxRange)
                Destroy(gameObject);
        }

        void OnCollisionEnter(Collision collision) => TryHitCollider(collision != null ? collision.collider : null);

        void OnTriggerEnter(Collider other) => TryHitCollider(other);

        void TryHitCollider(Collider col)
        {
            if (!_configured || col == null) return;
            IEffectable effectable = col.GetComponentInParent<IEffectable>();
            if (effectable == null) return;
            if (_caster != null && ReferenceEquals(effectable, _caster)) return;
            ApplyHit(effectable);
        }

        void ApplyHit(IEffectable hit)
        {
            hit.TakeDamage(_damage);
            hit.PreviewDamage(_damage);
            _hitCount++;
            if (_hitCount >= _maxTargets)
                Destroy(gameObject);
        }
    }
}
