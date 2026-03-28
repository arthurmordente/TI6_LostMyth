using Logic.Scripts.GameDomain.MVC.Ui;
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

        public int Current => _current;
        public int Max => _max;
        public int GainPerTurn => _gainPerTurn;

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
            _current += _gainPerTurn;
            if (_current > _max) _current = _max;
            PublishChange();
        }

        public void Refill()
        {
            _current = _max;
            PublishChange();
        }

        public void Reset()
        {
            _current = 2;
            PublishChange();
        }

		public void Add(int amount)
		{
			if (amount <= 0) return;
			_current += amount;
			if (_current > _max) _current = _max;
			PublishChange();
		}

		public void Subtract(int amount)
		{
			if (amount <= 0) return;
			_current -= amount;
			if (_current < 0) _current = 0;
			PublishChange();
		}

        private void PublishChange()
        {
			UnityEngine.Debug.Log($"[AP] {_current}/{_max} (gain/turn={_gainPerTurn})");
            _turnStateService.UpdateActionPoints(_current, _max);
            EnsureGamePlayUiController()?.OnPlayerActionPointsChange(_current);
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
