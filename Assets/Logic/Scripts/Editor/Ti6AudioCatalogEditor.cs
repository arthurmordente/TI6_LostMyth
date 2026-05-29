#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Logic.Scripts.GameDomain.ZenjectInstallers;
using Logic.Scripts.Services.AudioService;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.Ti6Editor {
    public static class Ti6AudioCatalogEditor {
        const string MusicFolder = "Assets/Art/Audios/Musics";
        const string SfxFolder = "Assets/SFX's";
        const string MusicAssetPath = "Assets/GameDesign/GameData/Audio/GameplayMusicClips.asset";
        const string SfxAssetPath = "Assets/GameDesign/GameData/Audio/GameplaySfxClips.asset";

        [MenuItem("TI6/Audio/Sync All Catalogs", priority = 0)]
        public static void SyncAll() {
            SyncMusicCatalog();
            SyncSfxCatalog();
            AssetDatabase.SaveAssets();
            Debug.Log("[Audio] Catalogs synced.");
        }

        [MenuItem("TI6/Audio/Sync Music Catalog", priority = 1)]
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

        [MenuItem("TI6/Audio/Sync Sfx Catalog", priority = 2)]
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

            var generalIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in clipPaths) {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var id = fileName.Replace(' ', '_');

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null) continue;

                if (path.Replace('\\', '/').Contains("/Gerais/"))
                    generalIds.Add(id);

                int index = entries.arraySize;
                entries.InsertArrayElementAtIndex(index);
                var element = entries.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("Id").stringValue = id;
                element.FindPropertyRelative("Clip").objectReferenceValue = clip;
            }

            WarnMissingGeneralSfxIds(generalIds);

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(so);
        }

        [MenuItem("TI6/Audio/Validate Setup In Open Scenes", priority = 20)]
        public static void ValidateAudioSetupInOpenScenes() {
            var music = AssetDatabase.LoadAssetAtPath<MusicClipsScriptableObject>(MusicAssetPath);
            var sfx = AssetDatabase.LoadAssetAtPath<SfxClipsScriptableObject>(SfxAssetPath);
            if (music == null || sfx == null)
                Debug.LogWarning("[Audio] GameplayMusicClips or GameplaySfxClips asset missing — run TI6/Audio/Sync All Catalogs.");

            var audioServices = Resources.FindObjectsOfTypeAll<AudioService>();
            foreach (var audio in audioServices) {
                if (audio == null || !audio.gameObject.scene.IsValid()) continue;
                var so = new SerializedObject(audio);
                ValidateDedicatedSource(so, "_musicAudioSource", "Music");
                ValidateDedicatedSource(so, "_sfxUiAudioSource", "SfxUi");
                ValidateDedicatedSource(so, "_sfxCombatAudioSource", "SfxCombat");
                ValidateDedicatedSource(so, "_sfxBossAudioSource", "SfxBoss");
                ValidateDedicatedSource(so, "_sfxAmbienceAudioSource", "SfxAmbience");
            }

            var installers = Resources.FindObjectsOfTypeAll<GameInstaller>();
            foreach (var installer in installers) {
                if (installer == null || !installer.gameObject.scene.IsValid()) continue;
                var so = new SerializedObject(installer);
                var musicRef = so.FindProperty("_gameplayMusicClips").objectReferenceValue;
                var sfxRef = so.FindProperty("_gameplaySfxClips").objectReferenceValue;
                if (musicRef == null)
                    Debug.LogError($"[Audio] GameInstaller on '{installer.name}' is missing GameplayMusicClips.", installer);
                if (sfxRef == null)
                    Debug.LogError($"[Audio] GameInstaller on '{installer.name}' is missing GameplaySfxClips.", installer);
                if (sfxRef != null && sfx != null && sfxRef != sfx)
                    Debug.LogWarning($"[Audio] GameInstaller on '{installer.name}' uses a SfxClips asset other than {SfxAssetPath}.", installer);
            }

            Debug.Log($"[Audio] Validated {audioServices.Length} AudioService(s) and {installers.Length} GameInstaller(s) in open scenes.");
        }

        [MenuItem("TI6/Audio/Wire GameInstaller In Open Scenes", priority = 21)]
        public static void WireGameInstaller() {
            var music = AssetDatabase.LoadAssetAtPath<MusicClipsScriptableObject>(MusicAssetPath);
            var sfx = AssetDatabase.LoadAssetAtPath<SfxClipsScriptableObject>(SfxAssetPath);
            if (music == null || sfx == null)
                SyncAll();

            music = AssetDatabase.LoadAssetAtPath<MusicClipsScriptableObject>(MusicAssetPath);
            sfx = AssetDatabase.LoadAssetAtPath<SfxClipsScriptableObject>(SfxAssetPath);

            var installers = Resources.FindObjectsOfTypeAll<GameInstaller>();
            foreach (var installer in installers) {
                var so = new SerializedObject(installer);
                so.FindProperty("_gameplayMusicClips").objectReferenceValue = music;
                so.FindProperty("_gameplaySfxClips").objectReferenceValue = sfx;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(installer);
            }

            Debug.Log($"[Audio] Wired {installers.Length} GameInstaller(s).");
        }

        static void WarnMissingGeneralSfxIds(HashSet<string> syncedGeneralIds) {
            string[] expected = {
                SfxIds.UI_Clique, SfxIds.UI_Clique2, SfxIds.UI_Tela_Vitoria, SfxIds.UI_Tela_Derrota,
                SfxIds.UI_Portal, SfxIds.UI_Novo_Turno, SfxIds.UI_Dados, SfxIds.NPC_Falando
            };
            foreach (var id in expected) {
                if (!syncedGeneralIds.Contains(id))
                    Debug.LogWarning($"[Audio] Gerais folder missing clip for expected id '{id}'.");
            }
        }

        static void ValidateDedicatedSource(SerializedObject audioSo, string propertyName, string label) {
            var prop = audioSo.FindProperty(propertyName);
            if (prop == null || prop.objectReferenceValue == null)
                Debug.LogError($"[Audio] AudioService missing dedicated {label} AudioSource ({propertyName}).", audioSo.targetObject);
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
