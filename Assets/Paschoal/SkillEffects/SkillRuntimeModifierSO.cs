using System.Collections.Generic;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

/// <summary>Optional hook to adjust resolved targets or other context before <see cref="SkillEffectSO"/> run (e.g. double damage in AoE center).</summary>
public abstract class SkillRuntimeModifierSO : ScriptableObject
{
    public abstract void Apply(SkillDataSO skill, IEffectable caster, Transform targetPoint, IList<IEffectable> resolvedTargets);
}
