using Logic.Scripts.GameDomain.MVC.Boss;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelTurnData", menuName = "Scriptable Objects/Levels/LevelTurnData")]
public class LevelTurnData : LevelData {
    [SerializeField] private BossConfigurationSO bossConfiguration;
    public BossConfigurationSO BossConfiguration => bossConfiguration;

    [SerializeField] private BossView bossPrefab;
    public BossView BossPrefab => bossPrefab;

    [SerializeField] private BossPhasesSO bossPhases;
    public BossPhasesSO BossPhases => bossPhases;

    [Tooltip("Nome exibido no HUD desta luta (ex.: Laki, Hokari). Se vazio, usa BossConfiguration → Boss Display Name.")]
    [SerializeField] private string bossHudDisplayName;

    /// <summary>
    /// Texto final para o HUD: prioridade ao nome do nível, senão o do <see cref="BossConfigurationSO"/>.
    /// </summary>
    public string GetEffectiveBossHudDisplayName() {
        if (!string.IsNullOrWhiteSpace(bossHudDisplayName))
            return bossHudDisplayName.Trim();
        if (bossConfiguration != null && !string.IsNullOrWhiteSpace(bossConfiguration.BossDisplayName))
            return bossConfiguration.BossDisplayName.Trim();
        return string.Empty;
    }
}
