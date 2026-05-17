using System;
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari
{
    [Serializable]
    public struct HokariArenaHazardCatalogTelegraph
    {
        public HokariBossAttackVisualId AttackVisualId;
        public HokariArenaHazardTelegraphVariant Variant;

        public GameObject ResolvePrefab(CombatAttackVisualCatalogSO catalog)
        {
            if (catalog == null) return null;
            bool isPull = Variant == HokariArenaHazardTelegraphVariant.Pull;
            bool isPush = Variant == HokariArenaHazardTelegraphVariant.Push;
            return catalog.GetTelegraph(AttackVisualId, isPull, isPush);
        }

        public string GetDisplayLabel(CombatAttackVisualCatalogSO catalog)
        {
            var prefab = ResolvePrefab(catalog);
            string prefabName = prefab != null ? prefab.name : "(missing)";
            return $"{AttackVisualId} / {Variant} — {prefabName}";
        }
    }
}
