using Logic.Scripts.Turns;

namespace Logic.Scripts.GameDomain.MVC.Book
{
    /// <summary>
    /// Standalone Action Points for the Book unit.
    /// Does not publish to TurnStateService (that slot is reserved for Nara's display).
    /// </summary>
    public class BookActionPoints : IActionPointsService
    {
        private int _current;
        private int _max;
        private int _gainPerTurn;

        public int Current => _current;
        public int Max => _max;
        public int GainPerTurn => _gainPerTurn;

        public BookActionPoints(int max, int gainPerTurn)
        {
            _max = max;
            _gainPerTurn = gainPerTurn;
            _current = 0;
        }

        public void Configure(int max, int gainPerTurn)
        {
            _max = max < 0 ? 0 : max;
            _gainPerTurn = gainPerTurn < 0 ? 0 : gainPerTurn;
            if (_current > _max) _current = _max;
        }

        public bool CanSpend(int amount)
        {
            if (amount <= 0) return true;
            return _current >= amount;
        }

        public bool Spend(int amount)
        {
            if (amount <= 0) return true;
            if (_current < amount) return false;
            _current -= amount;
            return true;
        }

        public void GainTurnPoints()
        {
            _current += _gainPerTurn;
            if (_current > _max) _current = _max;
        }

        public void Refill()
        {
            _current = _max;
        }

        public void Reset()
        {
            _current = 2;
        }

        public void Add(int amount)
        {
            if (amount <= 0) return;
            _current += amount;
            if (_current > _max) _current = _max;
        }

        public void Subtract(int amount)
        {
            if (amount <= 0) return;
            _current -= amount;
            if (_current < 0) _current = 0;
        }
    }
}
