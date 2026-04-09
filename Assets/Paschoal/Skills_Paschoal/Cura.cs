using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/Cura")]
public class Cura : SkillDataSO
{
    public override void OnCast(IEffectable caster, Transform target)
    {
        caster.Heal(Power);
        caster.PreviewHeal(Power);
    }
}
