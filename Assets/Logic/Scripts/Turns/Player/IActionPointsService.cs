namespace Logic.Scripts.Turns
{
    public interface IActionPointsService
    {
        int Current { get; }
        int Max { get; }
        int GainPerTurn { get; }
        bool CanSpend(int amount);
        bool Spend(int amount);
        void GainTurnPoints();
        void Refill();
        void Reset();
        void Configure(int max, int gainPerTurn);
			void Add(int amount);
			void Subtract(int amount);
        void GrantTemporaryGainPerTurnBonus(int bonus, int playerTurnsRemaining);
        void ConsumeTemporaryGainTurn();
        int TemporaryGainPerTurnBonus { get; }
    }
}
