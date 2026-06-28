using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.GameDomain.VisualFeedback;
using Zenject;

namespace Logic.Scripts.Turns
{
    public class ActionPointsService : IActionPointsService
    {
        private readonly TurnStateService _turnStateService;
        private IGamePlayUiController _gamePlayUiController;

        private int _current;
        private int _max;
        private int _gainPerTurn;
        private int _tempBonus;
        private int _tempTurnsRemaining;
        private bool _skipConsumeThisTurnEnd;

        public int Current => _current;
        public int Max => _max;
        public int GainPerTurn => _gainPerTurn;
        public int TemporaryGainPerTurnBonus => _tempTurnsRemaining > 0 ? _tempBonus : 0;

        public ActionPointsService(TurnStateService turnStateService)
        {
            _turnStateService = turnStateService;
            _max = 10;
            _gainPerTurn = 2;
            _current = 0;
            PublishChange();
        }

        public void Configure(int max, int gainPerTurn)
        {
            _max = max < 0 ? 0 : max;
            _gainPerTurn = gainPerTurn < 0 ? 0 : gainPerTurn;
            if (_current > _max) _current = _max;
            PublishChange();
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
            PublishChange();
            return true;
        }

        public void GainTurnPoints()
        {
            int before = _current;
            int effectiveGain = _gainPerTurn + (_tempTurnsRemaining > 0 ? _tempBonus : 0);
            _current += effectiveGain;
            if (_current > _max) _current = _max;
            PublishChange(effectiveGain);
            ManaGainFloatingFeedback.TryShowOnPlayer(_current - before);
        }

        public void GrantTemporaryGainPerTurnBonus(int bonus, int playerTurnsRemaining)
        {
            if (bonus <= 0 || playerTurnsRemaining <= 0) return;
            _tempBonus = bonus;
            _tempTurnsRemaining = playerTurnsRemaining;
            _skipConsumeThisTurnEnd = true;
        }

        public void ConsumeTemporaryGainTurn()
        {
            if (_skipConsumeThisTurnEnd)
            {
                _skipConsumeThisTurnEnd = false;
                return;
            }

            if (_tempTurnsRemaining > 0)
                _tempTurnsRemaining--;
        }

        public void Refill()
        {
            _current = _max;
            PublishChange();
        }

        public void Reset()
        {
            _current = 0;
            PublishChange();
        }

		public void Add(int amount)
		{
			if (amount <= 0) return;
			int before = _current;
			_current += amount;
			if (_current > _max) _current = _max;
			PublishChange();
			ManaGainFloatingFeedback.TryShowOnPlayer(_current - before);
		}

		public void Subtract(int amount)
		{
			if (amount <= 0) return;
			int before = _current;
			_current -= amount;
			if (_current < 0) _current = 0;
			PublishChange();
			int lost = before - _current;
			if (lost > 0)
				ManaLostFloatingFeedback.TryShowOnPlayer(lost);
		}

        private void PublishChange(int effectiveGainPerTurn = -1)
        {
            int loggedGain = effectiveGainPerTurn >= 0 ? effectiveGainPerTurn : _gainPerTurn;
			UnityEngine.Debug.Log($"[AP] {_current}/{_max} (gain/turn={loggedGain})");
            _turnStateService.UpdateActionPoints(_current, _max);
            EnsureGamePlayUiController()?.OnPlayerActionPointsChange(_current, _max);
        }

        private IGamePlayUiController EnsureGamePlayUiController()
        {
            if (_gamePlayUiController != null) return _gamePlayUiController;
            var sceneCtxs = UnityEngine.Object.FindObjectsByType<SceneContext>(UnityEngine.FindObjectsSortMode.None);
            for (int i = 0; i < sceneCtxs.Length; i++)
            {
                var sc = sceneCtxs[i];
                if (sc == null) continue;
                try
                {
                    _gamePlayUiController = sc.Container.Resolve<IGamePlayUiController>();
                    if (_gamePlayUiController != null) return _gamePlayUiController;
                }
                catch { }
            }
            return null;
        }
    }
}
