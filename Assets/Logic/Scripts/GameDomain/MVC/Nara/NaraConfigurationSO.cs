using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara {
    [CreateAssetMenu(fileName = "NaraConfiguration", menuName = "Scriptable Objects/NaraConfiguration")]
    public class NaraConfigurationSO : ScriptableObject {
        [field: SerializeField] public int MaxHealth { get; private set; }
        [field: SerializeField] public int InitialMovementDistance { get; private set; }
        [field: SerializeField] public int MaxActionPoints { get; private set; }
        [field: SerializeField] public int ActionPointsTurnGain { get; private set; }
        [field: SerializeField] public int Defense { get; private set; }
        [field: SerializeField] public float MoveSpeed { get; private set; }
        [Tooltip("Speed at or above this value uses Jog animation when moving (solo Erza rig).")]
        [field: SerializeField] public float JogSpeedThreshold { get; private set; } = 5.5f;
        [field: SerializeField] public float RotationSpeed { get; private set; }
        [field: SerializeField] public float MaxMovementRadius { get; private set; }
        [field: SerializeField] public NaraAreaLineHandlerView NaraAreaLineHandlerView { get; private set; }
    }
}
