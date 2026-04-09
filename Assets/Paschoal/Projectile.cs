using UnityEngine;

public class Projectile : MonoBehaviour
{
    enum Type { AreaDamage, SingleTarget, Pircer}
    [SerializeField] Type type;
    public float speed;
    Collider[] hits;
    [SerializeField]SkillDataSO skill;
    Vector3 startPos = new Vector3();
    private void Start()
    {
        startPos = transform.position;
    }
    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
        if ((startPos - transform.position).magnitude > skill.Range)
        {
            Destroy(gameObject);
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.TryGetComponent<IEffectable>(out IEffectable f))
        {
            OnHit(f);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IEffectable>(out IEffectable f))
        {
            OnHit(f);
        }
    }
    private void OnDestroy()
    {
        if(type == Type.AreaDamage)
        {
            hits = Physics.OverlapSphere(transform.position, skill.AreaOfEffect);
            foreach(Collider col in hits)
            {
                if(col.TryGetComponent<IEffectable>(out IEffectable f)) OnHit(f);
            }
        }
    }
    public void OnHit(IEffectable hit)
    {
        hit.TakeDamage(skill.Power);
        hit.PreviewDamage(skill.Power);
        if(type == Type.SingleTarget)
        {
            Destroy(gameObject);
        }
    }
}
