using UnityEngine;

[CreateAssetMenu(fileName = "ExplorationLevelData", menuName = "Scriptable Objects/Levels/ExplorationLevelData")]
public class LevelExplorationData : LevelData {
    [field: SerializeField] public Vector3 InitialPlayerPosition { get; private set; }
}
