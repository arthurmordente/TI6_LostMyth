using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(
    fileName = "GrantRandomNextOutgoingDamageMultiplierEffect",
    menuName = "ScriptableObjects/Skills/Effects/GrantRandomNextOutgoingDamageMultiplier")]
public class GrantRandomNextOutgoingDamageMultiplierSkillEffectSO : SkillEffectSO
{
    [SerializeField] private float _minMultiplier = 0.8f;
    [SerializeField] private float _maxMultiplier = 1.4f;

    public override void Execute(in SkillExecutionContext context)
    {
        if (context.Caster is not IOutgoingDamageModifier modifier) return;

        float min = Mathf.Min(_minMultiplier, _maxMultiplier);
        float max = Mathf.Max(_minMultiplier, _maxMultiplier);
        float rolled = Random.Range(min, max);
        modifier.GrantNextOutgoingDamageMultiplier(rolled);
    }

    private void OnValidate()
    {
        if (_minMultiplier < 0f) _minMultiplier = 0f;
        if (_maxMultiplier < 0f) _maxMultiplier = 0f;
    }
}
