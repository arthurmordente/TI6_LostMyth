using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>Fight-long passive logic referenced by <see cref="SkillDataSO"/> passive skills.</summary>
    public abstract class PassiveCombatBehaviorSO : ScriptableObject
    {
        public abstract float ComputeOutgoingDamageMultiplier(float healthRatio);
    }
}
