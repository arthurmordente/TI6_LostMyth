#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Hocari.Editor
{
    public static class HocariAnimationClipExporter
    {
        public const string RootPath = "Assets/Art/Animations/Hocari/Exported";
        public const string Phase1Path = RootPath + "/Phase1";
        public const string Phase2Path = RootPath + "/Phase2";
        public const string SharedPath = RootPath + "/Shared";
        public const string LegacyPath = RootPath + "/Legacy";

        private const string Anim1Fbx = "Assets/Art/FBX/Characters/HocariAnimations1.fbx";
        private const string Anim2Fbx = "Assets/Art/FBX/Characters/HocariAnimations2.fbx";
        private const string LegacyFbx = "Assets/Art/FBX/Characters/HOC_Hocari.fbx";

        private static readonly string[] Phase1FromAnim2 =
        {
            "Hocari_CombatIdle_2",
            "Hocari_Attack_Protean_2_Prep",
            "Hocari_Attack_Protean_2_Loop",
            "Hocari_Attack_Protean_2_Finish",
            "Hocari_Attack_Circle_Prep",
            "Hocari_Attack_Circle_Loop",
            "Hocari_Attack_Circle_Finish",
            "Hocari_Attack_Donut_Prep",
            "Hocari_Attack_Donut_Loop",
            "Hocari_Attack_Donut_Finish",
            "Hocari_Attack_SwordLines_Loop",
            "Hocari_Attack_SwordLines_Finish",
            "Hocari_Attack_WingSlash_Left_Prep",
            "Hocari_Attack_WingSlash_Left_Loop",
            "Hocari_Attack_WingSlash_Left_Finish",
            "Hocari_Attack_WingSlash_Right_Prep",
            "Hocari_Attack_WingSlash_Right_Loop",
            "Hocari_Attack_WingSlash_Right_Finish",
            "Hocari_Hit",
        };

        private static readonly string[] Phase1FromAnim1 = { "Hocari_Attack_SwordLines_Prep" };

        private static readonly string[] Phase2FromAnim1 =
        {
            "Hocari_Phase2_CombatIdle",
            "Hocari_Phase2_Attack_Protean_Prep",
            "Hocari_Phase2_Attack_Protean_Loop",
            "Hocari_Phase2_Attack_Protean_Finish",
            "Hocari_Phase2_Attack_Circle_Prep",
            "Hocari_Phase2_Attack_Circle_Loop",
            "Hocari_Phase2_Attack_Circle_Finish",
            "Hocari_Phase2_Attack_Donut_Prep",
            "Hocari_Phase2_Attack_Donut_Loop",
            "Hocari_Phase2_Attack_Donut_Finish",
            "Hocari_Phase2_Attack_SwordLines_Prep",
            "Hocari_Phase2_Attack_SwordLines_Loop",
            "Hocari_Phase2_Attack_SwordLines_Finish",
            "Hocari_Phase2_Attack_WingSlash_Left_Prep",
            "Hocari_Phase2_Attack_WingSlash_Left_Loop",
            "Hocari_Phase2_Attack_WingSlash_Left_Finish",
            "Hocari_Phase2_Attack_WingSlash_Right_Loop",
            "Hocari_Phase2_Attack_WingSlash_Right_Finish",
            "Hocari_Phase2_Hit",
            "Hocari_Phase2_Death",
        };

        private static readonly string[] Phase2FromAnim2 = { "Hocari_Phase2_Attack_WingSlash_Right_Prep" };

        private static readonly string[] SharedFromAnim1 =
        {
            "Hocari_PhaseTransition_Prep",
            "Hocari_PhaseTransition_Loop",
            "Hocari_PhaseTransition_Finish",
            "Hocari_PhaseTransition_Finish_2",
            "Hocari_Movement_Prep",
            "Hocari_Movement_Loop",
            "Hocari_Movement_Loop_2",
            "Hocari_Movement_Loop_3",
            "Hocari_Movement_Finish",
        };

        private const string FromAnim1Path = RootPath + "/FromAnim1";
        private const string FromAnim2Path = RootPath + "/FromAnim2";

        public static void ExportAllFbxClips()
        {
            EnsureFolders();
            EnsureFolder(FromAnim1Path);
            EnsureFolder(FromAnim2Path);

            int fromAnim1 = ExportEntireFbx(Anim1Fbx, FromAnim1Path);
            int fromAnim2 = ExportEntireFbx(Anim2Fbx, FromAnim2Path);
            int fromLegacy = ExportEntireFbx(LegacyFbx, LegacyPath);
            int gameplay = ExportGameplayClips();

            FixExportedClipObjectNames();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HocariAnimationClipExporter] FBX dump: Anim1={fromAnim1}, Anim2={fromAnim2}, Legacy={fromLegacy}. Gameplay folders: {gameplay} clips. Root: {RootPath}");
        }

        public static int ExportGameplayClips()
        {
            EnsureFolders();
            var index = BuildFbxClipIndex(Anim1Fbx, Anim2Fbx, LegacyFbx);
            int exported = 0;
            int missing = 0;

            exported += ExportList(index, Phase1FromAnim2, Anim2Fbx, Phase1Path, ref missing);
            exported += ExportList(index, Phase1FromAnim1, Anim1Fbx, Phase1Path, ref missing);
            exported += ExportList(index, Phase2FromAnim1, Anim1Fbx, Phase2Path, ref missing);
            exported += ExportList(index, Phase2FromAnim2, Anim2Fbx, Phase2Path, ref missing);
            exported += ExportList(index, SharedFromAnim1, Anim1Fbx, SharedPath, ref missing);

            if (missing > 0)
                Debug.LogWarning($"[HocariAnimationClipExporter] Gameplay export missing {missing} expected clips.");

            return exported;
        }

        private static int ExportEntireFbx(string fbxPath, string destFolder)
        {
            var index = BuildFbxClipIndex(fbxPath);
            int count = 0;
            foreach (var pair in index)
            {
                WriteClipCopy(pair.Value, $"{destFolder}/{pair.Key}.anim");
                count++;
            }

            return count;
        }

        public static AnimationClip LoadClip(string folder, string clipName)
        {
            var path = $"{RootPath}/{folder}/{clipName}.anim";
            return AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        }

        private static int ExportList(
            Dictionary<string, AnimationClip> index,
            string[] clipNames,
            string expectedFbx,
            string destFolder,
            ref int missing)
        {
            int count = 0;
            foreach (var name in clipNames)
            {
                if (!index.TryGetValue(name, out var source) || source == null)
                {
                    Debug.LogWarning($"[HocariAnimationClipExporter] Missing '{name}' (expected in {expectedFbx}).");
                    missing++;
                    continue;
                }

                WriteClipCopy(source, $"{destFolder}/{name}.anim");
                count++;
            }

            return count;
        }

        /// <summary>
        /// Renames exported clip main objects to match their .anim filename
        /// (strips FBX prefix e.g. HocariArmature|ClipName → ClipName).
        /// </summary>
        public static int FixExportedClipObjectNames()
        {
            if (!AssetDatabase.IsValidFolder(RootPath))
            {
                Debug.LogWarning($"[HocariAnimationClipExporter] Folder not found: {RootPath}");
                return 0;
            }

            int fixedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { RootPath }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null) continue;

                var expectedName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrEmpty(expectedName) || clip.name == expectedName) continue;

                clip.name = expectedName;
                EditorUtility.SetDirty(clip);
                fixedCount++;
            }

            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[HocariAnimationClipExporter] Fixed {fixedCount} clip object name(s) under {RootPath}.");
            return fixedCount;
        }

        private static void WriteClipCopy(AnimationClip source, string destPath)
        {
            var assetName = Path.GetFileNameWithoutExtension(destPath);
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(source, existing);
                existing.name = assetName;
                EditorUtility.SetDirty(existing);
                return;
            }

            var copy = Object.Instantiate(source);
            copy.name = assetName;
            AssetDatabase.CreateAsset(copy, destPath);
        }

        /// <summary>
        /// FBX Humanoid clips are often named "Armature|ClipName". Unity filenames cannot contain '|'.
        /// </summary>
        private static string SanitizeClipAssetName(string clipName)
        {
            if (string.IsNullOrEmpty(clipName)) return clipName;

            var name = clipName;
            var pipe = name.LastIndexOf('|');
            if (pipe >= 0 && pipe < name.Length - 1)
                name = name[(pipe + 1)..];

            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name;
        }

        private static Dictionary<string, AnimationClip> BuildFbxClipIndex(params string[] fbxPaths)
        {
            var index = new Dictionary<string, AnimationClip>();
            foreach (var path in fbxPaths)
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is not AnimationClip clip) continue;
                    if (clip.name.StartsWith("__")) continue;

                    var key = SanitizeClipAssetName(clip.name);
                    if (string.IsNullOrEmpty(key)) continue;
                    index[key] = clip;
                }
            }

            return index;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/Art/Animations/Hocari");
            EnsureFolder(RootPath);
            EnsureFolder(Phase1Path);
            EnsureFolder(Phase2Path);
            EnsureFolder(SharedPath);
            EnsureFolder(LegacyPath);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
