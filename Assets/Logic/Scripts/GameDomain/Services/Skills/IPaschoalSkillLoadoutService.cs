using System;
using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public interface IPaschoalSkillLoadoutService
    {
        IReadOnlyList<SkillDataSO> AllSkills { get; }
        int SlotCount { get; }
        event Action<SkillLoadoutUnitType> OnLoadoutChanged;
        bool TryGetSelectedSkill(SkillLoadoutUnitType unitType, int slotIndex, out SkillDataSO skill);
        SkillDataSO[] BuildRuntimeSlotsArray(SkillLoadoutUnitType unitType);
        bool SetSlotSkill(SkillLoadoutUnitType unitType, int slotIndex, SkillDataSO skill);
        bool SetSlotSkillFromCatalogIndex(SkillLoadoutUnitType unitType, int slotIndex, int catalogIndex);
    }
}
