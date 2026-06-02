using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(fileName = "ApplyLegacyEffect", menuName = "ScriptableObjects/Skills/Effects/ApplyLegacyEffect")]
public class ApplyLegacyEffectSkillEffectSO : SkillEffectSO
{
    [SerializeField] private EffectSO _effect;
    [SerializeField] private bool _applyOnCaster;

    public override void Execute(in SkillExecutionContext context)
    {
        if (_effect == null) return;
        if (_applyOnCaster)
        {
            if (context.EffectRecipient != null)
                _effect.DoStuff(context.EffectRecipient);
            return;
        }

        if (context.Targets == null) return;
        for (int i = 0; i < context.Targets.Count; i++)
        {
            IEffectable target = context.Targets[i];
            if (target == null) continue;
            _effect.DoStuff(target);
        }
    }
}
