using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/BolaDeFogo", order = 2)]
public class DanoEmArea : SkillDataSO
{
    Collider[] colliders;
    public override void OnCast(IEffectable caster, Transform target)
    {
        colliders = Physics.OverlapSphere(target.transform.position, AreaOfEffect);
        foreach (Collider col in colliders)
        {
            if (col.TryGetComponent<IEffectable>(out IEffectable f)) {
                f.TakeDamage(Power);
                f.PreviewDamage(Power);
            }
        }
    }
}
