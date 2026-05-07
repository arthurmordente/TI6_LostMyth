using UnityEngine;
using Logic.Scripts.GameDomain.Services.Skills;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/PilarDeFogoUpgrade", order = 2)]
public class PilarDeFogo2 : SkillDataSO
{
    Collider[] colliders;

    protected override SkillCastMode GetDefaultCastMode()
    {
        return SkillCastMode.Area;
    }

    public override void OnCast(IEffectable caster, Transform target)
    {
        float radius = GetAreaRadius();
        colliders = Physics.OverlapSphere(target.transform.position, radius);
        foreach (Collider col in colliders)
        {
            var f = col.GetComponentInParent<IEffectable>();
            if (f != null)
            {
                if ((target.position - col.transform.position).magnitude > radius / 2f)
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
