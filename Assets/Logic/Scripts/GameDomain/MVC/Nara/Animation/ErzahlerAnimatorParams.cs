namespace Logic.Scripts.GameDomain.MVC.Nara.Animation
{
    /// <summary>Shared Animator parameter names for Erzahler player controllers (solo + combined).</summary>
    public static class ErzahlerAnimatorParams
    {
        public const string Moving = "Moving";
        public const string Running = "Running";
        public const string WalkVariant = "WalkVariant";
        public const string IdleVariant = "IdleVariant";
        public const string ConjuringFast = "ConjuringFast";
        public const string ConjuringPrep = "ConjuringPrep";
        public const string ConjuringLoop = "ConjuringLoop";
        public const string ConjuringFinish = "ConjuringFinish";
        public const string ConjuringCancel = "ConjuringCancel";
        public const string ConjuringFail = "ConjuringFail";
        public const string Dead = "Dead";
        public const string Hit = "Hit";
        public const string BetWon = "BetWon";
        public const string BetLost = "BetLost";
        public const string DivideDeploy = "DivideDeploy";
        public const string DivideRecall = "DivideRecall";

        public const string TagIdle = "Idle";
        public const string TagLocomotion = "Locomotion";
        public const string TagConjuringLoop = "ConjuringLoop";
        public const string TagDeath = "Death";
    }

    /// <summary>Animator parameters for the deployed Book clone unit.</summary>
    public static class BookAnimatorParams
    {
        public const string Moving = "Moving";
        public const string IdleVariant = "IdleVariant";
        public const string WalkVariant = "WalkVariant";
        public const string Ability = "Ability";

        public const string TagIdle = "Idle";
        public const string TagLocomotion = "Locomotion";
        public const string TagAbility = "Ability";
    }

    /// <summary>Animator parameters for Madam Laki boss.</summary>
    public static class LakiAnimatorParams
    {
        public const string PerformanceId = "PerformanceId";
        public const string PerformancePrep = "PerformancePrep";
        public const string PerformanceLoop = "PerformanceLoop";
        public const string PerformanceFinish = "PerformanceFinish";
        public const string Ability = "Ability";
        public const string Spotlight = "Spotlight";
        public const string HitReaction = "HitReaction";
        public const string BetWon = "BetWon";
        public const string BetLost = "BetLost";
        public const string Death = "Death";
        public const string ThrowDiePrep = "ThrowDiePrep";
        public const string ThrowDieLoop = "ThrowDieLoop";
        public const string ThrowDieFinish = "ThrowDieFinish";

        public const string TagIdle = "Idle";
        public const string TagDeath = "Death";
        public const string TagThrowDieLoop = "ThrowDieLoop";
        public const string TagPerformanceLoop = "PerformanceLoop";
        public const string TagPerformancePrep = "PerformancePrep";
        public const string TagAbility = "Ability";
    }
}
