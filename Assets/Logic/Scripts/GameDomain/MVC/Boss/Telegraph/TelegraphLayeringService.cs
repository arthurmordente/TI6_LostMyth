using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.MVC.Boss.Telegraph
{
	public class TelegraphLayeringService : ITelegraphLayeringService
	{
		private readonly float _baseY = 0.1f;
		private readonly float _stepY = 0.01f;
		private readonly int _stepQueue = 5;

		private int _nextId = 0;
		private readonly Stack<int> _freeIds = new Stack<int>();

		public ITelegraphLayeringService.TelegraphLayer Register(bool preferTop)
		{
			int id = _freeIds.Count > 0 ? _freeIds.Pop() : _nextId++;
			return new ITelegraphLayeringService.TelegraphLayer
			{
				Id = id,
				Y = _baseY + id * _stepY,
				QueueAdd = id * _stepQueue
			};
		}

		public void Unregister(int id)
		{
			if (id < 0) return;
			_freeIds.Push(id);
		}
	}
}

