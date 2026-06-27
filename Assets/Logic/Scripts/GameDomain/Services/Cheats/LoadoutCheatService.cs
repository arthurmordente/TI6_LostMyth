using System;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.Cheats
{
    public class LoadoutCheatService : ILoadoutCheatService
    {
        const string PlayerPrefsPrefix = "LoadoutCheat_";

        readonly CheatDataSO[] _catalog;
        readonly Dictionary<string, bool> _enabledById = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<CheatDataSO> AllCheats => _catalog;
        public event Action OnCheatsChanged;

        public LoadoutCheatService(CheatDataSO[] catalog)
        {
            _catalog = catalog ?? Array.Empty<CheatDataSO>();
            LoadFromPlayerPrefs();
        }

        public bool IsEnabled(string cheatId)
        {
            if (string.IsNullOrEmpty(cheatId)) return false;
            return _enabledById.TryGetValue(cheatId, out bool enabled) && enabled;
        }

        public bool IsEnabled(CheatDataSO cheat) =>
            cheat != null && IsEnabled(cheat.CheatId);

        public void SetEnabled(string cheatId, bool enabled)
        {
            if (string.IsNullOrEmpty(cheatId)) return;

            _enabledById[cheatId] = enabled;
            PlayerPrefs.SetInt(PrefsKey(cheatId), enabled ? 1 : 0);
            PlayerPrefs.Save();
            OnCheatsChanged?.Invoke();
        }

        void LoadFromPlayerPrefs()
        {
            _enabledById.Clear();
            for (int i = 0; i < _catalog.Length; i++)
            {
                CheatDataSO cheat = _catalog[i];
                if (cheat == null || string.IsNullOrEmpty(cheat.CheatId)) continue;
                bool enabled = PlayerPrefs.GetInt(PrefsKey(cheat.CheatId), 0) == 1;
                _enabledById[cheat.CheatId] = enabled;
            }
        }

        static string PrefsKey(string cheatId) => PlayerPrefsPrefix + cheatId;
    }
}
