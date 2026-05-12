using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Snapshot of projectile rules at cast time. Built from <see cref="SkillDataSO"/> by spawn code — no ScriptableObject reference on the instance.
    /// </summary>
    public struct SkillProjectileSpawnArgs
    {
        public float Speed;
        public float MaxRange;
        public int MaxTargets;
        public SkillDataSO.ProjectileHitMode HitMode;
        public int Damage;
        public float ImpactAreaRadius;
        public IEffectable Caster;
    }

    /// <summary>
    /// Added at runtime to <see cref="SkillDataSO.AttackPrefab"/> by <see cref="SpawnProjectileSkillEffectSO"/>.
    /// Prefab can be visuals + collider only; call <see cref="Initialize"/> once after <see cref="Object.Instantiate"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class SkillSpawnedProjectile : MonoBehaviour
    {
        bool _configured;
        float _speed;
        float _maxRange;
        int _maxTargets;
        SkillDataSO.ProjectileHitMode _hitMode;
        int _damage;
        float _impactAreaRadius;
        IEffectable _caster;
        Vector3 _spawnPos;
        int _hitCount;

        bool Pierce => _hitMode == SkillDataSO.ProjectileHitMode.PierceUpToMaxTargets;

        public void Initialize(in SkillProjectileSpawnArgs args)
        {
            _speed = args.Speed;
            _maxRange = args.MaxRange;
            _maxTargets = Mathf.Max(1, args.MaxTargets);
            _hitMode = args.HitMode;
            _damage = args.Damage;
            _impactAreaRadius = Mathf.Max(0f, args.ImpactAreaRadius);
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
            bool stop = !Pierce || _hitCount >= _maxTargets;
            if (stop)
                Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (!_configured || _impactAreaRadius <= 0.0001f) return;
            Collider[] hits = Physics.OverlapSphere(transform.position, _impactAreaRadius, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < hits.Length; i++)
            {
                IEffectable f = hits[i].GetComponentInParent<IEffectable>();
                if (f == null) continue;
                if (_caster != null && ReferenceEquals(f, _caster)) continue;
                f.TakeDamage(_damage);
                f.PreviewDamage(_damage);
            }
        }
    }
}
