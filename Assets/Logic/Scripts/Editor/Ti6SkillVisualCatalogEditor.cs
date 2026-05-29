#if UNITY_EDITOR
using System;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.Ti6Editor {
    public static class Ti6SkillVisualCatalogEditor {
        const string CatalogAssetPath = "Assets/GameDesign/GameData/Skills/SkillVisualCatalog.asset";

        [MenuItem("TI6/Skills/Validate Skill Visual Catalog")]
        public static void ValidateCatalog() {
            var catalog = AssetDatabase.LoadAssetAtPath<SkillVisualCatalogSO>(CatalogAssetPath);
            if (catalog == null) {
                Debug.LogWarning($"[Skills] Missing asset at {CatalogAssetPath}. Create via Create > Scriptable Objects > Skills > Skill Visual Catalog.");
                return;
            }

            int missing = 0;
            foreach (SkillDivinity divinity in SkillDivinityUtil.AllValues) {
                foreach (SkillType skillType in Enum.GetValues(typeof(SkillType))) {
                    var st = (SkillType)skillType;
                    if (!catalog.TryGet(divinity, st, out var bg, out var frame)) {
                        Debug.LogWarning($"[Skills] Missing catalog entry: {SkillDivinityUtil.DisplayLabel(divinity)} + {st}");
                        missing++;
                        continue;
                    }
                    if (bg == null)
                        Debug.LogWarning($"[Skills] Missing BackgroundPaint: {SkillDivinityUtil.DisplayLabel(divinity)} + {st}", catalog);
                    if (frame == null)
                        Debug.LogWarning($"[Skills] Missing Frame: {SkillDivinityUtil.DisplayLabel(divinity)} + {st}", catalog);
                    if (bg == null || frame == null) missing++;
                }
            }

            if (missing == 0)
                Debug.Log("[Skills] SkillVisualCatalog has all 20 divinity×type pairs with background and frame.");
            else
                Debug.LogWarning($"[Skills] SkillVisualCatalog has {missing} missing or incomplete entries.");
        }
    }
}
#endif
