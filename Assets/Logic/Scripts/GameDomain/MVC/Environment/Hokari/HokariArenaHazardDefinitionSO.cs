using Logic.Scripts.GameDomain.MVC.Environment;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Hokari
{
    /// <summary>Where to place the hazard telegraph one environment turn before displacement.</summary>
    public enum HokariArenaHazardTelegraphSpawnMode
    {
        AtPlayerFeet = 0,
        OppositePlayerAcrossArena = 1,
        RandomInArena = 2,
        AtArenaCenter = 3,
    }

    /// <summary>
    /// Single environment hazard: telegraph (turn N−1) + planar pull toward telegraph anchor (turn N).
    /// Variants differ only by <see cref="TelegraphSpawn"/>; displacement target is always the telegraph position.
    /// </summary>
    [CreateAssetMenu(fileName = "HokariArenaHazardDefinition", menuName = "ScriptableObjects/Environment/Hokari Arena Hazard Definition")]
    public sealed class HokariArenaHazardDefinitionSO : ScriptableObject
    {
        [Header("Turn window (environment phase)")]
        [Min(1)] public int TurnMin = 2;
        [Min(1)] public int TurnMax = 99;

        [Header("Displacement (pull target = telegraph world position)")]
        public PlanarPushRequest Push;
        [Tooltip("When true, also pushes the Book if present.")]
        public bool ApplyToBook;
        [Min(0f)] public float DelayBeforePushSeconds;

        [Header("Telegraph position (visual comes from parent Hokari Arena Hazard Pattern)")]
        public HokariArenaHazardTelegraphSpawnMode TelegraphSpawn = HokariArenaHazardTelegraphSpawnMode.AtPlayerFeet;

        public bool MatchesTurn(int environmentTurnNumber) =>
            environmentTurnNumber >= TurnMin && environmentTurnNumber <= TurnMax;

        public PlanarPushRequest ResolvePush(Vector3 telegraphAnchorWorld)
        {
            PlanarPushRequest push = Push;
            if (push.DirectionMode == ArenaPlanarDirectionMode.RadialInToPoint
                || push.DirectionMode == ArenaPlanarDirectionMode.RadialOutFromPoint)
            {
                push.ReferenceWorldPoint = telegraphAnchorWorld;
            }
            return push;
        }

        void OnValidate()
        {
            if (TurnMax < TurnMin) TurnMax = TurnMin;
            if (Push.DistanceMeters <= 0f)
                Push = HokariArenaHazardPresets.PullTowardTelegraph4m();
        }
    }

    public static class HokariArenaHazardPresets
    {
        public static PlanarPushRequest PullTowardTelegraph4m() => new PlanarPushRequest
        {
            DistanceMeters = 4f,
            DurationSeconds = 0.45f,
            DirectionMode = ArenaPlanarDirectionMode.RadialInToPoint,
            ReferenceWorldPoint = Vector3.zero,
            MultiplyByDebuffStacks = true,
        };
    }
}
