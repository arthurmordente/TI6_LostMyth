using System;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Skills
{
    public static class RichTextHighlightFormatter
    {
        public static string Format(
            string text,
            SkillDescriptionHighlightEntry[] entries,
            Func<SkillDescriptionHighlightEntry, string> resolveValue)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
            if (entries == null || entries.Length == 0 || resolveValue == null) return text;

            for (int i = 0; i < entries.Length; i++)
            {
                SkillDescriptionHighlightEntry entry = entries[i];
                if (string.IsNullOrEmpty(entry.Placeholder)) continue;

                string value = resolveValue(entry);
                string hex = ColorUtility.ToHtmlStringRGBA(entry.Color);
                text = text.Replace(entry.Placeholder, $"<color=#{hex}>{value}</color>");
            }

            return text;
        }
    }
}
