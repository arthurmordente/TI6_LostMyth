using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public abstract class SkillEffectSO : ScriptableObject
    {
        public abstract void Execute(in SkillExecutionContext context);
    }
}
