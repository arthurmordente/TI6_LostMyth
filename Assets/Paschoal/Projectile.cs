using UnityEngine;

/// <summary>
/// Paschoal projectile: hits via physics messages. Uses <see cref="Component.GetComponentInParent{T}"/> so a collider
/// on a child (e.g. die mesh) still resolves <see cref="IEffectable"/> on the root.
/// <see cref="OnTriggerEnter"/> needs a Rigidbody on at least one body: a kinematic RB on the die + trigger on the
/// projectile (or vice‑versa) is enough. Pure transform motion without any RB on either side won’t raise trigger events reliably.
/// </summary>
public class Projectile : MonoBehaviour
{
    public enum Type { AreaDamage, SingleTarget, Pircer }
    [SerializeField] Type type;
    public float speed;
    Collider[] hits;
    [SerializeField] SkillDataSO skill;
    Vector3 startPos = new Vector3();
    int _hitCount;

    /// <summary>
    /// Used by Paschoal aim preview: line highlight includes every hittable target along the ray
    /// (piercing / area-on-destroy). Single-target projectiles only preview the first target.
    /// </summary>
    public bool PaschoalAimUsesPiercingLineHighlight =>
        type == Type.Pircer || type == Type.AreaDamage;

    private void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (skill == null) return;
        transform.position += transform.forward * speed * Time.deltaTime;
        if ((startPos - transform.position).magnitude > skill.GetProjectileRange())
            Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        var f = collision.gameObject.GetComponentInParent<IEffectable>();
        if (f != null)
            OnHit(f);
    }

    private void OnTriggerEnter(Collider other)
    {
        var f = other.GetComponentInParent<IEffectable>();
        if (f != null)
            OnHit(f);
    }

    /// <summary>
    /// Call after instantiate so range, max hits and hit mode come from the casting <see cref="SkillDataSO"/>, not the prefab defaults.
    /// </summary>
    public void ConfigureForCast(SkillDataSO activeSkill)
    {
        skill = activeSkill;
        _hitCount = 0;
        if (skill == null) return;
        if (skill.GetProjectileHitMode() == SkillDataSO.ProjectileHitMode.PierceUpToMaxTargets)
            type = Type.Pircer;
        else
            type = Type.SingleTarget;
    }

    private void OnDestroy()
    {
        if (skill == null) return;
        if (type == Type.AreaDamage)
        {
            float r = skill.GetAreaRadius();
            hits = Physics.OverlapSphere(transform.position, r > 0.0001f ? r : skill.AreaOfEffect);
            foreach (Collider col in hits)
            {
                var f = col.GetComponentInParent<IEffectable>();
                if (f != null) OnHit(f);
            }
        }
    }

    public void OnHit(IEffectable hit)
    {
        hit.TakeDamage(skill.Power);
        hit.PreviewDamage(skill.Power);
        _hitCount++;
        bool reachedMaxTargets = _hitCount >= skill.GetProjectileMaxTargets();
        if (type == Type.SingleTarget || reachedMaxTargets)
            Destroy(gameObject);
    }
}
