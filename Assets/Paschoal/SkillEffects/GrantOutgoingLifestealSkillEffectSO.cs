using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GrantOutgoingLifestealEffect",
    menuName = "ScriptableObjects/Skills/Effects/GrantOutgoingLifesteal")]
public class GrantOutgoingLifestealSkillEffectSO : SkillEffectSO
{
    [Tooltip("Fraction of outgoing damage dealt that heals the caster (0.3 = 30%).")]
    [SerializeField] private float _healPercentOfDamageDealt = 0.3f;

    public override void Execute(in SkillExecutionContext context)
    {
        if (context.EffectRecipient is not IOutgoingDamageLifesteal lifesteal) return;
        lifesteal.SetOutgoingLifestealPercent(Mathf.Max(0f, _healPercentOfDamageDealt));
    }

    private void OnValidate()
    {
        if (_healPercentOfDamageDealt < 0f) _healPercentOfDamageDealt = 0f;
    }
}
