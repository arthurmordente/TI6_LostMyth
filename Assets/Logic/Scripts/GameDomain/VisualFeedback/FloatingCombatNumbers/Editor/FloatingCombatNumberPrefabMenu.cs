#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

namespace Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers.Editor
{
    public static class FloatingCombatNumberPrefabMenu
    {
        const string DefaultPath = "Assets/Logic/Prefabs/Combat/FloatingCombatNumber.prefab";

        [MenuItem("TI6/Combat/Create Floating Combat Number Prefab")]
        public static void CreatePrefab()
        {
            var root = new GameObject("FloatingCombatNumber");
            var tmp = root.AddComponent<TextMeshPro>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 4f;
            tmp.rectTransform.sizeDelta = new Vector2(4f, 2f);
            root.AddComponent<FloatingCombatNumberView>();

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(DefaultPath) ?? "Assets");
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, DefaultPath);
            Object.DestroyImmediate(root);

            if (prefab != null)
            {
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
                Debug.Log($"[FloatingCombatNumber] Prefab created at {DefaultPath}. Assign it on GamePlayInstaller → Floating Combat Number Prefab.");
            }
        }
    }
}
#endif
