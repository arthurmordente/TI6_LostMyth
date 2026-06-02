using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public struct SkillExecutionContext
    {
        public SkillDataSO Skill;
        public IEffectable Caster;
        public IEffectable Beneficiary;
        public Transform TargetTransform;
        public Vector3 TargetPoint;
        public IReadOnlyList<IEffectable> Targets;

        public IEffectable EffectRecipient => Beneficiary ?? Caster;
    }
}
