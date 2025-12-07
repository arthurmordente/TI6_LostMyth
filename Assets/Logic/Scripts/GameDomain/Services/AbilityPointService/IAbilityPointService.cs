using Logic.Scripts.GameDomain.MVC.Abilitys;
using System.Collections.Generic;

public interface IAbilityPointService {
    int CurrentBalance { get; }
    int AvailablePoints { get; }
    List<AbilityData> AllAbilities { get; }
    void RecomputeStats();
    bool TryIncreaseStat(AbilityData ability, AbilityStat stat);
    bool TryDecreaseStat(AbilityData ability, AbilityStat stat);
    void ResetAllAbilities();
    void SaveStats();
    void LoadStats();
    void DeleteSavedStats();
}
