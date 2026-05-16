using System;
using Logic.Scripts.GameDomain.MVC.Environment;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari
{
    [Serializable]
    public struct HokariArenaHazardTurnEntry
    {
        [Min(0)] public int TurnMin;
        [Min(0)] public int TurnMax;
        public PlanarPushRequest Push;
        [Tooltip("When true, also pushes the Book if present in the scene.")]
        public bool ApplyToBook;
        [Min(0f)] public float DelayBeforePushSeconds;
    }

    [CreateAssetMenu(fileName = "HokariArenaHazardPattern", menuName = "ScriptableObjects/Environment/Hokari Arena Hazard Pattern")]
    public sealed class HokariArenaHazardPatternSO : ScriptableObject
    {
        [SerializeField] private HokariArenaHazardTurnEntry[] _entries = Array.Empty<HokariArenaHazardTurnEntry>();

        public HokariArenaHazardTurnEntry[] Entries => _entries ?? Array.Empty<HokariArenaHazardTurnEntry>();

        public bool TryGetEntryForTurn(int turnNumber, out HokariArenaHazardTurnEntry entry)
        {
            entry = default;
            if (_entries == null) return false;
            for (int i = 0; i < _entries.Length; i++)
            {
                var e = _entries[i];
                if (turnNumber >= e.TurnMin && turnNumber <= e.TurnMax)
                {
                    entry = e;
                    return true;
                }
            }
            return false;
        }
    }
}
