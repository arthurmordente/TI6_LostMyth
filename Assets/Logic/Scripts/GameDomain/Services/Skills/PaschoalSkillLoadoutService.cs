using System;
using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public class PaschoalSkillLoadoutService : IPaschoalSkillLoadoutService
    {
        private readonly SkillDataSO[] _catalog;
        private readonly SkillDataSO[] _selectedSlots;

        public IReadOnlyList<SkillDataSO> AllSkills => _catalog;
        public int SlotCount => _selectedSlots.Length;
        public event Action OnLoadoutChanged;

        public PaschoalSkillLoadoutService(SkillDataSO[] allSkills, int slotCount)
        {
            _catalog = allSkills ?? Array.Empty<SkillDataSO>();
            int safeSlotCount = Math.Max(1, slotCount);
            _selectedSlots = new SkillDataSO[safeSlotCount];
            InitializeDefaultSelection();
        }

        public bool TryGetSelectedSkill(int slotIndex, out SkillDataSO skill)
        {
            skill = null;
            if (slotIndex < 0 || slotIndex >= _selectedSlots.Length) return false;
            skill = _selectedSlots[slotIndex];
            return skill != null;
        }

        public SkillDataSO[] BuildRuntimeSlotsArray()
        {
            var clone = new SkillDataSO[_selectedSlots.Length];
            Array.Copy(_selectedSlots, clone, _selectedSlots.Length);
            return clone;
        }

        public bool SetSlotSkill(int slotIndex, SkillDataSO skill)
        {
            if (slotIndex < 0 || slotIndex >= _selectedSlots.Length) return false;
            _selectedSlots[slotIndex] = skill;
            OnLoadoutChanged?.Invoke();
            return true;
        }

        public bool SetSlotSkillFromCatalogIndex(int slotIndex, int catalogIndex)
        {
            if (catalogIndex < 0 || catalogIndex >= _catalog.Length) return false;
            return SetSlotSkill(slotIndex, _catalog[catalogIndex]);
        }

        private void InitializeDefaultSelection()
        {
            int count = Math.Min(_selectedSlots.Length, _catalog.Length);
            for (int i = 0; i < count; i++)
                _selectedSlots[i] = _catalog[i];
        }
    }
}
