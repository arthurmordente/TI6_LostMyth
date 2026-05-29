using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    /// <summary>Fight-long passive logic triggered when the player takes damage.</summary>
    public abstract class PassiveOnDamageTakenBehaviorSO : ScriptableObject
    {
        public abstract float MovementRadiusMultiplierPerStack { get; }
    }
}
