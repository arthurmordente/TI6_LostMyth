using System;
using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public class NewSkillSystemSkillLoadoutService : INewSkillSystemSkillLoadoutService
    {
        private const string PlayerPrefsPlayerPrefix = "NewSkillSystemLoadout_Player_";
        private const string PlayerPrefsBookPrefix = "NewSkillSystemLoadout_Book_";
        private const string LegacyPlayerPrefsPlayerPrefix = "PaschoalLoadout_Player_";
        private const string LegacyPlayerPrefsBookPrefix = "PaschoalLoadout_Book_";

        private readonly SkillDataSO[] _catalog;
        private readonly SkillDataSO[] _selectedPlayerSlots;
        private readonly SkillDataSO[] _selectedBookSlots;
        private readonly SkillType?[] _requiredPlayerSlotTypes;
        private readonly SkillType?[] _requiredBookSlotTypes;

        public IReadOnlyList<SkillDataSO> AllSkills => _catalog;
        public int SlotCount => _selectedPlayerSlots.Length;
        public bool AreSlotRestrictionsEnabled { get; set; }
        public event Action<SkillLoadoutUnitType> OnLoadoutChanged;

        public NewSkillSystemSkillLoadoutService(SkillDataSO[] allSkills, int slotCount)
        {
            _catalog = allSkills ?? Array.Empty<SkillDataSO>();
            int safeSlotCount = Math.Max(1, slotCount);
            _selectedPlayerSlots = new SkillDataSO[safeSlotCount];
            _selectedBookSlots = new SkillDataSO[safeSlotCount];
            _requiredPlayerSlotTypes = new SkillType?[safeSlotCount];
            _requiredBookSlotTypes = new SkillType?[safeSlotCount];
            InitializeDefaultSelection(_selectedPlayerSlots);
            InitializeDefaultSelection(_selectedBookSlots);
            LoadFromPlayerPrefs();
        }

        public bool TryGetSelectedSkill(SkillLoadoutUnitType unitType, int slotIndex, out SkillDataSO skill)
        {
            skill = null;
            SkillDataSO[] selectedSlots = ResolveSlots(unitType);
            if (slotIndex < 0 || slotIndex >= selectedSlots.Length) return false;
            skill = selectedSlots[slotIndex];
            return skill != null;
        }

        public SkillDataSO[] BuildRuntimeSlotsArray(SkillLoadoutUnitType unitType)
        {
            SkillDataSO[] selectedSlots = ResolveSlots(unitType);
            EnsureSlotsFilledWithDefaults(selectedSlots);
            var clone = new SkillDataSO[selectedSlots.Length];
            Array.Copy(selectedSlots, clone, selectedSlots.Length);
            return clone;
        }

        public bool SetSlotSkill(SkillLoadoutUnitType unitType, int slotIndex, SkillDataSO skill)
        {
            SkillDataSO[] selectedSlots = ResolveSlots(unitType);
            if (slotIndex < 0 || slotIndex >= selectedSlots.Length) return false;
            if (!CanAssignSkillToSlot(unitType, slotIndex, skill)) return false;
            selectedSlots[slotIndex] = skill;
            SaveToPlayerPrefs(unitType, selectedSlots);
            OnLoadoutChanged?.Invoke(unitType);
            return true;
        }

        public bool CanAssignSkillToSlot(SkillLoadoutUnitType unitType, int slotIndex, SkillDataSO skill)
        {
            SkillDataSO[] selectedSlots = ResolveSlots(unitType);
            if (slotIndex < 0 || slotIndex >= selectedSlots.Length) return false;
            if (skill == null) return true;
            if (!AreSlotRestrictionsEnabled) return true;
            if (!TryGetRequiredSkillType(unitType, slotIndex, out SkillType requiredSkillType)) return true;
            return skill.SkillType == requiredSkillType;
        }

        public bool TryGetRequiredSkillType(SkillLoadoutUnitType unitType, int slotIndex, out SkillType requiredSkillType)
        {
            requiredSkillType = SkillType.Damage;
            SkillType?[] requiredTypes = ResolveRequiredTypes(unitType);
            if (slotIndex < 0 || slotIndex >= requiredTypes.Length) return false;
            SkillType? value = requiredTypes[slotIndex];
            if (!value.HasValue) return false;
            requiredSkillType = value.Value;
            return true;
        }

        public bool SetRequiredSkillType(SkillLoadoutUnitType unitType, int slotIndex, SkillType requiredSkillType)
        {
            SkillType?[] requiredTypes = ResolveRequiredTypes(unitType);
            if (slotIndex < 0 || slotIndex >= requiredTypes.Length) return false;
            requiredTypes[slotIndex] = requiredSkillType;
            return true;
        }

        public bool ClearRequiredSkillType(SkillLoadoutUnitType unitType, int slotIndex)
        {
            SkillType?[] requiredTypes = ResolveRequiredTypes(unitType);
            if (slotIndex < 0 || slotIndex >= requiredTypes.Length) return false;
            requiredTypes[slotIndex] = null;
            return true;
        }

        public bool SetSlotSkillFromCatalogIndex(SkillLoadoutUnitType unitType, int slotIndex, int catalogIndex)
        {
            if (catalogIndex < 0 || catalogIndex >= _catalog.Length) return false;
            return SetSlotSkill(unitType, slotIndex, _catalog[catalogIndex]);
        }

        private SkillDataSO[] ResolveSlots(SkillLoadoutUnitType unitType)
        {
            return unitType == SkillLoadoutUnitType.Book ? _selectedBookSlots : _selectedPlayerSlots;
        }

        private SkillType?[] ResolveRequiredTypes(SkillLoadoutUnitType unitType)
        {
            return unitType == SkillLoadoutUnitType.Book ? _requiredBookSlotTypes : _requiredPlayerSlotTypes;
        }

        private void InitializeDefaultSelection(SkillDataSO[] target)
        {
            int count = Math.Min(target.Length, _catalog.Length);
            for (int i = 0; i < count; i++)
                target[i] = _catalog[i];
        }

        private void EnsureSlotsFilledWithDefaults(SkillDataSO[] target)
        {
            int count = Math.Min(target.Length, _catalog.Length);
            for (int i = 0; i < count; i++)
            {
                if (target[i] == null)
                    target[i] = _catalog[i];
            }
        }

        private void SaveToPlayerPrefs(SkillLoadoutUnitType unitType, SkillDataSO[] slots)
        {
            string prefix = unitType == SkillLoadoutUnitType.Book ? PlayerPrefsBookPrefix : PlayerPrefsPlayerPrefix;
            for (int i = 0; i < slots.Length; i++)
            {
                int catalogIndex = IndexOfInCatalog(slots[i]);
                UnityEngine.PlayerPrefs.SetInt(prefix + i, catalogIndex);
            }
            UnityEngine.PlayerPrefs.Save();
        }

        private void LoadFromPlayerPrefs()
        {
            LoadUnitFromPlayerPrefs(SkillLoadoutUnitType.Player, _selectedPlayerSlots);
            LoadUnitFromPlayerPrefs(SkillLoadoutUnitType.Book, _selectedBookSlots);
        }

        private void LoadUnitFromPlayerPrefs(SkillLoadoutUnitType unitType, SkillDataSO[] target)
        {
            string prefix = unitType == SkillLoadoutUnitType.Book ? PlayerPrefsBookPrefix : PlayerPrefsPlayerPrefix;
            string legacyPrefix = unitType == SkillLoadoutUnitType.Book ? LegacyPlayerPrefsBookPrefix : LegacyPlayerPrefsPlayerPrefix;
            bool usedLegacy = false;
            for (int i = 0; i < target.Length; i++)
            {
                string newKey = prefix + i;
                string oldKey = legacyPrefix + i;
                int idx = -1;
                if (UnityEngine.PlayerPrefs.HasKey(newKey))
                    idx = UnityEngine.PlayerPrefs.GetInt(newKey, -1);
                else if (UnityEngine.PlayerPrefs.HasKey(oldKey))
                {
                    idx = UnityEngine.PlayerPrefs.GetInt(oldKey, -1);
                    usedLegacy = true;
                }
                if (idx < 0 || idx >= _catalog.Length) continue;
                target[i] = _catalog[idx];
            }
            EnsureSlotsFilledWithDefaults(target);
            if (usedLegacy)
                SaveToPlayerPrefs(unitType, target);
        }

        private int IndexOfInCatalog(SkillDataSO skill)
        {
            if (skill == null) return -1;
            for (int i = 0; i < _catalog.Length; i++)
            {
                if (_catalog[i] == skill) return i;
            }
            return -1;
        }
    }
}
