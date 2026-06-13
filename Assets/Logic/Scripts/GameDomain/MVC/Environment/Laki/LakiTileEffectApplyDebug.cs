using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    static class LakiTileEffectApplyDebug
    {
        public static void LogUnitOnTile(string unitLabel, int turn, int tileIndex, RouletteArenaService.TileEffectType tileType, Vector3 worldPos)
        {
            if (tileIndex < 0)
            {
                Debug.Log(
                    $"[LakiTile][{unitLabel}] Turn={turn} — fora da arena (tile=-1) pos=({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})");
                return;
            }

            Debug.Log(
                $"[LakiTile][{unitLabel}] Turn={turn} — detectado na casa {tileIndex} " +
                $"(tipo={tileType}) pos=({worldPos.x:F2}, {worldPos.y:F2}, {worldPos.z:F2})");
        }

        public static void LogApplyStart(string unitLabel, int turn, int tileIndex, RouletteArenaService.TileEffectType tileType, int effectCount, bool usingFallback)
        {
            Debug.Log(
                $"[LakiTile][{unitLabel}] Turn={turn} casa={tileIndex} tipo={tileType} — " +
                $"a aplicar {(usingFallback ? "fallback" : $"{effectCount} efeito(s)")}");
        }

        public static void LogEffectStep(string unitLabel, int tileIndex, int stepIndex, string effectTypeName, string effectDisplayName)
        {
            Debug.Log(
                $"[LakiTile][{unitLabel}] casa={tileIndex} efeito[{stepIndex}] " +
                $"{effectTypeName} (nome=\"{effectDisplayName}\")");
        }

        public static void LogApplyComplete(string unitLabel, int turn, int tileIndex, string appliedSummary)
        {
            Debug.Log(
                $"[LakiTile][{unitLabel}] Turn={turn} casa={tileIndex} — efeitos aplicados: {appliedSummary ?? "nenhum"}");
        }

        public static void LogManaDelta(string unitLabel, string source, int before, int after, int requestedDelta)
        {
            Debug.Log(
                $"[LakiTile][{unitLabel}][Mana] {source} pedido={requestedDelta} AP {before} → {after}");
        }

        public static void LogManaSkipped(string unitLabel, string source, string reason)
        {
            Debug.LogWarning($"[LakiTile][{unitLabel}][Mana] {source} ignorado — {reason}");
        }
    }
}
