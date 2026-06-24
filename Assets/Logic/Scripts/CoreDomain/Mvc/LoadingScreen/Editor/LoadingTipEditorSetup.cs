#if UNITY_EDITOR
using System.IO;
using Logic.Scripts.Core.Mvc.LoadingScreen;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.Core.Mvc.LoadingScreen.Editor
{
    public static class LoadingTipEditorSetup
    {
        const string TipPrefabPath = "Assets/GameDesign/Prefabs/Ui/LoadingTips/LoadingTip_Template.prefab";
        const string PoolAssetPath = "Assets/GameDesign/GameData/Loading/LoadingTipPool.asset";
        const string CoreLoadingScreenPath = "Assets/GameDesign/Prefabs/Ui/CoreLoadingScreen.prefab";
        const string CoreScenePath = "Assets/GameDesign/Scenes/CoreScene.unity";
        const string TmpFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        [InitializeOnLoadMethod]
        static void EnsureAssetsOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(TipPrefabPath) || !File.Exists(PoolAssetPath))
                    CreateAllAssets(silent: true, regenerateTemplate: false);
            };
        }

        [MenuItem("TI6/Loading Screen/Create Tip Template Assets")]
        public static void CreateAllAssetsMenu() => CreateAllAssets(silent: false, regenerateTemplate: false);

        [MenuItem("TI6/Loading Screen/Regenerate Tip Template")]
        public static void RegenerateTipTemplateMenu()
        {
            EnsureDirectory(Path.GetDirectoryName(TipPrefabPath));
            var tipPrefab = CreateTipTemplatePrefab();
            var pool = AssetDatabase.LoadAssetAtPath<LoadingTipPoolSO>(PoolAssetPath);
            if (pool != null)
                AssignTipToPool(pool, tipPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = tipPrefab;
            EditorGUIUtility.PingObject(tipPrefab);
            Debug.Log("[LoadingScreen] Tip template regenerated with modular TipContent layout.");
        }

        public static void CreateAllAssets(bool silent, bool regenerateTemplate)
        {
            EnsureDirectory(Path.GetDirectoryName(TipPrefabPath));
            EnsureDirectory(Path.GetDirectoryName(PoolAssetPath));

            var tipPrefab = AssetDatabase.LoadAssetAtPath<LoadingTipCanvasView>(TipPrefabPath);
            if (tipPrefab == null || regenerateTemplate)
                tipPrefab = CreateTipTemplatePrefab();

            var pool = AssetDatabase.LoadAssetAtPath<LoadingTipPoolSO>(PoolAssetPath);
            if (pool == null)
            {
                pool = ScriptableObject.CreateInstance<LoadingTipPoolSO>();
                AssetDatabase.CreateAsset(pool, PoolAssetPath);
            }

            AssignTipToPool(pool, tipPrefab);
            UpdateCoreLoadingScreenPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!silent)
            {
                WireCoreSceneReferences(pool);
                Selection.activeObject = tipPrefab;
                EditorGUIUtility.PingObject(tipPrefab);
                Debug.Log("[LoadingScreen] Tip template, pool asset and CoreLoadingScreen host are ready. CoreScene wired if it was openable.");
            }
        }

        static LoadingTipCanvasView CreateTipTemplatePrefab()
        {
            var root = new GameObject("LoadingTip_Template", typeof(RectTransform));
            StretchFull(root.GetComponent<RectTransform>());

            var panel = CreateUiObject("Panel", root.transform, typeof(Image));
            StretchFull(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

            var tipContent = CreateUiObject("TipContent", panel.transform, typeof(VerticalLayoutGroup));
            var tipContentRect = tipContent.GetComponent<RectTransform>();
            tipContentRect.anchorMin = new Vector2(0.08f, 0.28f);
            tipContentRect.anchorMax = new Vector2(0.92f, 0.88f);
            tipContentRect.offsetMin = Vector2.zero;
            tipContentRect.offsetMax = Vector2.zero;
            var tipLayout = tipContent.GetComponent<VerticalLayoutGroup>();
            tipLayout.childAlignment = TextAnchor.MiddleCenter;
            tipLayout.spacing = 20f;
            tipLayout.padding = new RectOffset(16, 16, 16, 16);
            tipLayout.childControlWidth = true;
            tipLayout.childControlHeight = true;
            tipLayout.childForceExpandWidth = true;
            tipLayout.childForceExpandHeight = false;

            var icon = CreateUiObject("img_Icon", tipContent.transform, typeof(Image));
            var iconRect = icon.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(128f, 128f);
            var iconImage = icon.GetComponent<Image>();
            iconImage.color = new Color(1f, 1f, 1f, 0.35f);
            iconImage.raycastTarget = false;

            var tipText = CreateTmpText("txt_Tip", tipContent.transform,
                "Dica: use as skills do Livro para combos.");
            tipText.fontSize = 32f;
            tipText.alignment = TextAlignmentOptions.Center;

            var continuePrompt = CreateUiObject("ContinuePrompt", panel.transform);
            continuePrompt.SetActive(false);
            var continueRect = continuePrompt.GetComponent<RectTransform>();
            continueRect.anchorMin = new Vector2(0.08f, 0.06f);
            continueRect.anchorMax = new Vector2(0.92f, 0.22f);
            continueRect.offsetMin = Vector2.zero;
            continueRect.offsetMax = Vector2.zero;

            var continueContent = CreateUiObject("ContinueContent", continuePrompt.transform, typeof(HorizontalLayoutGroup));
            StretchFull(continueContent.GetComponent<RectTransform>());
            var continueLayout = continueContent.GetComponent<HorizontalLayoutGroup>();
            continueLayout.childAlignment = TextAnchor.MiddleCenter;
            continueLayout.spacing = 12f;
            continueLayout.childControlWidth = true;
            continueLayout.childControlHeight = true;
            continueLayout.childForceExpandWidth = false;
            continueLayout.childForceExpandHeight = true;

            var continueText = CreateTmpText("txt_Continue", continueContent.transform,
                "Pressione qualquer tecla para continuar");
            continueText.fontSize = 24f;
            continueText.alignment = TextAlignmentOptions.Center;
            continueText.fontStyle = FontStyles.Italic;

            var tipView = root.AddComponent<LoadingTipCanvasView>();
            var serializedTip = new SerializedObject(tipView);
            serializedTip.FindProperty("_panelRoot").objectReferenceValue = panel;
            serializedTip.FindProperty("_tipContentRoot").objectReferenceValue = tipContent;
            serializedTip.FindProperty("_continuePrompt").objectReferenceValue = continuePrompt;
            serializedTip.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, TipPrefabPath);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<LoadingTipCanvasView>();
        }

        static void AssignTipToPool(LoadingTipPoolSO pool, LoadingTipCanvasView tipPrefab)
        {
            var serializedPool = new SerializedObject(pool);
            var list = serializedPool.FindProperty("_tipPrefabs");
            list.ClearArray();
            list.InsertArrayElementAtIndex(0);
            list.GetArrayElementAtIndex(0).objectReferenceValue = tipPrefab;
            serializedPool.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pool);
        }

        static void UpdateCoreLoadingScreenPrefab()
        {
            var prefabRoot = PrefabUtility.LoadPrefabContents(CoreLoadingScreenPath);
            try
            {
                var canvas = prefabRoot.GetComponent<Canvas>();
                if (canvas == null)
                    canvas = prefabRoot.AddComponent<Canvas>();

                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;

                if (prefabRoot.GetComponent<CanvasScaler>() == null)
                {
                    var scaler = prefabRoot.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    scaler.matchWidthOrHeight = 1f;
                }

                if (prefabRoot.GetComponent<GraphicRaycaster>() == null)
                    prefabRoot.AddComponent<GraphicRaycaster>();

                var canvasGroup = prefabRoot.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = prefabRoot.AddComponent<CanvasGroup>();

                var tipContainer = prefabRoot.transform.Find("TipContainer");
                if (tipContainer == null)
                {
                    var containerGo = new GameObject("TipContainer", typeof(RectTransform));
                    containerGo.transform.SetParent(prefabRoot.transform, false);
                    StretchFull(containerGo.GetComponent<RectTransform>());
                    tipContainer = containerGo.transform;
                }

                var loadingView = prefabRoot.GetComponent<LoadingScreenCanvasView>();
                if (loadingView == null)
                    loadingView = prefabRoot.AddComponent<LoadingScreenCanvasView>();

                var serializedView = new SerializedObject(loadingView);
                serializedView.FindProperty("_tipContainer").objectReferenceValue = tipContainer;
                serializedView.FindProperty("_rootPanel").objectReferenceValue = prefabRoot;
                serializedView.FindProperty("_canvasGroup").objectReferenceValue = canvasGroup;
                serializedView.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, CoreLoadingScreenPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        static void WireCoreSceneReferences(LoadingTipPoolSO pool)
        {
            if (!File.Exists(CoreScenePath))
                return;

            var coreScene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
            var installers = Object.FindObjectsByType<CoreInstaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < installers.Length; i++)
            {
                var serializedInstaller = new SerializedObject(installers[i]);
                serializedInstaller.FindProperty("_loadingTipPool").objectReferenceValue = pool;

                var loadingView = serializedInstaller.FindProperty("_loadingScreenView").objectReferenceValue as LoadingScreenCanvasView;
                if (loadingView == null)
                {
                    var viewInScene = Object.FindObjectsByType<LoadingScreenCanvasView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    if (viewInScene.Length > 0)
                        serializedInstaller.FindProperty("_loadingScreenView").objectReferenceValue = viewInScene[0];
                }

                serializedInstaller.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(installers[i]);
            }

            EditorSceneManager.SaveScene(coreScene);
        }

        static GameObject CreateUiObject(string name, Transform parent, System.Type extraComponent = null)
        {
            var components = extraComponent != null
                ? new[] { typeof(RectTransform), extraComponent }
                : new[] { typeof(RectTransform) };
            var go = new GameObject(name, components);
            go.transform.SetParent(parent, false);
            return go;
        }

        static TextMeshProUGUI CreateTmpText(string name, Transform parent, string text)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = Color.white;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            if (font != null)
                tmp.font = font;
            return tmp;
        }

        static void StretchFull(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }

        static void EnsureDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
        }
    }
}
#endif
