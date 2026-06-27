using Logic.Scripts.GameDomain.Services.Skills;

namespace Logic.Scripts.GameDomain.Services.Cheats
{
    public enum CheatDescriptionDynamicValue
    {
        EffectAmount,
    }

    public static class CheatDescriptionValueResolver
    {
        public static string Resolve(CheatDataSO cheat, CheatDescriptionDynamicValue value)
        {
            if (cheat == null) return string.Empty;

            switch (value)
            {
                case CheatDescriptionDynamicValue.EffectAmount:
                    return cheat.EffectAmount.ToString();
                default:
                    return string.Empty;
            }
        }
    }

    public static class CheatDescriptionRichTextFormatter
    {
        public static string Format(CheatDataSO cheat)
        {
            if (cheat == null) return string.Empty;

            return RichTextHighlightFormatter.Format(
                cheat.Description,
                cheat.DescriptionHighlights,
                entry =>
                {
                    if (entry.Mode == SkillDescriptionHighlightMode.ManualText)
                        return entry.ManualText ?? string.Empty;
                    return CheatDescriptionValueResolver.Resolve(cheat, CheatDescriptionDynamicValue.EffectAmount);
                });
        }
    }
}
