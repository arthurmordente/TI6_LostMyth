using System;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public enum SkillDescriptionDynamicValue
    {
        Power,
        Cost,
        Range,
        ProjectileMaxTargets,
        AreaRadius,
    }

    public enum SkillDescriptionHighlightMode
    {
        DynamicValue,
        ManualText,
    }

    [Serializable]
    public struct SkillDescriptionHighlightEntry
    {
        [Tooltip("Substring in Description to replace (e.g. X, Y, {0}).")]
        public string Placeholder;

        public SkillDescriptionHighlightMode Mode;

        [Tooltip("Used when Mode is DynamicValue.")]
        public SkillDescriptionDynamicValue Value;

        [Tooltip("Used when Mode is ManualText.")]
        public string ManualText;

        public Color Color;
    }

    public static class SkillDescriptionValueResolver
    {
        public static string Resolve(SkillDataSO skill, SkillDescriptionDynamicValue value)
        {
            if (skill == null) return string.Empty;

            switch (value)
            {
                case SkillDescriptionDynamicValue.Power:
                    return skill.Power.ToString();
                case SkillDescriptionDynamicValue.Cost:
                    return skill.Cost.ToString();
                case SkillDescriptionDynamicValue.Range:
                    return skill.SkillType == SkillType.SelfBuff
                        ? "-"
                        : skill.Range.ToString("0.##");
                case SkillDescriptionDynamicValue.ProjectileMaxTargets:
                    return skill.GetProjectileMaxTargets().ToString();
                case SkillDescriptionDynamicValue.AreaRadius:
                    return skill.GetAreaRadius().ToString("0.##");
                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>
    /// Replaces Description placeholders with colored TMP rich-text values from skill data.
    /// </summary>
    public static class SkillDescriptionRichTextFormatter
    {
        public static string Format(SkillDataSO skill)
        {
            if (skill == null) return string.Empty;

            return RichTextHighlightFormatter.Format(
                skill.Description ?? string.Empty,
                skill.DescriptionHighlights,
                entry => entry.Mode == SkillDescriptionHighlightMode.ManualText
                    ? entry.ManualText ?? string.Empty
                    : SkillDescriptionValueResolver.Resolve(skill, entry.Value));
        }
    }
}
