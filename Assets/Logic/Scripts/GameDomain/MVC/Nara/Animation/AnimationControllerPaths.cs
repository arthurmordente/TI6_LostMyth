namespace Logic.Scripts.GameDomain.MVC.Nara.Animation
{
    /// <summary>
    /// Caminhos dos animator controllers gerados pelo pipeline FINAL (TI6 → Animation).
    /// Legado sem sufixo _FINAL (ex. 1HOC_Hocari, HOC_AnimatorController) não usar em gameplay novo.
    /// </summary>
    public static class AnimationControllerPaths
    {
        // Erzahler — 3 controllers (livro no player, solo com clone, clone book)
        public const string ErzahlerBook = "Assets/Art/Animations/erz+book/ERZ_ErzahlerBook_FINAL.controller";
        public const string ErzahlerSolo = "Assets/Art/Animations/Erzahler/ERZ_Erzahler_FINAL.controller";
        public const string BookClone = "Assets/Art/Animations/Book/ERZ_Book_FINAL.controller";

        // Laki boss
        public const string LakiBoss = "Assets/Art/Animations/MadamLaki/LKI_Animator_FINAL.controller";

        // Hocari boss (substitui 1HOC_Hocari / HOC_AnimatorController)
        public const string HocariBoss = "Assets/Art/Animations/Hocari/HOC_Hocari_FINAL.controller";
    }
}
