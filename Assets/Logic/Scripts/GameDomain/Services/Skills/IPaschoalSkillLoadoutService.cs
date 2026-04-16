using System;
using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public interface IPaschoalSkillLoadoutService
    {
        IReadOnlyList<SkillDataSO> AllSkills { get; }
        int SlotCount { get; }
        event Action OnLoadoutChanged;
        bool TryGetSelectedSkill(int slotIndex, out SkillDataSO skill);
        SkillDataSO[] BuildRuntimeSlotsArray();
        bool SetSlotSkill(int slotIndex, SkillDataSO skill);
        bool SetSlotSkillFromCatalogIndex(int slotIndex, int catalogIndex);
    }
}
