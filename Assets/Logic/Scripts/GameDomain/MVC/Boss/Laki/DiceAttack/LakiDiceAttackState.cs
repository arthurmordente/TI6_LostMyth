namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    public static class LakiDiceAttackState
    {
        public static int PlayerDiceCount { get; private set; } = 1;
        public static int BossDiceCount { get; private set; } = 1;
        public static int PlayerFaceMin { get; private set; } = 1;
        public static int PlayerFaceMax { get; private set; } = 6;
        public static int BossFaceMin { get; private set; } = 1;
        public static int BossFaceMax { get; private set; } = 6;

        public static void Configure(
            int playerDiceCount,
            int bossDiceCount,
            int playerFaceMin,
            int playerFaceMax,
            int bossFaceMin,
            int bossFaceMax)
        {
            PlayerDiceCount = playerDiceCount < 1 ? 1 : playerDiceCount;
            BossDiceCount = bossDiceCount < 1 ? 1 : bossDiceCount;
            PlayerFaceMin = playerFaceMin < 1 ? 1 : playerFaceMin;
            BossFaceMin = bossFaceMin < 1 ? 1 : bossFaceMin;
            PlayerFaceMax = playerFaceMax < PlayerFaceMin ? PlayerFaceMin : playerFaceMax;
            BossFaceMax = bossFaceMax < BossFaceMin ? BossFaceMin : bossFaceMax;
        }

        public static void ResetDefaults()
        {
            Configure(1, 1, 1, 6, 1, 6);
        }
    }
}
