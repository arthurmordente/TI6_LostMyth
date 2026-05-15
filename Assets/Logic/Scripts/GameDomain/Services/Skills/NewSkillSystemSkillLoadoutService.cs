using System;
using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public class NewSkillSystemSkillLoadoutService : INewSkillSystemSkillLoadoutService
    {
        /// <summary>Grava o nome/chave da skill — sobrevive a catálogos com ordem diferente entre cenas.</summary>
        private const string PlayerPrefsPlayerV2Prefix = "NSLoadoutV2_Player_";
        private const string PlayerPrefsBookV2Prefix = "NSLoadoutV2_Book_";
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
            string legacyPrefix = unitType == SkillLoadoutUnitType.Book ? PlayerPrefsBookPrefix : PlayerPrefsPlayerPrefix;
            string v2Prefix = unitType == SkillLoadoutUnitType.Book ? PlayerPrefsBookV2Prefix : PlayerPrefsPlayerV2Prefix;
            for (int i = 0; i < slots.Length; i++)
            {
                string v2Key = v2Prefix + i;
                string legacyKey = legacyPrefix + i;
                SkillDataSO skill = slots[i];
                if (skill == null)
                {
                    UnityEngine.PlayerPrefs.DeleteKey(v2Key);
                    UnityEngine.PlayerPrefs.DeleteKey(legacyKey);
                    continue;
                }

                string persistenceKey = skill.LoadoutPersistenceKey;
                UnityEngine.PlayerPrefs.SetString(v2Key, persistenceKey);

                int catalogIndex = IndexOfInCatalog(skill);
                if (catalogIndex >= 0)
                    UnityEngine.PlayerPrefs.SetInt(legacyKey, catalogIndex);
                else
                {
                    UnityEngine.PlayerPrefs.DeleteKey(legacyKey);
                    UnityEngine.Debug.LogWarning(
                        $"[NewSkillSystemSkillLoadoutService] Skill '{skill.name}' não está no catálogo desta cena — loadout guardado só por chave V2. Confirma que GamePlay e Exploration usam o mesmo asset em cada entrada do catálogo.");
                }
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
            string legacyPrefix = unitType == SkillLoadoutUnitType.Book ? PlayerPrefsBookPrefix : PlayerPrefsPlayerPrefix;
            string legacyPaschoalPrefix = unitType == SkillLoadoutUnitType.Book ? LegacyPlayerPrefsBookPrefix : LegacyPlayerPrefsPlayerPrefix;
            string v2Prefix = unitType == SkillLoadoutUnitType.Book ? PlayerPrefsBookV2Prefix : PlayerPrefsPlayerV2Prefix;
            bool usedLegacy = false;
            bool migratedToV2 = false;
            for (int i = 0; i < target.Length; i++)
            {
                string v2Key = v2Prefix + i;
                if (UnityEngine.PlayerPrefs.HasKey(v2Key))
                {
                    string key = UnityEngine.PlayerPrefs.GetString(v2Key, string.Empty);
                    if (!string.IsNullOrEmpty(key) && TryResolveSkillByPersistenceKey(key, out SkillDataSO resolved))
                    {
                        target[i] = resolved;
                        continue;
                    }
                    if (!string.IsNullOrEmpty(key))
                        UnityEngine.Debug.LogWarning(
                            $"[NewSkillSystemSkillLoadoutService] Chave V2 '{key}' (slot {i}, {unitType}) não bate com nenhuma skill do catálogo desta cena. A usar índice antigo ou predefinição.");
                }

                int idx = -1;
                if (UnityEngine.PlayerPrefs.HasKey(legacyPrefix + i))
                    idx = UnityEngine.PlayerPrefs.GetInt(legacyPrefix + i, -1);
                else if (UnityEngine.PlayerPrefs.HasKey(legacyPaschoalPrefix + i))
                {
                    idx = UnityEngine.PlayerPrefs.GetInt(legacyPaschoalPrefix + i, -1);
                    usedLegacy = true;
                }

                if (idx >= 0 && idx < _catalog.Length)
                {
                    target[i] = _catalog[idx];
                    migratedToV2 = true;
                }
            }
            EnsureSlotsFilledWithDefaults(target);
            if (usedLegacy || migratedToV2)
                SaveToPlayerPrefs(unitType, target);
        }

        /// <summary>Resolve por <see cref="SkillDataSO.LoadoutPersistenceKey"/> ou pelo nome do asset (<c>name</c>).</summary>
        private bool TryResolveSkillByPersistenceKey(string key, out SkillDataSO skill)
        {
            skill = null;
            if (string.IsNullOrEmpty(key)) return false;
            SkillDataSO firstNameMatch = null;
            for (int i = 0; i < _catalog.Length; i++)
            {
                if (_catalog[i] == null) continue;
                if (_catalog[i].LoadoutPersistenceKey == key)
                {
                    skill = _catalog[i];
                    return true;
                }
                if (firstNameMatch == null && _catalog[i].name == key)
                    firstNameMatch = _catalog[i];
            }
            if (firstNameMatch != null)
            {
                skill = firstNameMatch;
                return true;
            }
            return false;
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
