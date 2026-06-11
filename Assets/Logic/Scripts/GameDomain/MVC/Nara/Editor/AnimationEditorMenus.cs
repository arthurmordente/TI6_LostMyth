#if UNITY_EDITOR
using Logic.Scripts.GameDomain.MVC.Boss.Hocari.Editor;
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Editor
{
    /// <summary>
    /// Entrada única para export de FBX finais e rebuild de animator controllers.
    /// Clips finais: Assets/ArquivosArthur/Animacoes/{Erzahler,Laki,Hocari}
    /// </summary>
    public static class AnimationEditorMenus
    {
        private const string ExportMenu = "TI6/Animation/1 Export FBX Clips/";
        private const string BuildMenu = "TI6/Animation/2 Build State Machines/";

        [MenuItem(ExportMenu + "All Final (Erzahler + Laki + Hocari)", false, 0)]
        public static void ExportAllFinalFbx()
        {
            int erz = ErzahlerErzaClipExporter.ExportAllFbxClips();
            int laki = LakiAnimationClipExporter.ExportAllFbxClips();
            HocariAnimationClipExporter.ExportAllFbxClips();

            ErzahlerAnimatorControllerBuilder.BuildErzahlerStateMachines();
            ErzahlerAnimatorControllerBuilder.BuildLakiBossOnly();
            HocariAnimatorControllerBuilder.BuildUnifiedOnly();

            Debug.Log($"[Animation] All final: Erzahler={erz}, Laki={laki} clips → {AnimationFinalExportPaths.ExportRoot}. Controllers rebuilt.");
        }

        [MenuItem(ExportMenu + "Erzahler", false, 1)]
        public static void ExportErzahlerFbx()
        {
            int count = ErzahlerErzaClipExporter.ExportAllFbxClips();
            ErzahlerAnimatorControllerBuilder.BuildErzahlerStateMachines();
            Debug.Log($"[Animation] Erzahler: {count} clips → {ErzahlerErzaClipExporter.ExportPath}. Controllers rebuilt.");
        }

        [MenuItem(ExportMenu + "Erzahler — Book Idle (ErzahlerBookIdle.fbx)", false, 2)]
        public static void ExportErzahlerBookIdleFbx()
        {
            if (!ErzahlerErzaClipExporter.ExportBookIdleClip())
                return;

            ErzahlerAnimatorControllerBuilder.BuildErzahlerWithBookOnly();
            Debug.Log($"[Animation] Erzahler book idle → {ErzahlerErzaClipExporter.ExportPath}/{AnimationFinalExportPaths.ErzahlerBookIdleClipName}.anim. With-book controller rebuilt.");
        }

        [MenuItem(ExportMenu + "Laki", false, 3)]
        public static void ExportLakiFbx()
        {
            int count = LakiAnimationClipExporter.ExportAllFbxClips();
            ErzahlerAnimatorControllerBuilder.BuildLakiBossOnly();
            Debug.Log($"[Animation] Laki: {count} clips → {LakiAnimationClipExporter.ExportPath}. Controller rebuilt.");
        }

        [MenuItem(ExportMenu + "Hocari", false, 4)]
        public static void ExportHocariFbx()
        {
            HocariAnimationClipExporter.ExportAllFbxClips();
            HocariAnimatorControllerBuilder.BuildUnifiedOnly();
            Debug.Log($"[Animation] Hocari: clips → {HocariAnimationClipExporter.RootPath}. Controller rebuilt.");
        }

        [MenuItem(ExportMenu + "Fix all clip names (Armature|...)", false, 20)]
        public static void FixAllExportedClipNames()
        {
            int erz = AnimationClipExportEditorUtility.FixClipObjectNamesUnder(AnimationFinalExportPaths.ErzahlerExport);
            int laki = AnimationClipExportEditorUtility.FixClipObjectNamesUnder(AnimationFinalExportPaths.LakiExport);
            int hoc = HocariAnimationClipExporter.FixExportedClipObjectNames();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Animation] Fixed clip names: Erzahler={erz}, Laki={laki}, Hocari={hoc}.");
        }

        [MenuItem(BuildMenu + "All (Erzahler + Laki + Hocari)", false, 10)]
        public static void BuildAllStateMachines()
        {
            ErzahlerAnimatorControllerBuilder.BuildErzahlerStateMachines();
            ErzahlerAnimatorControllerBuilder.BuildLakiBossOnly();
            HocariAnimatorControllerBuilder.BuildUnifiedOnly();
            Debug.Log("[Animation] Built all state machines from ArquivosArthur/Animacoes exports.");
        }

        [MenuItem(BuildMenu + "Erzahler", false, 11)]
        public static void BuildErzahlerStateMachines()
        {
            ErzahlerAnimatorControllerBuilder.BuildErzahlerStateMachines();
        }

        [MenuItem(BuildMenu + "Laki", false, 12)]
        public static void BuildLakiStateMachine()
        {
            ErzahlerAnimatorControllerBuilder.BuildLakiBossOnly();
        }

        [MenuItem(BuildMenu + "Hocari", false, 13)]
        public static void BuildHocariStateMachine()
        {
            HocariAnimatorControllerBuilder.BuildUnifiedOnly();
        }

        [MenuItem(BuildMenu + "Hocari — Assign HOC_Hocari_FINAL to HokariBoss prefab", false, 14)]
        public static void AssignHocariPrefab()
        {
            HocariAnimatorControllerBuilder.AssignToHokariPrefab();
        }
    }
}
#endif
