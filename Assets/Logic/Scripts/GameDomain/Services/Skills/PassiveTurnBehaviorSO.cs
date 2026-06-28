using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>Per-turn passive logic referenced by <see cref="SkillDataSO"/> passive skills.</summary>
    public abstract class PassiveTurnBehaviorSO : ScriptableObject
    {
        public abstract bool TryRollTurnEffect(
            out RandomTurnPassiveEffectKind kind,
            out float value,
            out string displayText);
    }
}
