using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/Shield")]
public class Shield : SkillDataSO
{
    public EffectSO effectSO;
    public override void OnCast(IEffectable caster = null, Transform target = null)
    {
        //caster.AddEffect(effectSO)
    }
}
