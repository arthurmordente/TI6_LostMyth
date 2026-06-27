#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Scenario.Lobby.Editor
{
    public static class LobbyInteractionZoneEditorSetup
    {
        const string LibraryPrefabPath = "Assets/GameDesign/Prefabs/LobbyLibrary/Library.prefab";
        const string PopUpPrefabPath = "Assets/Ui/UI_Jordan/PopUp_Interactable.prefab";

        [MenuItem("TI6/Lobby/Setup Interaction Zones In Library Prefab")]
        public static void SetupLibraryPrefab()
        {
            var libraryPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LibraryPrefabPath);
            if (libraryPrefab == null)
            {
                Debug.LogError($"[LobbyZones] Prefab not found at {LibraryPrefabPath}");
                return;
            }

            var popUpPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PopUpPrefabPath);
            if (popUpPrefab == null)
            {
                Debug.LogError($"[LobbyZones] PopUp prefab not found at {PopUpPrefabPath}");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(LibraryPrefabPath);
            try
            {
                var scenarioView = root.GetComponent<LevelScenarioView>();
                if (scenarioView == null)
                {
                    Debug.LogError("[LobbyZones] LevelScenarioView not found on Library prefab root.");
                    return;
                }

                RemoveLegacyZones(root.transform);
                DisableLegacyInteractable(root);

                var oganjdan = FindChild(root.transform, "InteractableOgandjan");
                var skillZone = CreateZone(
                    root.transform,
                    "Zone_SkillLoadout",
                    oganjdan != null ? oganjdan.position : new Vector3(9.66f, 0f, 2.46f),
                    new Vector3(8f, 3f, 8f),
                    LobbyInteractionKind.SkillLoadout);

                var tipsZone = CreateZone(
                    root.transform,
                    "Zone_Tips",
                    root.transform.TransformPoint(new Vector3(-6f, 0f, 0f)),
                    new Vector3(10f, 3f, 10f),
                    LobbyInteractionKind.TipsCatalog);

                var skillHint = WireSkillHint(oganjdan, skillZone.transform, popUpPrefab);
                var tipsHint = CreateHint(popUpPrefab, tipsZone.transform, "Hint_Tips");

                skillZone.Configure(LobbyInteractionKind.SkillLoadout, skillHint);
                tipsZone.Configure(LobbyInteractionKind.TipsCatalog, tipsHint);

                SetScenarioZones(root, scenarioView, new[] { tipsZone, skillZone });
                ClearInteractables(root, scenarioView);

                PrefabUtility.SaveAsPrefabAsset(root, LibraryPrefabPath);
                Debug.Log("[LobbyZones] Library prefab updated with Tips and SkillLoadout interaction zones.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void RemoveLegacyZones(Transform root)
        {
            foreach (var zoneName in new[] { "Zone_Tips", "Zone_SkillLoadout" })
            {
                var existing = root.Find(zoneName);
                if (existing != null)
                    Object.DestroyImmediate(existing.gameObject);
            }
        }

        static void DisableLegacyInteractable(GameObject root)
        {
            foreach (var interactable in root.GetComponentsInChildren<OganjdanInteractable>(true))
                interactable.enabled = false;
        }

        static LobbyInteractionZoneView CreateZone(
            Transform parent,
            string zoneName,
            Vector3 worldCenter,
            Vector3 size,
            LobbyInteractionKind kind)
        {
            var zoneGo = new GameObject(zoneName, typeof(BoxCollider), typeof(LobbyInteractionZoneView));
            zoneGo.transform.SetParent(parent, false);
            zoneGo.transform.position = worldCenter;

            var collider = zoneGo.GetComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;

            var zone = zoneGo.GetComponent<LobbyInteractionZoneView>();
            zone.Configure(kind, null);
            return zone;
        }

        static LobbyFHintView WireSkillHint(Transform oganjdan, Transform zoneTransform, GameObject popUpPrefab)
        {
            if (oganjdan == null)
                return CreateHint(popUpPrefab, zoneTransform, "Hint_SkillLoadout");

            var popup = FindChild(oganjdan, "PopUp_Interactable") ?? FindChild(oganjdan, "UI_PopUp_Interactable");
            if (popup == null)
                return CreateHint(popUpPrefab, zoneTransform, "Hint_SkillLoadout");

            if (!popup.TryGetComponent<LobbyFHintView>(out var hint))
                hint = popup.gameObject.AddComponent<LobbyFHintView>();

            hint.SetVisible(false);
            return hint;
        }

        static LobbyFHintView CreateHint(GameObject popUpPrefab, Transform parent, string hintName)
        {
            var hintGo = (GameObject)PrefabUtility.InstantiatePrefab(popUpPrefab, parent);
            hintGo.name = hintName;
            hintGo.transform.localPosition = new Vector3(0f, 2f, 0f);
            hintGo.transform.localRotation = Quaternion.identity;

            if (!hintGo.TryGetComponent<LobbyFHintView>(out var hint))
                hint = hintGo.AddComponent<LobbyFHintView>();

            hint.SetVisible(false);
            return hint;
        }

        static void SetScenarioZones(GameObject root, LevelScenarioView scenarioView, LobbyInteractionZoneView[] zones)
        {
            var serializedObject = new SerializedObject(scenarioView);
            serializedObject.FindProperty("<LobbyInteractionZones>k__BackingField").arraySize = zones.Length;
            for (int i = 0; i < zones.Length; i++)
                serializedObject.FindProperty("<LobbyInteractionZones>k__BackingField").GetArrayElementAtIndex(i).objectReferenceValue = zones[i];

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(root);
        }

        static void ClearInteractables(GameObject root, LevelScenarioView scenarioView)
        {
            var serializedObject = new SerializedObject(scenarioView);
            serializedObject.FindProperty("<Interactableviews>k__BackingField").arraySize = 0;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(root);
        }

        static Transform FindChild(Transform root, string childName)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(transform => transform.name == childName);
        }
    }
}
#endif
