using System;
using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.Services.Cheats
{
    public interface ILoadoutCheatService
    {
        IReadOnlyList<CheatDataSO> AllCheats { get; }
        bool IsEnabled(string cheatId);
        bool IsEnabled(CheatDataSO cheat);
        void SetEnabled(string cheatId, bool enabled);
        event Action OnCheatsChanged;
    }
}
