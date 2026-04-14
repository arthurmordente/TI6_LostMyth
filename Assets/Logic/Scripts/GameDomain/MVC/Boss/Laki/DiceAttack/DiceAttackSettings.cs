using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    /// <summary>Serialized on <see cref="Boss.BossAttack"/> when attack type is DiceAttack.</summary>
    [System.Serializable]
    public struct DiceAttackSettings
    {
        [Tooltip("Shown in UI / debug while this dice attack is active.")]
        public string DisplayName;

        [Tooltip("Visual prefab for the player's die (optional; empty = placeholder cube).")]
        public GameObject PlayerDiePrefab;

        [Tooltip("Visual prefab for the boss die (optional).")]
        public GameObject BossDiePrefab;

        [Tooltip("HP used by DiceActor (legacy field on die prefab).")]
        public int DieHp;

        [Tooltip("After any-key confirm, small delay before spawning player dice so the input is not consumed as gameplay.")]
        [Min(0f)]
        public float PlayerRollInputConsumeDelay;

        [Tooltip("Screen-space prefab (own Canvas) shown while waiting for any input to confirm the player's roll.")]
        public GameObject PlayerRollPromptPrefab;

        public static DiceAttackSettings Default()
        {
            return new DiceAttackSettings
            {
                DisplayName = "DiceAttack",
                PlayerDiePrefab = null,
                BossDiePrefab = null,
                DieHp = 99,
                PlayerRollInputConsumeDelay = 0.1f,
                PlayerRollPromptPrefab = null
            };
        }
    }
}
