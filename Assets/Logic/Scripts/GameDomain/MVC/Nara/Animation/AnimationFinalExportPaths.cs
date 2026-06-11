namespace Logic.Scripts.GameDomain.MVC.Nara.Animation
{
    /// <summary>
    /// Caminhos dos FBX finais e pastas de export de clips (.anim) por personagem.
    /// </summary>
    public static class AnimationFinalExportPaths
    {
        public const string FbxRoot = "Assets/Art/FBX/Characters/FinalFBXs";

        public const string ExportRoot = "Assets/ArquivosArthur/Animacoes";

        public const string ErzahlerFbx = FbxRoot + "/ErzahlerFinal.fbx";
        public const string LakiFbx = FbxRoot + "/MadameLakiAnimations.fbx";
        public const string HocariPart1Fbx = FbxRoot + "/HocariAllAnimsPart1.fbx";
        public const string HocariPart2Fbx = FbxRoot + "/HocariAllAnimsPart2.fbx";

        public const string ErzahlerExport = ExportRoot + "/Erzahler";
        public const string LakiExport = ExportRoot + "/Laki";
        public const string HocariExport = ExportRoot + "/Hocari";
        public const string HocariPhase1 = HocariExport + "/Phase1";
        public const string HocariPhase2 = HocariExport + "/Phase2";
        public const string HocariShared = HocariExport + "/Shared";

        /// <summary>Export folders legados — removidos no export final.</summary>
        public static readonly string[] LegacyExportRoots =
        {
            "Assets/Art/Animations/Erzahler/Exported",
            "Assets/Art/Animations/MadamLaki/Exported",
            "Assets/Art/Animations/Hocari/Exported",
        };
    }
}
