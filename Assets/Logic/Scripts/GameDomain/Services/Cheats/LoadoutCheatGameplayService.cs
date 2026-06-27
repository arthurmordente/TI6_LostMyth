using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.Turns;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Cheats
{
    public class LoadoutCheatGameplayService
    {
        const string InfiniteManaId = "infinite_mana";
        const string InfiniteHealthId = "infinite_health";

        readonly ILoadoutCheatService _cheatService;

        public LoadoutCheatGameplayService(ILoadoutCheatService cheatService)
        {
            _cheatService = cheatService;
        }

        public void ApplyPlayerTurnStart(IActionPointsService actionPoints, INaraController naraController)
        {
            if (_cheatService == null) return;

            if (IsManaCheatEnabled())
                actionPoints?.Add(GetManaRegenAmount());

            if (IsHealthCheatEnabled())
                naraController?.ApplySharedHealthHeal(GetHealthRegenAmount(), showNaraHealFeedback: true);
        }

        public void ApplyBookTurnStart(IBookController bookController)
        {
            if (_cheatService == null || !IsManaCheatEnabled() || bookController == null) return;
            bookController.GetActionPoints()?.Add(GetManaRegenAmount());
        }

        public void ApplyAfterCast(IPlayableUnit caster)
        {
            if (_cheatService == null || !IsManaCheatEnabled() || caster == null) return;
            caster.GetActionPoints()?.Add(GetManaRegenAmount());
        }

        public void ApplyAfterDamage(INaraController naraController)
        {
            if (_cheatService == null || !IsHealthCheatEnabled()) return;
            naraController?.ApplySharedHealthHeal(GetHealthRegenAmount(), showNaraHealFeedback: true);
        }

        bool IsManaCheatEnabled() => IsCheatEnabledForEffect(LoadoutCheatEffectType.ManaRegen, InfiniteManaId);

        bool IsHealthCheatEnabled() => IsCheatEnabledForEffect(LoadoutCheatEffectType.HealthRegen, InfiniteHealthId);

        bool IsCheatEnabledForEffect(LoadoutCheatEffectType effectType, string legacyId)
        {
            if (_cheatService == null) return false;

            if (_cheatService.IsEnabled(legacyId))
                return true;

            if (_cheatService.AllCheats == null) return false;

            for (int i = 0; i < _cheatService.AllCheats.Count; i++)
            {
                CheatDataSO cheat = _cheatService.AllCheats[i];
                if (cheat == null || cheat.EffectType != effectType) continue;
                if (_cheatService.IsEnabled(cheat))
                    return true;
            }

            return false;
        }

        int GetManaRegenAmount() => GetEffectAmount(InfiniteManaId, LoadoutCheatEffectType.ManaRegen, 10);

        int GetHealthRegenAmount() => GetEffectAmount(InfiniteHealthId, LoadoutCheatEffectType.HealthRegen, 100);

        int GetEffectAmount(string legacyId, LoadoutCheatEffectType expectedType, int fallback)
        {
            if (_cheatService?.AllCheats == null) return fallback;

            for (int i = 0; i < _cheatService.AllCheats.Count; i++)
            {
                CheatDataSO cheat = _cheatService.AllCheats[i];
                if (cheat == null) continue;

                bool idMatch = string.Equals(cheat.CheatId, legacyId, System.StringComparison.OrdinalIgnoreCase);
                if (!idMatch && cheat.EffectType != expectedType) continue;
                if (cheat.EffectType != expectedType) return fallback;
                return Mathf.Max(0, cheat.EffectAmount);
            }

            return fallback;
        }
    }
}
