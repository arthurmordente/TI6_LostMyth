#if UNITY_EDITOR
using Logic.Scripts.GameDomain.MVC.Boss.Hocari.Editor;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Editor
{
    /// <summary>
    /// Entrada única para export de FBX e rebuild de animator controllers.
    /// Ver AnimationInventory_UnwiredClips.md para clips sem hook de gameplay.
    /// </summary>
    public static class AnimationEditorMenus
    {
        private const string ExportMenu = "TI6/Animation/1 Export FBX Clips/";
        private const string BuildMenu = "TI6/Animation/2 Build State Machines/";

        [MenuItem(ExportMenu + "Erzahler", false, 1)]
        public static void ExportErzahlerFbx()
        {
            int count = ErzahlerErzaClipExporter.ExportAllFbxClips();
            Debug.Log($"[Animation] Erzahler: exported {count} clips from ErzahlerFinal.fbx → {ErzahlerErzaClipExporter.ExportPath}");
        }

        [MenuItem(ExportMenu + "Laki", false, 2)]
        public static void ExportLakiFbx()
        {
            int count = LakiAnimationClipExporter.ExportAllFbxClips();
            Debug.Log($"[Animation] Laki: exported {count} clips from MadameLakiAnimations.fbx → {LakiAnimationClipExporter.ExportPath}");
        }

        [MenuItem(ExportMenu + "Hocari", false, 3)]
        public static void ExportHocariFbx()
        {
            HocariAnimationClipExporter.ExportAllFbxClips();
        }

        [MenuItem(ExportMenu + "Hocari — Fix clip names (Armature|...)", false, 4)]
        public static void FixHocariExportedClipNames()
        {
            int count = HocariAnimationClipExporter.FixExportedClipObjectNames();
            Debug.Log($"[Animation] Hocari: fixed {count} exported clip name(s) under {HocariAnimationClipExporter.RootPath}.");
        }

        [MenuItem(BuildMenu + "All (Erzahler + Laki + Hocari)", false, 10)]
        public static void BuildAllStateMachines()
        {
            ErzahlerAnimatorControllerBuilder.BuildErzahlerStateMachines();
            ErzahlerAnimatorControllerBuilder.BuildLakiBossOnly();
            HocariAnimatorControllerBuilder.BuildUnifiedOnly();
            Debug.Log("[Animation] Built all state machines (Erzahler, Laki, Hocari). Optional states depend on exported clips.");
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
