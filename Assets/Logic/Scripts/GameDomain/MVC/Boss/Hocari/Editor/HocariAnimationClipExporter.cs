#if UNITY_EDITOR
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using Logic.Scripts.GameDomain.MVC.Nara.Editor;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Hocari.Editor
{
    public static class HocariAnimationClipExporter
    {
        public const string RootPath = AnimationFinalExportPaths.HocariExport;
        public const string Phase1Path = AnimationFinalExportPaths.HocariPhase1;
        public const string Phase2Path = AnimationFinalExportPaths.HocariPhase2;
        public const string SharedPath = AnimationFinalExportPaths.HocariShared;

        private static readonly string[] Phase1FromPart2 =
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
            "Hocari_Attack_SwordLines_Prep",
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

        private static readonly string[] Phase2FromPart1 =
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
            "Hocari_Phase2_Attack_WingSlash_Right_Prep",
            "Hocari_Phase2_Attack_WingSlash_Right_Loop",
            "Hocari_Phase2_Attack_WingSlash_Right_Finish",
            "Hocari_Phase2_Hit",
            "Hocari_Phase2_Death",
        };

        private static readonly string[] SharedFromPart1 =
        {
            "Hocari_PhaseTransition_Prep",
            "Hocari_PhaseTransition_Loop",
            "Hocari_PhaseTransition_Finish",
            "Hocari_PhaseTransition_Finish_2",
        };

        private static readonly string[] SharedFromPart2 =
        {
            "Hocari_Movement_Prep",
            "Hocari_Movement_Loop",
            "Hocari_Movement_Loop_2",
            "Hocari_Movement_Loop_3",
            "Hocari_Movement_Finish",
        };

        public static void ExportAllFbxClips()
        {
            AnimationClipExportEditorUtility.DeleteLegacyExportFolders();
            AnimationClipExportEditorUtility.RecreateExportFolder(RootPath);

            int gameplay = ExportGameplayClips();
            int fixedNames = FixExportedClipObjectNames();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HocariAnimationClipExporter] Gameplay export: {gameplay} clips → {RootPath}. Fixed names: {fixedNames}.");
        }

        public static int ExportGameplayClips()
        {
            var index = AnimationClipExportEditorUtility.BuildFbxClipIndex(
                AnimationFinalExportPaths.HocariPart1Fbx,
                AnimationFinalExportPaths.HocariPart2Fbx);

            int exported = 0;
            int missing = 0;

            exported += AnimationClipExportEditorUtility.ExportClipList(
                index, Phase1FromPart2, AnimationFinalExportPaths.HocariPart2Fbx, Phase1Path, ref missing);
            exported += AnimationClipExportEditorUtility.ExportClipList(
                index, Phase2FromPart1, AnimationFinalExportPaths.HocariPart1Fbx, Phase2Path, ref missing);
            exported += AnimationClipExportEditorUtility.ExportClipList(
                index, SharedFromPart1, AnimationFinalExportPaths.HocariPart1Fbx, SharedPath, ref missing);
            exported += AnimationClipExportEditorUtility.ExportClipList(
                index, SharedFromPart2, AnimationFinalExportPaths.HocariPart2Fbx, SharedPath, ref missing);

            if (missing > 0)
                Debug.LogWarning($"[HocariAnimationClipExporter] Gameplay export missing {missing} expected clips.");

            return exported;
        }

        public static AnimationClip LoadClip(string folder, string clipName) =>
            AnimationClipExportEditorUtility.LoadExportedClip($"{RootPath}/{folder}", clipName);

        public static int FixExportedClipObjectNames() =>
            AnimationClipExportEditorUtility.FixClipObjectNamesUnder(RootPath);
    }
}
#endif
