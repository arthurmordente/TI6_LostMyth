using Logic.Scripts.Core.Mvc.WorldCamera;
using UnityEngine;

[CreateAssetMenu(fileName = "ExplorationLevelData", menuName = "Scriptable Objects/Levels/ExplorationLevelData")]
public class LevelExplorationData : LevelData, ISceneCameraEntryProvider {
    [field: SerializeField] public Vector3 InitialPlayerPosition { get; private set; }

    [SerializeField] private SceneCameraEntrySettings sceneCameraEntry;

    public SceneCameraEntrySettings GetEffectiveSceneCameraEntry() =>
        sceneCameraEntry.OverrideDefaults ? sceneCameraEntry : SceneCameraEntrySettings.ExplorationDefaults();
}
