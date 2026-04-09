using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/PilarDeFogoUpgrade", order = 2)]
public class PilarDeFogo2 : SkillDataSO
{
    Collider[] colliders;
    public override void OnCast(IEffectable caster, Transform target)
    {
        colliders = Physics.OverlapSphere(target.transform.position, AreaOfEffect);
        foreach (Collider col in colliders)
        {
            if (col.TryGetComponent<IEffectable>(out IEffectable f))
            {
                if ((target.position - col.transform.position).magnitude > AreaOfEffect / 2)
                {
                    f.TakeDamage(Power);
                    f.PreviewDamage(Power);
                }
                else
                {
                    f.TakeDamage(Power * 2);
                    f.PreviewDamage(Power * 2);
                }
            }
        }
    }
}
