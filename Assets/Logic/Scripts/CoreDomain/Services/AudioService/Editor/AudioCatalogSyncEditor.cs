#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.Services.AudioService.Editor {
    public static class AudioCatalogSyncEditor {
        const string MusicFolder = "Assets/Art/Audios/Musics";
        const string SfxFolder = "Assets/SFX's";
        const string MusicAssetPath = "Assets/GameDesign/GameData/Audio/GameplayMusicClips.asset";
        const string SfxAssetPath = "Assets/GameDesign/GameData/Audio/GameplaySfxClips.asset";

        [MenuItem("TI6/Audio/Sync Music && Sfx Catalogs")]
        public static void SyncAll() {
            SyncMusicCatalog();
            SyncSfxCatalog();
            AssetDatabase.SaveAssets();
            Debug.Log("[Audio] Catalogs synced.");
        }

        [MenuItem("TI6/Audio/Sync Music Catalog")]
        public static void SyncMusicCatalog() {
            var so = LoadOrCreate<MusicClipsScriptableObject>(MusicAssetPath);
            var serialized = new SerializedObject(so);
            var entries = serialized.FindProperty("_entries");
            entries.ClearArray();

            AddMusicEntry(entries, MusicIds.Menu, $"{MusicFolder}/Menu_Lost Dreams mp3.mp3");
            AddMusicEntry(entries, MusicIds.FightHokari, $"{MusicFolder}/Hocari_song.mp3");
            AddMusicEntry(entries, MusicIds.FightLaki, $"{MusicFolder}/Laki_song.mp3");

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);
        }

        [MenuItem("TI6/Audio/Sync Sfx Catalog from SFX's folder")]
        public static void SyncSfxCatalog() {
            var so = LoadOrCreate<SfxClipsScriptableObject>(SfxAssetPath);
            var serialized = new SerializedObject(so);
            var entries = serialized.FindProperty("_entries");
            entries.ClearArray();

            var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { SfxFolder });
            var clipPaths = new List<string>();
            foreach (var guid in guids) {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    clipPaths.Add(path);
            }
            clipPaths.Sort(StringComparer.OrdinalIgnoreCase);

            foreach (var path in clipPaths) {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var id = fileName.Replace(' ', '_');

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                int index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                var element = entries.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("Id").stringValue = id;
                element.FindPropertyRelative("Clip").objectReferenceValue = clip;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);
        }

        [MenuItem("TI6/Audio/Wire GameInstaller In Open Scenes")]
        public static void WireGameInstaller() {
            var music = AssetDatabase.LoadAssetAtPath<MusicClipsScriptableObject>(MusicAssetPath);
            var sfx = AssetDatabase.LoadAssetAtPath<SfxClipsScriptableObject>(SfxAssetPath);
            if (music == null || sfx == null)
                SyncAll();

            music = AssetDatabase.LoadAssetAtPath<MusicClipsScriptableObject>(MusicAssetPath);
            sfx = AssetDatabase.LoadAssetAtPath<SfxClipsScriptableObject>(SfxAssetPath);

            var installers = Resources.FindObjectsOfTypeAll<Logic.Scripts.GameDomain.ZenjectInstallers.GameInstaller>();
            foreach (var installer in installers) {
                var so = new SerializedObject(installer);
                so.FindProperty("_gameplayMusicClips").objectReferenceValue = music;
                so.FindProperty("_gameplaySfxClips").objectReferenceValue = sfx;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(installer);
            }

            Debug.Log($"[Audio] Wired {installers.Length} GameInstaller(s).");
        }

        static void AddMusicEntry(SerializedProperty entries, string id, string clipPath) {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) {
                Debug.LogError($"[Audio] Missing music clip at {clipPath}");
                return;
            }
            int index = entries.arraySize;
            entries.InsertArrayElementAtIndex(index);
            var element = entries.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("Id").stringValue = id;
            element.FindPropertyRelative("Clip").objectReferenceValue = clip;
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir)) {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
#endif
