using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss
{
    [CreateAssetMenu(fileName = "BossConfiguration", menuName = "Scriptable Objects/BossConfiguration")]
    public class BossConfigurationSO : ScriptableObject
    {
        [Tooltip("Shown in the fight HUD (e.g. Laki, Hokari).")]
        [field: SerializeField] public string BossDisplayName { get; private set; } = "Laki";

        [field: SerializeField] public int MaxHealth { get; private set; }
        [field: SerializeField] public int InitialMovementDistance { get; private set; }
        [field: SerializeField] public float MoveSpeed { get; private set; }
        [field: SerializeField] public float RotationSpeed { get; private set; }

        // Absolute world positions for spawn. When set, they should be used instead of ArenaPosReference.
        // Defaults reproduce current behavior in world space.
        [field: SerializeField] public Vector3 InitialBossPosition { get; private set; } = new Vector3(0f, 0f, 0f);
        [field: SerializeField] public Vector3 InitialPlayerPosition { get; private set; } = new Vector3(0f, 0f, -10f);
    }
}
