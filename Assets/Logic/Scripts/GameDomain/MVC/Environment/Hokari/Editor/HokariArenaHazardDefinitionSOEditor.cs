#if UNITY_EDITOR
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari.Editor
{
    [CustomEditor(typeof(HokariArenaHazardDefinitionSO))]
    public sealed class HokariArenaHazardDefinitionSOEditor : UnityEditor.Editor
    {
        const string CatalogPath =
            "Assets/Logic/Scripts/GameDomain/MVC/Boss/Visuals/CombatAttackVisualCatalog.asset";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var kindProp = serializedObject.FindProperty(nameof(HokariArenaHazardDefinitionSO.DisplacementKind));
            EditorGUILayout.PropertyField(kindProp);

            var def = (HokariArenaHazardDefinitionSO)target;
            var kind = (HokariArenaHazardDisplacementKind)kindProp.enumValueIndex;

            DrawFilteredCatalogTelegraph(def, kind);

            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HokariArenaHazardDefinitionSO.TelegraphDiscRadius)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HokariArenaHazardDefinitionSO.TelegraphSpawn)));
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HokariArenaHazardDefinitionSO.TurnMin)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HokariArenaHazardDefinitionSO.TurnMax)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HokariArenaHazardDefinitionSO.Push)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HokariArenaHazardDefinitionSO.ApplyToBook)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(HokariArenaHazardDefinitionSO.DelayBeforePushSeconds)));

            if (serializedObject.ApplyModifiedProperties())
                def.SyncCatalogAndPushFromDisplacementKind();
        }

        static void DrawFilteredCatalogTelegraph(HokariArenaHazardDefinitionSO def, HokariArenaHazardDisplacementKind kind)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CombatAttackVisualCatalogSO>(CatalogPath);
            var expectedVariant = kind == HokariArenaHazardDisplacementKind.PullTowardTelegraph
                ? HokariArenaHazardTelegraphVariant.Pull
                : HokariArenaHazardTelegraphVariant.Push;

            var options = new System.Collections.Generic.List<(HokariBossAttackVisualId id, string label)>(24);
            foreach (HokariBossAttackVisualId id in System.Enum.GetValues(typeof(HokariBossAttackVisualId)))
            {
                if (id == HokariBossAttackVisualId.None) continue;
                var entry = new HokariArenaHazardCatalogTelegraph { AttackVisualId = id, Variant = expectedVariant };
                var prefab = entry.ResolvePrefab(catalog);
                if (prefab == null) continue;
                options.Add((id, entry.GetDisplayLabel(catalog)));
            }

            if (options.Count == 0)
            {
                EditorGUILayout.HelpBox($"No {expectedVariant} telegraphs in catalog.", MessageType.Warning);
                return;
            }

            int current = 0;
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].id == def.CatalogTelegraph.AttackVisualId)
                {
                    current = i;
                    break;
                }
            }

            string[] labels = new string[options.Count];
            for (int i = 0; i < options.Count; i++) labels[i] = options[i].label;

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUILayout.Popup("Catalog Telegraph", current, labels);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(def, "Set hazard catalog telegraph");
                def.CatalogTelegraph.AttackVisualId = options[picked].id;
                def.CatalogTelegraph.AlignToDisplacementKind(kind);
                EditorUtility.SetDirty(def);
            }

            if (!def.CatalogTelegraph.MatchesDisplacementKind(kind))
            {
                EditorGUILayout.HelpBox("Telegraph variant does not match displacement kind. Click Sync.", MessageType.Warning);
                if (GUILayout.Button("Sync telegraph to displacement"))
                {
                    Undo.RecordObject(def, "Sync hazard telegraph");
                    def.SyncCatalogAndPushFromDisplacementKind();
                    EditorUtility.SetDirty(def);
                }
            }
        }
    }
}
#endif
