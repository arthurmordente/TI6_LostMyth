using Logic.Scripts.GameDomain.MVC.Environment;
using Logic.Scripts.GameDomain.MVC.Shared;
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
        public bool MoveCasterToHit;
        /// <summary>Horizontal distance from target root to leave the caster when pulling (meters).</summary>
        public float PullStandoffFromTarget;
        public float MinTravelBeforeHitMeters;
        public float HitDisplacementDurationSeconds;
        public GameObject ImpactPrefab;
    }

    /// <summary>
    /// Runtime motor added by <see cref="SkillProjectileSpawn"/> to <see cref="SkillDataSO.ProjectilePrefab"/>.
    /// Damage hits and movement-pull hits both resolve an <see cref="IEffectable"/> the same way (<see cref="GetComponentInParent{T}"/>).
    /// Movement only runs when that resolves to a non-caster victim; otherwise the projectile keeps traveling.
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
        bool _moveCasterToHit;
        float _pullStandoff;
        float _minTravelSq;
        float _hitDisplacementDuration;
        bool _canResolveHits;
        GameObject _impactPrefab;

        public void Initialize(in SkillProjectileSpawnArgs args)
        {
            _speed = args.Speed;
            _maxRange = args.MaxRange;
            _maxTargets = Mathf.Max(1, args.MaxTargets);
            _damage = args.Damage;
            _caster = args.Caster;
            _moveCasterToHit = args.MoveCasterToHit;
            _pullStandoff = Mathf.Max(0.1f, args.PullStandoffFromTarget);
            float minMeters = Mathf.Max(0f, args.MinTravelBeforeHitMeters);
            _minTravelSq = minMeters * minMeters;
            _hitDisplacementDuration = Mathf.Max(0.05f, args.HitDisplacementDurationSeconds);
            _impactPrefab = args.ImpactPrefab;
            _spawnPos = transform.position;
            _hitCount = 0;
            _canResolveHits = true;
            _configured = true;
        }

        void Update()
        {
            if (!_configured || _maxRange <= 0f) return;
            transform.position += transform.forward * (_speed * Time.deltaTime);
            if ((transform.position - _spawnPos).sqrMagnitude > _maxRange * _maxRange)
                Destroy(gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            var col = collision != null ? collision.collider : null;
            if (_moveCasterToHit)
                TryHitEffectableForMovePull(col);
            else
                TryHitCollider(col);
        }

        void OnTriggerEnter(Collider other)
        {
            if (_moveCasterToHit)
                TryHitEffectableForMovePull(other);
            else
                TryHitCollider(other);
        }

        /// <summary>Projectile pull: only reacts when the collider belongs to another <see cref="IEffectable"/>, matching damage resolution.</summary>
        void TryHitEffectableForMovePull(Collider col)
        {
            if (!_configured || !_canResolveHits || col == null) return;
            if (IsCasterCollider(col)) return;
            if ((transform.position - _spawnPos).sqrMagnitude < _minTravelSq) return;

            IEffectable hit = col.GetComponentInParent<IEffectable>();
            if (hit == null) return;
            if (_caster != null && ReferenceEquals(hit, _caster)) return;

            _canResolveHits = false;

            if (_damage > 0)
                OutgoingDamageApplier.Apply(_caster, hit, _damage);

            TrySpawnImpactVfx(hit);

            if (_caster is IPlayableUnit playable)
            {
                Vector3 destination = ComputePullDestinationNearTarget(hit, transform.position, _pullStandoff, _caster);
                playable.BeginSkillGuidedDisplacementToWorldPosition(destination, _hitDisplacementDuration, () =>
                {
                    playable.SyncArenaMovementAfterMovementSkillDisplacement();
                });
            }

            RegisterHitDestroy();
        }

        static Vector3 ComputePullDestinationNearTarget(IEffectable hit, Vector3 projectileWorldPos, float standoffMeters, IEffectable caster)
        {
            Transform targetRoot = hit.GetReferenceTransform();
            if (targetRoot == null)
                return projectileWorldPos;

            Vector3 targetCenter = targetRoot.position;
            Vector3 fromTarget = projectileWorldPos - targetCenter;
            fromTarget.y = 0f;
            if (fromTarget.sqrMagnitude < 1e-6f && caster != null && caster.GetReferenceTransform() != null)
            {
                fromTarget = caster.GetReferenceTransform().position - targetCenter;
                fromTarget.y = 0f;
            }
            if (fromTarget.sqrMagnitude < 1e-6f)
                fromTarget = -targetRoot.forward;
            fromTarget.Normalize();

            Vector3 dest = targetCenter + fromTarget * standoffMeters;
            if (caster != null && caster.GetReferenceTransform() != null)
                dest.y = caster.GetReferenceTransform().position.y;
            CombatArenaBoundaryRuntime.TryClampVoluntaryWorldPosition(ref dest);
            return dest;
        }

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
            if (_damage > 0)
                OutgoingDamageApplier.Apply(_caster, hit, _damage);
            TrySpawnImpactVfx(hit);
            RegisterHitDestroy();
        }

        void TrySpawnImpactVfx(IEffectable hit)
        {
            if (_impactPrefab == null || !SkillImpactVfxRules.ShouldSpawnImpactOn(hit)) return;

            Transform reference = hit.GetReferenceTransform();
            Vector3 position = reference != null ? reference.position : transform.position;
            Quaternion rotation = reference != null ? reference.rotation : Quaternion.identity;
            SkillCastVfxUtility.TrySpawnTransiient(_impactPrefab, position, rotation);
        }

        void RegisterHitDestroy()
        {
            _hitCount++;
            if (_hitCount >= _maxTargets)
                Destroy(gameObject);
        }

        bool IsCasterCollider(Collider col)
        {
            if (_caster == null || col == null) return false;
            Transform root = _caster.GetReferenceTransform();
            if (root == null) return false;
            return col.transform == root || col.transform.IsChildOf(root);
        }
    }
}
