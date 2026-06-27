using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Cheats
{
    public enum LoadoutCheatEffectType
    {
        ManaRegen,
        HealthRegen,
    }

    [CreateAssetMenu(fileName = "CheatData", menuName = "TI6/Loadout Cheat")]
    public class CheatDataSO : ScriptableObject
    {
        public string CheatId;
        public string DisplayName;
        public Sprite Icon;

        [TextArea(3, 8)]
        public string Description;

        [TextArea(3, 8)]
        public string Lore;

        public LoadoutCheatEffectType EffectType;
        public int EffectAmount = 10;

        [SerializeField] private SkillDescriptionHighlightEntry[] _descriptionHighlights = System.Array.Empty<SkillDescriptionHighlightEntry>();

        public SkillDescriptionHighlightEntry[] DescriptionHighlights => _descriptionHighlights;
    }
}
