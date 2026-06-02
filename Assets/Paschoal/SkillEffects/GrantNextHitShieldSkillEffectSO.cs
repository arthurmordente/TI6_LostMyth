using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(fileName = "GrantNextHitShieldEffect", menuName = "ScriptableObjects/Skills/Effects/GrantNextHitShield")]
public class GrantNextHitShieldSkillEffectSO : SkillEffectSO
{
    public override void Execute(in SkillExecutionContext context)
    {
        if (context.EffectRecipient is INextHitDamageShield shield)
            shield.GrantNextHitShield();
    }
}
