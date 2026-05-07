using System.Collections.Generic;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

/// <summary>
/// Skill driven only by <see cref="SkillDataSO"/> fields + <see cref="SkillEffectSO"/> list.
/// Optional <see cref="SkillRuntimeModifierSO"/> hooks allow custom rules without a new skill script class.
/// </summary>
[CreateAssetMenu(fileName = "DeclarativeSkill", menuName = "ScriptableObjects/Skills/Declarative Skill", order = 1)]
public class DeclarativeSkillDataSO : SkillDataSO
{
    [SerializeField] private SkillRuntimeModifierSO[] _runtimeModifiers;

    public override void OnCast(IEffectable caster = null, Transform target = null)
    {
        if (!IsCastable) return;

        List<IEffectable> targets = new List<IEffectable>();
        IReadOnlyList<IEffectable> resolved = ResolveTargets(caster, target);
        if (resolved != null)
        {
            for (int i = 0; i < resolved.Count; i++)
            {
                if (resolved[i] != null)
                    targets.Add(resolved[i]);
            }
        }

        if (_runtimeModifiers != null)
        {
            for (int i = 0; i < _runtimeModifiers.Length; i++)
                _runtimeModifiers[i]?.Apply(this, caster, target, targets);
        }

        SkillEffectsRunner.Execute(this, caster, target, targets);
    }
}
