using System;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>Tile colour counts for the Laki roulette (16 tiles = 8 sectors × 2 bands).</summary>
    [Serializable]
    public struct LakiArenaTileDisposition
    {
        public const int TileCount = 16;

        [Tooltip("Green / positive tiles.")]
        public int PositiveCount;
        [Tooltip("Grey / neutral tiles.")]
        public int NeutralCount;
        [Tooltip("Red / negative tiles.")]
        public int NegativeCount;

        public int Total => PositiveCount + NeutralCount + NegativeCount;

        public static LakiArenaTileDisposition Default =>
            ForTileCount(TileCount, negative: 6, neutral: 5, positive: 5);

        public static LakiArenaTileDisposition ForTileCount(int tileCount, int negative, int neutral, int positive)
        {
            tileCount = Mathf.Max(1, tileCount);
            var d = new LakiArenaTileDisposition
            {
                NegativeCount = Mathf.Max(0, negative),
                NeutralCount = Mathf.Max(0, neutral),
                PositiveCount = Mathf.Max(0, positive),
            };
            return d.NormalizeTo(tileCount);
        }

        /// <summary>Percentages 0–1 for negative / neutral / positive; remainder assigned to neutral.</summary>
        public static LakiArenaTileDisposition FromPercentages(int tileCount, float negative01, float neutral01, float positive01)
        {
            tileCount = Mathf.Max(1, tileCount);
            int neg = Mathf.RoundToInt(tileCount * Mathf.Clamp01(negative01));
            int pos = Mathf.RoundToInt(tileCount * Mathf.Clamp01(positive01));
            int neu = Mathf.RoundToInt(tileCount * Mathf.Clamp01(neutral01));
            return ForTileCount(tileCount, neg, neu, pos);
        }

        public LakiArenaTileDisposition NormalizeTo(int tileCount)
        {
            tileCount = Mathf.Max(1, tileCount);
            int n = Mathf.Max(0, NegativeCount);
            int u = Mathf.Max(0, NeutralCount);
            int p = Mathf.Max(0, PositiveCount);
            int sum = n + u + p;
            if (sum == tileCount) return this;
            if (sum <= 0) return ForTileCount(tileCount, 6, 5, 5);
            float scale = tileCount / (float)sum;
            n = Mathf.RoundToInt(n * scale);
            u = Mathf.RoundToInt(u * scale);
            p = Mathf.RoundToInt(p * scale);
            int fix = tileCount - (n + u + p);
            for (int i = 0; i < Mathf.Abs(fix); i++)
            {
                if (fix > 0) u++;
                else if (u > 0) u--;
                else if (n > 0) n--;
                else if (p > 0) p--;
            }
            return new LakiArenaTileDisposition { NegativeCount = n, NeutralCount = u, PositiveCount = p };
        }
    }
}
