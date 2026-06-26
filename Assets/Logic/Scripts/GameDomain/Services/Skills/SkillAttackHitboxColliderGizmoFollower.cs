using Logic.Scripts.GameDomain.Services.Skills.Debug;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>
    /// Registers the projectile collider each frame so debug gizmos match runtime collision.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkillAttackHitboxColliderGizmoFollower : MonoBehaviour
    {
        SkillDivinity _divinity;
        Collider _collider;

        public static void Attach(GameObject projectileRoot, SkillDivinity divinity)
        {
            if (projectileRoot == null) return;
            var follower = projectileRoot.GetComponent<SkillAttackHitboxColliderGizmoFollower>();
            if (follower == null)
                follower = projectileRoot.AddComponent<SkillAttackHitboxColliderGizmoFollower>();
            follower._divinity = divinity;
            follower._collider = projectileRoot.GetComponentInChildren<Collider>();
        }

        void Update()
        {
            if (_collider == null)
                _collider = GetComponentInChildren<Collider>();
            SkillAttackHitboxDebugService.Instance?.RecordProjectileCollider(_divinity, _collider);
        }
    }
}
