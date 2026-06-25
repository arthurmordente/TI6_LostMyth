using System;
using System.Collections.Generic;

namespace Logic.Scripts.Turns
{
    public interface ITurnQuery
    {
        bool Active { get; }
        int TurnNumber { get; }
        TurnPhase Phase { get; }
        int ActionPointsCurrent { get; }
        int ActionPointsMax { get; }
        System.Collections.Generic.List<BarrierState> BarrierStates { get; }
        System.Collections.Generic.List<HazardZoneState> HazardZones { get; }
    }

    public interface ITurnStateReader
    {
        bool Active { get; }
        int TurnNumber { get; }
        TurnPhase Phase { get; }
        int ActionPointsCurrent { get; }
        int ActionPointsMax { get; }
    }

    public class TurnStateService : ITurnStateReader, ITurnQuery
    {
        public event Action<int, TurnPhase> OnPhaseChanged;

        public bool Active { get; private set; }
        public int TurnNumber { get; private set; }
        public TurnPhase Phase { get; private set; }
        public int ActionPointsCurrent { get; private set; }
        public int ActionPointsMax { get; private set; }

        private System.Collections.Generic.List<BarrierState> _barriers = new System.Collections.Generic.List<BarrierState>();
        private System.Collections.Generic.List<HazardZoneState> _hazardZones = new System.Collections.Generic.List<HazardZoneState>();

        public System.Collections.Generic.List<BarrierState> BarrierStates => _barriers;
        public System.Collections.Generic.List<HazardZoneState> HazardZones => _hazardZones;

        public void EnterTurnMode()
        {
            Active = true;
            TurnNumber = 0;
            Phase = TurnPhase.None;
        }

        public void ExitTurnMode()
        {
            Active = false;
            Phase = TurnPhase.None;
        }

        public void AdvanceTurn(int turnNumber, TurnPhase phase)
        {
            TurnNumber = turnNumber;
            Phase = phase;
            try { OnPhaseChanged?.Invoke(turnNumber, phase); } catch { }
        }

        public void UpdateActionPoints(int current, int max)
        {
            ActionPointsCurrent = current;
            ActionPointsMax = max;
        }

        public void PublishBarrierStateChanged(List<BarrierState> barriers)
        {
            _barriers = barriers ?? new List<BarrierState>();
        }

        public void PublishHazardZonesChanged(List<HazardZoneState> zones)
        {
            _hazardZones = zones ?? new List<HazardZoneState>();
        }

        public void RequestPlayerAction()
        {
        }
    }
}


