#if UNITY_EDITOR
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Editor
{
    public static class LakiAnimationClipExporter
    {
        public const string ExportPath = AnimationFinalExportPaths.LakiExport;

        public static int ExportAllFbxClips()
        {
            AnimationClipExportEditorUtility.DeleteLegacyExportFolders();
            AnimationClipExportEditorUtility.RecreateExportFolder(ExportPath);

            int exported = AnimationClipExportEditorUtility.ExportAllClipsFromFbx(
                AnimationFinalExportPaths.LakiFbx, ExportPath);

            int fixedNames = AnimationClipExportEditorUtility.FixClipObjectNamesUnder(ExportPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LakiAnimationClipExporter] Exported {exported} clips from {AnimationFinalExportPaths.LakiFbx} → {ExportPath}. Fixed names: {fixedNames}.");
            return exported;
        }

        public static AnimationClip LoadExported(string clipName) =>
            AnimationClipExportEditorUtility.LoadExportedClip(ExportPath, clipName);
    }
}
#endif
