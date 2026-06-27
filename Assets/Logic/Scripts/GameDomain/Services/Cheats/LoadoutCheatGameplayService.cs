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

            if (_cheatService.IsEnabled(InfiniteManaId))
                actionPoints?.Add(GetEffectAmount(InfiniteManaId, LoadoutCheatEffectType.ManaRegen, 10));

            if (_cheatService.IsEnabled(InfiniteHealthId))
                naraController?.ApplySharedHealthHeal(GetEffectAmount(InfiniteHealthId, LoadoutCheatEffectType.HealthRegen, 100), showNaraHealFeedback: true);
        }

        public void ApplyBookTurnStart(IBookController bookController)
        {
            if (_cheatService == null || !_cheatService.IsEnabled(InfiniteManaId) || bookController == null) return;
            bookController.GetActionPoints()?.Add(GetEffectAmount(InfiniteManaId, LoadoutCheatEffectType.ManaRegen, 10));
        }

        public void ApplyAfterCast(IPlayableUnit caster)
        {
            if (_cheatService == null || !_cheatService.IsEnabled(InfiniteManaId) || caster == null) return;

            int amount = GetEffectAmount(InfiniteManaId, LoadoutCheatEffectType.ManaRegen, 10);
            caster.GetActionPoints()?.Add(amount);
        }

        public void ApplyAfterDamage(INaraController naraController)
        {
            if (_cheatService == null || !_cheatService.IsEnabled(InfiniteHealthId)) return;
            naraController?.ApplySharedHealthHeal(
                GetEffectAmount(InfiniteHealthId, LoadoutCheatEffectType.HealthRegen, 100),
                showNaraHealFeedback: true);
        }

        int GetEffectAmount(string cheatId, LoadoutCheatEffectType expectedType, int fallback)
        {
            if (_cheatService?.AllCheats == null) return fallback;

            for (int i = 0; i < _cheatService.AllCheats.Count; i++)
            {
                CheatDataSO cheat = _cheatService.AllCheats[i];
                if (cheat == null || !string.Equals(cheat.CheatId, cheatId, System.StringComparison.OrdinalIgnoreCase))
                    continue;
                if (cheat.EffectType != expectedType) return fallback;
                return Mathf.Max(0, cheat.EffectAmount);
            }

            return fallback;
        }
    }
}
