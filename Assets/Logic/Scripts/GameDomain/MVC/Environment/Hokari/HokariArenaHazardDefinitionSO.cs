using Logic.Scripts.GameDomain.MVC.Boss.Visuals;
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
    /// Single environment hazard: telegraph (turn N−1) + planar displacement toward/away from telegraph anchor (turn N).
    /// </summary>
    [CreateAssetMenu(fileName = "HokariArenaHazardDefinition", menuName = "ScriptableObjects/Environment/Hokari Arena Hazard Definition")]
    public sealed class HokariArenaHazardDefinitionSO : ScriptableObject
    {
        [Header("Turn window (environment phase)")]
        [Min(1)] public int TurnMin = 2;
        [Min(1)] public int TurnMax = 99;

        [Header("Displacement")]
        public HokariArenaHazardDisplacementKind DisplacementKind = HokariArenaHazardDisplacementKind.PullTowardTelegraph;
        public PlanarPushRequest Push;
        [Tooltip("When true, also pushes the Book if present.")]
        public bool ApplyToBook;
        [Min(0f)] public float DelayBeforePushSeconds;

        [Header("Telegraph (matched to displacement via catalog)")]
        public HokariArenaHazardCatalogTelegraph CatalogTelegraph;
        [Tooltip("Catalog disc XZ scale (arena mesh at 1 ≈ this radius in meters).")]
        [Min(0.1f)] public float TelegraphDiscRadius = 3.5f;
        public HokariArenaHazardTelegraphSpawnMode TelegraphSpawn = HokariArenaHazardTelegraphSpawnMode.AtPlayerFeet;

        public bool MatchesTurn(int environmentTurnNumber) =>
            environmentTurnNumber >= TurnMin && environmentTurnNumber <= TurnMax;

        public bool IsPull => DisplacementKind == HokariArenaHazardDisplacementKind.PullTowardTelegraph;

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

        public void SyncCatalogAndPushFromDisplacementKind()
        {
            CatalogTelegraph.AlignToDisplacementKind(DisplacementKind);

            bool directionOk = IsPull
                ? Push.DirectionMode == ArenaPlanarDirectionMode.RadialInToPoint
                : Push.DirectionMode == ArenaPlanarDirectionMode.RadialOutFromPoint;

            if (Push.DistanceMeters <= 0f || !directionOk)
                Push = HokariArenaHazardPresets.ForKind(DisplacementKind, Push.DistanceMeters, Push.DurationSeconds);
        }

        void OnValidate()
        {
            if (TurnMax < TurnMin) TurnMax = TurnMin;
            SyncCatalogAndPushFromDisplacementKind();
        }
    }

    public static class HokariArenaHazardPresets
    {
        public static PlanarPushRequest ForKind(
            HokariArenaHazardDisplacementKind kind,
            float distanceMeters = 4f,
            float durationSeconds = 0.45f) => kind switch
        {
            HokariArenaHazardDisplacementKind.PushAwayFromTelegraph => PushAwayFromTelegraph4m(distanceMeters, durationSeconds),
            _ => PullTowardTelegraph4m(distanceMeters, durationSeconds),
        };

        public static PlanarPushRequest PullTowardTelegraph4m(float distanceMeters = 4f, float durationSeconds = 0.45f) =>
            new PlanarPushRequest
            {
                DistanceMeters = distanceMeters,
                DurationSeconds = durationSeconds,
                DirectionMode = ArenaPlanarDirectionMode.RadialInToPoint,
                ReferenceWorldPoint = Vector3.zero,
                MultiplyByDebuffStacks = true,
            };

        public static PlanarPushRequest PushAwayFromTelegraph4m(float distanceMeters = 4f, float durationSeconds = 0.45f) =>
            new PlanarPushRequest
            {
                DistanceMeters = distanceMeters,
                DurationSeconds = durationSeconds,
                DirectionMode = ArenaPlanarDirectionMode.RadialOutFromPoint,
                ReferenceWorldPoint = Vector3.zero,
                MultiplyByDebuffStacks = true,
            };
    }
}
