#if UNITY_EDITOR
using System.Collections.Generic;
using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari.Editor
{
    [CustomPropertyDrawer(typeof(HokariArenaHazardCatalogTelegraph))]
    public sealed class HokariArenaHazardCatalogTelegraphDrawer : PropertyDrawer
    {
        const string CatalogPath =
            "Assets/Logic/Scripts/GameDomain/MVC/Boss/Visuals/CombatAttackVisualCatalog.asset";

        struct TelegraphOption
        {
            public HokariBossAttackVisualId VisualId;
            public HokariArenaHazardTelegraphVariant Variant;
            public string Label;
        }

        static List<TelegraphOption> _options;
        static string[] _labels;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            var visualProp = property.FindPropertyRelative(nameof(HokariArenaHazardCatalogTelegraph.AttackVisualId));
            var variantProp = property.FindPropertyRelative(nameof(HokariArenaHazardCatalogTelegraph.Variant));

            EnsureOptions();
            int current = FindIndex(
                (HokariBossAttackVisualId)visualProp.enumValueIndex,
                (HokariArenaHazardTelegraphVariant)variantProp.enumValueIndex);

            Rect row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
            int picked = EditorGUI.Popup(row, label.text, current, _labels);
            if (picked >= 0 && picked < _options.Count)
            {
                visualProp.enumValueIndex = (int)_options[picked].VisualId;
                variantProp.enumValueIndex = (int)_options[picked].Variant;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUIUtility.singleLineHeight;

        static void EnsureOptions()
        {
            if (_options != null && _labels != null) return;

            var catalog = AssetDatabase.LoadAssetAtPath<CombatAttackVisualCatalogSO>(CatalogPath);
            _options = new List<TelegraphOption>(48);
            var labels = new List<string>(48);

            foreach (HokariBossAttackVisualId id in System.Enum.GetValues(typeof(HokariBossAttackVisualId)))
            {
                if (id == HokariBossAttackVisualId.None) continue;
                TryAdd(catalog, id, HokariArenaHazardTelegraphVariant.Normal, labels);
                TryAdd(catalog, id, HokariArenaHazardTelegraphVariant.Pull, labels);
                TryAdd(catalog, id, HokariArenaHazardTelegraphVariant.Push, labels);
            }

            _labels = labels.ToArray();
        }

        static void TryAdd(
            CombatAttackVisualCatalogSO catalog,
            HokariBossAttackVisualId id,
            HokariArenaHazardTelegraphVariant variant,
            List<string> labels)
        {
            var entry = new HokariArenaHazardCatalogTelegraph { AttackVisualId = id, Variant = variant };
            var prefab = entry.ResolvePrefab(catalog);
            if (prefab == null) return;
            _options.Add(new TelegraphOption { VisualId = id, Variant = variant, Label = entry.GetDisplayLabel(catalog) });
            labels.Add(_options[_options.Count - 1].Label);
        }

        static int FindIndex(HokariBossAttackVisualId id, HokariArenaHazardTelegraphVariant variant)
        {
            if (_options == null) return 0;
            for (int i = 0; i < _options.Count; i++)
            {
                if (_options[i].VisualId == id && _options[i].Variant == variant)
                    return i;
            }
            return 0;
        }
    }
}
#endif
