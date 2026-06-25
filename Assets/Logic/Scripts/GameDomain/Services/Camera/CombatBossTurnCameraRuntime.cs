using System;

namespace Logic.Scripts.GameDomain.Services.Camera
{
    /// <summary>
    /// Boss turn camera milestones: resolve keeps focus on the player; prepare shifts to the boss.
    /// </summary>
    public static class CombatBossTurnCameraRuntime
    {
        public static event Action OnBossAttackResolveStarted;
        public static event Action OnBossAttackResolveCompleted;
        public static event Action OnBossPrepareStarted;

        public static void NotifyBossAttackResolveStarted()
        {
            try { OnBossAttackResolveStarted?.Invoke(); } catch { }
        }

        public static void NotifyBossAttackResolveCompleted()
        {
            try { OnBossAttackResolveCompleted?.Invoke(); } catch { }
        }

        public static void NotifyBossPrepareStarted()
        {
            try { OnBossPrepareStarted?.Invoke(); } catch { }
        }
    }
}
