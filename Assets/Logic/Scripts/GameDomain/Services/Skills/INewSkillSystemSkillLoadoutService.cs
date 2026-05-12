using System;
using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public interface INewSkillSystemSkillLoadoutService
    {
        IReadOnlyList<SkillDataSO> AllSkills { get; }
        int SlotCount { get; }
        bool AreSlotRestrictionsEnabled { get; set; }
        event Action<SkillLoadoutUnitType> OnLoadoutChanged;
        bool TryGetSelectedSkill(SkillLoadoutUnitType unitType, int slotIndex, out SkillDataSO skill);
        SkillDataSO[] BuildRuntimeSlotsArray(SkillLoadoutUnitType unitType);
        bool CanAssignSkillToSlot(SkillLoadoutUnitType unitType, int slotIndex, SkillDataSO skill);
        bool TryGetRequiredSkillType(SkillLoadoutUnitType unitType, int slotIndex, out SkillType requiredSkillType);
        bool SetRequiredSkillType(SkillLoadoutUnitType unitType, int slotIndex, SkillType requiredSkillType);
        bool ClearRequiredSkillType(SkillLoadoutUnitType unitType, int slotIndex);
        bool SetSlotSkill(SkillLoadoutUnitType unitType, int slotIndex, SkillDataSO skill);
        bool SetSlotSkillFromCatalogIndex(SkillLoadoutUnitType unitType, int slotIndex, int catalogIndex);
    }
}
