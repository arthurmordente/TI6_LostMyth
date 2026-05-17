using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    [CreateAssetMenu(fileName = "LakiArenaPhaseTileDisposition", menuName = "ScriptableObjects/Environment/Laki Arena Phase Tile Disposition")]
    public sealed class LakiArenaPhaseTileDispositionSO : ScriptableObject
    {
        [System.Serializable]
        public struct PhaseEntry
        {
            public string Name;
            [Tooltip("Red / negative tile count (of 16).")]
            public int NegativeCount;
            [Tooltip("Grey / neutral tile count.")]
            public int NeutralCount;
            [Tooltip("Green / positive tile count.")]
            public int PositiveCount;

            public LakiArenaTileDisposition ToDisposition() =>
                LakiArenaTileDisposition.ForTileCount(
                    LakiArenaTileDisposition.TileCount,
                    NegativeCount,
                    NeutralCount,
                    PositiveCount);
        }

        [SerializeField] private PhaseEntry[] _phases = System.Array.Empty<PhaseEntry>();

        public int PhaseCount => _phases != null ? _phases.Length : 0;

        public LakiArenaTileDisposition Resolve(int phaseIndex)
        {
            if (_phases == null || _phases.Length == 0)
                return LakiArenaTileDisposition.Default;
            int i = Mathf.Clamp(phaseIndex, 0, _phases.Length - 1);
            return _phases[i].ToDisposition();
        }

        void OnValidate()
        {
            if (_phases == null) return;
            for (int i = 0; i < _phases.Length; i++)
            {
                var e = _phases[i];
                var normalized = e.ToDisposition();
                e.NegativeCount = normalized.NegativeCount;
                e.NeutralCount = normalized.NeutralCount;
                e.PositiveCount = normalized.PositiveCount;
                _phases[i] = e;
            }
        }
    }
}
