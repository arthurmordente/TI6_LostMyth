#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Editor
{
    public static class ErzahlerErzaClipExporter
    {
        public const string ExportPath = "Assets/Art/Animations/Erzahler/Exported";
        private const string ErzahlerFinalFbx = "Assets/Art/FBX/Characters/ErzahlerFinal.fbx";

        private static readonly string[] ClipNames =
        {
            "Erzahler_Death",
            "Erzahler_Hit",
            "Erzahler_BetWon",
            "Erzahler_BetLost",
            "Erzahler_Conjuring_Fail",
            "Book_CreateClone",
            "Book_ReturnClone",
        };

        public static int ExportAllFbxClips()
        {
            EnsureFolder(ExportPath);
            var index = BuildFbxClipIndex(ErzahlerFinalFbx);
            int exported = 0;

            foreach (var pair in index)
            {
                WriteClipCopy(pair.Value, $"{ExportPath}/{pair.Key}.anim");
                exported++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return exported;
        }

        public static void ExportGameplayClips()
        {
            EnsureFolder(ExportPath);
            var index = BuildFbxClipIndex(ErzahlerFinalFbx);
            int exported = 0;
            int missing = 0;

            foreach (var name in ClipNames)
            {
                if (!index.TryGetValue(name, out var source) || source == null)
                {
                    Debug.LogWarning($"[ErzahlerErzaClipExporter] Missing clip '{name}' in {ErzahlerFinalFbx}.");
                    missing++;
                    continue;
                }

                WriteClipCopy(source, $"{ExportPath}/{name}.anim");
                exported++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ErzahlerErzaClipExporter] Exported {exported} gameplay clips to {ExportPath}. Missing: {missing}.");
        }

        public static AnimationClip LoadExported(string clipName) =>
            AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ExportPath}/{clipName}.anim");

        private static void WriteClipCopy(AnimationClip source, string destPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(destPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(source, existing);
                EditorUtility.SetDirty(existing);
                return;
            }

            var copy = Object.Instantiate(source);
            copy.name = source.name;
            AssetDatabase.CreateAsset(copy, destPath);
        }

        private static Dictionary<string, AnimationClip> BuildFbxClipIndex(string fbxPath)
        {
            var index = new Dictionary<string, AnimationClip>();
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            {
                if (asset is not AnimationClip clip) continue;
                if (clip.name.StartsWith("__")) continue;
                index[clip.name] = clip;
            }

            return index;
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
