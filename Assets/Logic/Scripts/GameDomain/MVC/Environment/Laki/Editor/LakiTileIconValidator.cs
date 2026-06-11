#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki.Editor
{
    public static class LakiTileIconValidator
    {
        const string BossPrefabPath = "Assets/GameDesign/Prefabs/Bosses/Laki/LKI_LakiPrefabBoss.prefab";
        const string HealIconPath = "Assets/Ui/Images/Luana/Icons_Skills/iconSkill_Heal.png";
        const string DamageIconPath = "Assets/Ui/Images/Luana/Icons_Skills/DanoCaster_Icon.png";
        const string ManaIconPath = "Assets/Ui/Images/Luana/Icons_Skills/Mana_Icon.png";

        static readonly string[] PoolPropertyNames =
        {
            "_largePositiveEffects",
            "_smallPositiveEffects",
            "_largeNegativeEffects",
            "_smallNegativeEffects",
        };

        [MenuItem("TI6/Laki/Validate Tile Icons on Boss Prefab")]
        public static void ValidateBossPrefabTileIcons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[LakiTileIconValidator] Prefab not found: {BossPrefabPath}");
                return;
            }

            var bootstrap = prefab.GetComponent<LakiArenaBossBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("[LakiTileIconValidator] LakiArenaBossBootstrap missing on boss prefab.");
                return;
            }

            var so = new SerializedObject(bootstrap);
            var missing = new List<string>();

            foreach (var poolName in PoolPropertyNames)
            {
                var pool = so.FindProperty(poolName);
                if (pool == null || !pool.isArray) continue;

                for (int i = 0; i < pool.arraySize; i++)
                {
                    var element = pool.GetArrayElementAtIndex(i);
                    var nameProp = element.FindPropertyRelative("Name");
                    var iconProp = element.FindPropertyRelative("TileIcon");
                    string effectName = nameProp != null ? nameProp.stringValue : "?";
                    if (iconProp != null && iconProp.objectReferenceValue != null) continue;
                    missing.Add($"  [{poolName}] {effectName}");
                }
            }

            if (missing.Count == 0)
            {
                Debug.Log("[LakiTileIconValidator] All tile effect pools have TileIcon assigned.");
                return;
            }

            Debug.LogWarning($"[LakiTileIconValidator] {missing.Count} effect(s) missing TileIcon:\n" + string.Join("\n", missing)
                + $"\nSuggested sprites: Heal → {HealIconPath}, Damage → {DamageIconPath}, AP → {ManaIconPath}"
                + "\nAssign in Inspector on LKI_LakiPrefabBoss → LakiArenaBossBootstrap effect pools.");
        }
    }
}
#endif
