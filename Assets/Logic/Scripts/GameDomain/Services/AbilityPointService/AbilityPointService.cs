using UnityEngine;
using System.Collections.Generic;
using System;
using Logic.Scripts.GameDomain.MVC.Abilitys;

public class AbilityPointService : IAbilityPointService {

    public int _availablePoints;
    private int _currentBalance;

    public List<AbilityData> allTrackedAbilities;
    private AbilityPointData AbilityPointData;

    public int CurrentBalance => _currentBalance;
    public int AvailablePoints => _availablePoints;

    public List<AbilityData> AllAbilities => allTrackedAbilities;


    public AbilityPointService(List<AbilityData> abilities, AbilityPointData pointData) {
        allTrackedAbilities = abilities;
        AbilityPointData = pointData;
        LoadStats();
    }

    public void RecomputeStats() {
        int totalPointsSpent = 0;

        foreach (AbilityData ability in allTrackedAbilities) {
            if (ability == null) {
                Debug.LogWarning("Ability data nulo");
                continue;
            }
            totalPointsSpent += ability.GetPointsSpent();
        }

        _currentBalance = _availablePoints - totalPointsSpent;
    }

    public bool TryIncreaseStat(AbilityData ability, AbilityStat stat) {
        int cost = 1;
        if (_currentBalance < cost) {
            return false;
        }

        int currentModifier = ability.GetModifierStatValue(stat);
        ability.SetModifierStatValue(stat, currentModifier + 1);

        RecomputeStats();
        return true;
    }

    public bool TryDecreaseStat(AbilityData ability, AbilityStat stat) {
        int currentModifier = ability.GetModifierStatValue(stat);
        if ((currentModifier + ability.GetBaseStatValue(stat)) == ability.GetBaseStatValue(stat)) {
            return false;
        }
        int newModifier = currentModifier - 1;

        if (newModifier >= 0) {
            ability.SetModifierStatValue(stat, newModifier);
            return false;
        }
        RecomputeStats();
        return true;
    }

    public void ResetAllAbilities() {
        foreach (AbilityData ability in allTrackedAbilities) {
            ability.ResetModifiers();
        }
        RecomputeStats();
    }

    #region TempSave
    public void SaveStats() {
        PlayerPrefs.SetInt("Available", _availablePoints);
        foreach (AbilityData ability in allTrackedAbilities) {
            if (ability == null) continue;
            string abilityKey = ability.name;
            foreach (AbilityStat stat in Enum.GetValues(typeof(AbilityStat))) {
                string playerPrefsKey = abilityKey + "_" + stat.ToString();
                int modifierValue = ability.GetModifierStatValue(stat);
                PlayerPrefs.SetInt(playerPrefsKey, modifierValue);
            }
        }
        PlayerPrefs.Save();
        Debug.Log("Habilidades salvas no PlayerPrefs.");
    }

    public void LoadStats() {
        _availablePoints = PlayerPrefs.GetInt("Available", AbilityPointData.StartPoints);
        foreach (AbilityData ability in allTrackedAbilities) {
            if (ability == null) continue;
            string abilityKey = ability.name;
            foreach (AbilityStat stat in Enum.GetValues(typeof(AbilityStat))) {
                string playerPrefsKey = abilityKey + "_" + stat.ToString();
                int loadedValue = PlayerPrefs.GetInt(playerPrefsKey, 0);
                ability.SetModifierStatValue(stat, loadedValue);
            }
        }
        Debug.Log("Habilidades carregadas do PlayerPrefs.");
        RecomputeStats();
    }

    public void DeleteSavedStats() {
        foreach (AbilityData ability in allTrackedAbilities) {
            if (ability == null) continue;
            string abilityKey = ability.name;

            foreach (AbilityStat stat in Enum.GetValues(typeof(AbilityStat))) {
                string playerPrefsKey = abilityKey + "_" + stat.ToString();
                PlayerPrefs.DeleteKey(playerPrefsKey);
            }
        }
        PlayerPrefs.Save();
        Debug.Log("Dados de habilidades salvos foram deletados do PlayerPrefs.");
        ResetAllAbilities();
    }
    #endregion
}