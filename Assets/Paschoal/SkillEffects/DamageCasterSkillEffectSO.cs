using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "DamageCasterEffect", menuName = "ScriptableObjects/Skills/Effects/DamageCaster")]
public class DamageCasterSkillEffectSO : SkillEffectSO
{
    [SerializeField, FormerlySerializedAs("_flatDamageBonus"), Tooltip("Dano aplicado ao caster por esta instância do efeito (não usa Power da skill).")]
    private int _damageAmount;

    /// <summary>Damage this effect deals to the caster (preview + <see cref="Execute"/>).</summary>
    public int EffectDamageAmount => Mathf.Max(0, _damageAmount);

    public override void Execute(in SkillExecutionContext context)
    {
        if (context.Caster == null) return;
        int amount = EffectDamageAmount;
        if (amount <= 0) return;
        context.Caster.TakeDamage(amount);
    }
}
