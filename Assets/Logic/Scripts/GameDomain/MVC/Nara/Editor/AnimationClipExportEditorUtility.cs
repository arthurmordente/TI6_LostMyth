#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Editor
{
    internal static class AnimationClipExportEditorUtility
    {
        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parts = path.Replace('\\', '/').Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        public static void RecreateExportFolder(string folderAssetPath)
        {
            if (AssetDatabase.IsValidFolder(folderAssetPath))
                AssetDatabase.DeleteAsset(folderAssetPath);
            EnsureFolder(folderAssetPath);
        }

        public static void DeleteLegacyExportFolders()
        {
            foreach (var path in AnimationFinalExportPaths.LegacyExportRoots)
            {
                if (!AssetDatabase.IsValidFolder(path)) continue;
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[AnimationExport] Removed legacy export folder: {path}");
            }
        }

        public static Dictionary<string, AnimationClip> BuildFbxClipIndex(params string[] fbxPaths)
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

        public static string SanitizeClipAssetName(string clipName)
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

        public static void WriteClipCopy(AnimationClip source, string destPath)
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

        public static int ExportAllClipsFromFbx(string fbxPath, string destFolder)
        {
            EnsureFolder(destFolder);
            var index = BuildFbxClipIndex(fbxPath);
            int count = 0;
            foreach (var pair in index)
            {
                WriteClipCopy(pair.Value, $"{destFolder}/{pair.Key}.anim");
                count++;
            }

            return count;
        }

        public static int ExportClipList(
            Dictionary<string, AnimationClip> index,
            string[] clipNames,
            string expectedFbx,
            string destFolder,
            ref int missing)
        {
            EnsureFolder(destFolder);
            int count = 0;
            foreach (var name in clipNames)
            {
                if (!index.TryGetValue(name, out var source) || source == null)
                {
                    Debug.LogWarning($"[AnimationExport] Missing '{name}' (expected in {expectedFbx}).");
                    missing++;
                    continue;
                }

                WriteClipCopy(source, $"{destFolder}/{name}.anim");
                count++;
            }

            return count;
        }

        public static int FixClipObjectNamesUnder(string rootPath)
        {
            if (!AssetDatabase.IsValidFolder(rootPath)) return 0;

            int fixedCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { rootPath }))
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

            return fixedCount;
        }

        public static AnimationClip LoadExportedClip(string folder, string clipName) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>($"{folder}/{clipName}.anim");
    }
}
#endif
