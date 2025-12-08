using System.Collections.Generic;

namespace Logic.Scripts.GameDomain.MVC.Boss.Telegraph
{
	public static class TelegraphVisibilityRegistry
	{
		private static readonly object _guard = new object();
		private static readonly List<Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility> _items
			= new List<Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility>(16);

		public static void Register(Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility v)
		{
			if (v == null) return;
			lock (_guard)
			{
				if (!_items.Contains(v)) _items.Add(v);
			}
		}

		public static void Unregister(Logic.Scripts.GameDomain.MVC.Boss.Attacks.Core.ITelegraphVisibility v)
		{
			if (v == null) return;
			lock (_guard)
			{
				_items.Remove(v);
			}
		}

		public static void SetAllVisible(bool visible)
		{
			lock (_guard)
			{
				for (int i = 0; i < _items.Count; i++)
				{
					try { _items[i]?.SetTelegraphVisible(visible); } catch { }
				}
			}
		}

		public static void Clear()
		{
			lock (_guard) { _items.Clear(); }
		}
	}
}


