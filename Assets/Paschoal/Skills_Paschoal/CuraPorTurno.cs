using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/CuraPorTurno")]
public class CuraPorTurno : SkillDataSO
{
    public EffectSO effectSO;
    public override void OnCast(IEffectable caster, Transform target)
    {
        //caster.AddEffect(effectSO);
    }
}
