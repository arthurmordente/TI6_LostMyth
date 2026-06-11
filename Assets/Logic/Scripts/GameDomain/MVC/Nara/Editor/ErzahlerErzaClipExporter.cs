#if UNITY_EDITOR
using Logic.Scripts.GameDomain.MVC.Nara.Animation;
using UnityEditor;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Editor
{
    public static class ErzahlerErzaClipExporter
    {
        public const string ExportPath = AnimationFinalExportPaths.ErzahlerExport;

        public static int ExportAllFbxClips()
        {
            AnimationClipExportEditorUtility.DeleteLegacyExportFolders();
            AnimationClipExportEditorUtility.RecreateExportFolder(ExportPath);

            int exported = AnimationClipExportEditorUtility.ExportAllClipsFromFbx(
                AnimationFinalExportPaths.ErzahlerFbx, ExportPath);

            if (ExportBookIdleClip(logSuccess: false))
                exported++;

            int fixedNames = AnimationClipExportEditorUtility.FixClipObjectNamesUnder(ExportPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ErzahlerErzaClipExporter] Exported {exported} clips from {AnimationFinalExportPaths.ErzahlerFbx} (+ book idle) → {ExportPath}. Fixed names: {fixedNames}.");
            return exported;
        }

        /// <summary>
        /// Exports <see cref="AnimationFinalExportPaths.ErzahlerBookIdleClipName"/> from ErzahlerBookIdle.fbx
        /// into the Erzahler export folder (does not wipe other clips).
        /// </summary>
        public static bool ExportBookIdleClip(bool logSuccess = true)
        {
            bool ok = AnimationClipExportEditorUtility.ExportSingleClipFromFbx(
                AnimationFinalExportPaths.ErzahlerBookIdleFbx,
                ExportPath,
                AnimationFinalExportPaths.ErzahlerBookIdleClipName);

            if (!ok)
            {
                Debug.LogError($"[ErzahlerErzaClipExporter] Failed to export {AnimationFinalExportPaths.ErzahlerBookIdleClipName} from {AnimationFinalExportPaths.ErzahlerBookIdleFbx}.");
                return false;
            }

            AnimationClipExportEditorUtility.FixClipObjectNamesUnder(ExportPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (logSuccess)
                Debug.Log($"[ErzahlerErzaClipExporter] Exported {AnimationFinalExportPaths.ErzahlerBookIdleClipName} → {ExportPath}/{AnimationFinalExportPaths.ErzahlerBookIdleClipName}.anim");

            return true;
        }

        public static AnimationClip LoadExported(string clipName) =>
            AnimationClipExportEditorUtility.LoadExportedClip(ExportPath, clipName);
    }
}
#endif
