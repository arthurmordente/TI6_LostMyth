using Zenject;
using Logic.Scripts.GameDomain.MVC.Abilitys;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips
{
	public class LakiChipRuntimeService : IChipService
	{
		private int _player;
		private int _boss;
		public int PlayerChips => _player;
		public int BossChips => _boss;
		public System.Action<int, int> OnChipsChanged { get; set; }
		public System.Action<int, int> OnBetPlaced { get; set; }
		public System.Action<bool, int> OnPotResolve { get; set; }

		public void SetInitial(int player, int boss)
		{
			_player = player < 0 ? 0 : player;
			_boss = boss < 0 ? 0 : boss;
			UnityEngine.Debug.Log($"[Laki][Chips] SetInitial P={_player} B={_boss}");
			OnChipsChanged?.Invoke(_player, _boss);
		}

		public void ApplyMinigameResult(Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.MinigameResult result)
		{
			int pDelta = result.PlayerChipsDelta;
			int bDelta = result.BossChipsDelta;
			int pot = pDelta + bDelta;
			bool playerWon = pDelta > 0;
			if (pot > 0) OnPotResolve?.Invoke(playerWon, pot);
			_player += pDelta;
			_boss += bDelta;
			if (_player < 0) _player = 0;
			if (_boss < 0) _boss = 0;
			UnityEngine.Debug.Log($"[Laki][Chips] ApplyResult P+={pDelta} B+={bDelta} -> P={_player} B={_boss}");
			OnChipsChanged?.Invoke(_player, _boss);
		}

		public void Refresh()
		{
			UnityEngine.Debug.Log($"[Laki][Chips] Refresh -> publish P={_player} B={_boss}");
			OnChipsChanged?.Invoke(_player, _boss);
		}

		public bool TryPayPlayer(Logic.Scripts.GameDomain.MVC.Nara.INaraController player, int cost, out int hpConverted)
		{
			hpConverted = 0;
			if (cost <= 0) return true;
			if (_player >= cost)
			{
				_player -= cost;
				UnityEngine.Debug.Log($"[Laki][Chips] TryPayPlayer cost={cost} (no convert) -> P={_player}");
				return true;
			}
			int need = cost - _player;
			var eff = player as IEffectable;
			if (eff == null) return false;
			hpConverted = need;
			try { eff.TakeDamage(need); } catch { }
			_player += need;
			if (_player < 0) _player = 0;
			_player -= cost;
			if (_player < 0) _player = 0;
			UnityEngine.Debug.Log($"[Laki][Chips] TryPayPlayer cost={cost} convertHP={need} -> P={_player}");
			return true;
		}

		public bool TryPayBoss(Logic.Scripts.GameDomain.MVC.Boss.IBossController boss, int cost, out int hpConverted)
		{
			hpConverted = 0;
			if (cost <= 0) return true;
			if (_boss >= cost)
			{
				_boss -= cost;
				UnityEngine.Debug.Log($"[Laki][Chips] TryPayBoss cost={cost} (no convert) -> B={_boss}");
				return true;
			}
			int need = cost - _boss;
			var eff = boss as IEffectable;
			if (eff == null) return false;
			hpConverted = need;
			try { eff.TakeDamage(need); } catch { }
			_boss += need;
			if (_boss < 0) _boss = 0;
			_boss -= cost;
			if (_boss < 0) _boss = 0;
			UnityEngine.Debug.Log($"[Laki][Chips] TryPayBoss cost={cost} convertHP={need} -> B={_boss}");
			return true;
		}
	}
}

