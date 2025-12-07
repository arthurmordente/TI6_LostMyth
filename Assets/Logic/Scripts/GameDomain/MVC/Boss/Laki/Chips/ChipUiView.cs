using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Zenject;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Chips
{
	public class ChipUiView : MonoBehaviour
	{
		[SerializeField] private Image _playerIcon;
		[SerializeField] private TMP_Text _playerText;
		[SerializeField] private Image _bossIcon;
		[SerializeField] private TMP_Text _bossText;
		[SerializeField] private TMP_Text _potText;
		[SerializeField] private float _stepInterval = 0.05f;

		private IChipService _chipService;
		private int _dispPlayer;
		private int _dispBoss;
		private bool _animating;
		private bool _potAnimating;
		private int _potValue;
		private bool _hasPendingFinal;
		private int _pendingFinalPlayer;
		private int _pendingFinalBoss;

		public void SetRefs(Image playerIcon, TMP_Text playerText, Image bossIcon, TMP_Text bossText, TMP_Text potText, float stepInterval = 0.05f)
		{
			_playerIcon = playerIcon;
			_playerText = playerText;
			_bossIcon = bossIcon;
			_bossText = bossText;
			_potText = potText;
			_stepInterval = stepInterval;
			if (_chipService == null) TryResolveService();
			if (_chipService != null && _playerText != null && _bossText != null)
			{
				_dispPlayer = _chipService.PlayerChips;
				_dispBoss = _chipService.BossChips;
				_playerText.SetText(_dispPlayer.ToString());
				_bossText.SetText(_dispBoss.ToString());
				if (_potText != null) { _potValue = 0; _potText.SetText("0"); }
				UnityEngine.Debug.Log($"[Laki][ChipsUI] SetRefs and init texts P={_dispPlayer} B={_dispBoss}");
			}
		}

		private void Start()
		{
			if (_chipService == null) TryResolveService();
			if (_chipService != null)
			{
				if (_playerText == null || _bossText == null) return;
				_dispPlayer = _chipService.PlayerChips;
				_dispBoss = _chipService.BossChips;
				_playerText.SetText(_dispPlayer.ToString());
				_bossText.SetText(_dispBoss.ToString());
				if (_potText != null) { _potValue = 0; _potText.SetText("0"); }
				UnityEngine.Debug.Log($"[Laki][ChipsUI] Start subscribe; init P={_dispPlayer} B={_dispBoss}");
				_chipService.OnChipsChanged += OnChipsChanged;
				_chipService.OnBetPlaced += OnBetPlaced;
				_chipService.OnPotResolve += OnPotResolve;
			}
		}

		private void OnDestroy()
		{
			if (_chipService != null)
			{
				_chipService.OnChipsChanged -= OnChipsChanged;
				_chipService.OnBetPlaced -= OnBetPlaced;
				_chipService.OnPotResolve -= OnPotResolve;
			}
		}

		private void OnChipsChanged(int player, int boss)
		{
			if (_potAnimating) { _hasPendingFinal = true; _pendingFinalPlayer = player; _pendingFinalBoss = boss; return; }
			UnityEngine.Debug.Log($"[Laki][ChipsUI] OnChipsChanged P={player} B={boss} (start anim)");
			StopAllCoroutines();
			StartCoroutine(AnimateCounts(player, boss));
		}

		private void OnBetPlaced(int playerBet, int bossBet)
		{
			if (_playerText == null || _bossText == null) return;
			StopAllCoroutines();
			StartCoroutine(AnimateBet(playerBet, bossBet));
		}

		private void OnPotResolve(bool playerWon, int pot)
		{
			if (_playerText == null || _bossText == null) return;
			StopAllCoroutines();
			StartCoroutine(AnimatePotResolve(playerWon, pot));
		}

		private System.Collections.IEnumerator AnimateCounts(int targetPlayer, int targetBoss)
		{
			_animating = true;
			UnityEngine.Debug.Log($"[Laki][ChipsUI] AnimateCounts begin from P={_dispPlayer} B={_dispBoss} to P={targetPlayer} B={targetBoss}");
			while (_dispPlayer != targetPlayer || _dispBoss != targetBoss)
			{
				if (_dispPlayer < targetPlayer) _dispPlayer++;
				else if (_dispPlayer > targetPlayer) _dispPlayer--;
				if (_dispBoss < targetBoss) _dispBoss++;
				else if (_dispBoss > targetBoss) _dispBoss--;
				_playerText.SetText(_dispPlayer.ToString());
				_bossText.SetText(_dispBoss.ToString());
				yield return new WaitForSeconds(_stepInterval);
			}
			_animating = false;
			UnityEngine.Debug.Log($"[Laki][ChipsUI] AnimateCounts end P={_dispPlayer} B={_dispBoss}");
		}

		private System.Collections.IEnumerator AnimateBet(int playerBet, int bossBet)
		{
			_potAnimating = true;
			int pRemain = playerBet < 0 ? 0 : playerBet;
			int bRemain = bossBet < 0 ? 0 : bossBet;
			if (_potText != null) { _potValue = 0; _potText.SetText("0"); }
			while (pRemain > 0 || bRemain > 0)
			{
				int potDelta = 0;
				if (pRemain > 0 && _dispPlayer > 0) { _dispPlayer--; pRemain--; potDelta++; }
				if (bRemain > 0 && _dispBoss > 0) { _dispBoss--; bRemain--; potDelta++; }
				if (potDelta > 0)
				{
					if (_potText != null) { _potValue += potDelta; _potText.SetText(_potValue.ToString()); }
					_playerText.SetText(_dispPlayer.ToString());
					_bossText.SetText(_dispBoss.ToString());
					yield return new WaitForSeconds(_stepInterval);
				}
				else { break; }
			}
			yield return null;
			_potAnimating = false;
		}

		private System.Collections.IEnumerator AnimatePotResolve(bool playerWon, int pot)
		{
			_potAnimating = true;
			int remain = pot < 0 ? 0 : pot;
			while (remain > 0 && _potValue > 0)
			{
				_potValue--;
				if (playerWon) _dispPlayer++; else _dispBoss++;
				_playerText.SetText(_dispPlayer.ToString());
				_bossText.SetText(_dispBoss.ToString());
				if (_potText != null) _potText.SetText(_potValue.ToString());
				remain--;
				yield return new WaitForSeconds(_stepInterval);
			}
			_potAnimating = false;
			if (_hasPendingFinal)
			{
				_hasPendingFinal = false;
				StartCoroutine(AnimateCounts(_pendingFinalPlayer, _pendingFinalBoss));
			}
		}

		private void TryResolveService()
		{
			DiContainer sceneContainer = null;
			var sceneCtxs = Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None);
			for (int i = 0; i < sceneCtxs.Length; i++)
			{
				var sc = sceneCtxs[i];
				if (sc != null && sc.gameObject.scene == gameObject.scene)
				{
					sceneContainer = sc.Container;
					break;
				}
			}
			if (sceneContainer != null)
			{
				try { _chipService = sceneContainer.Resolve<IChipService>(); UnityEngine.Debug.Log("[Laki][ChipsUI] Resolved IChipService via SceneContext"); } catch { _chipService = null; }
				if (_chipService != null) return;
			}
			// No fallback to ProjectContext per user request
		}
	}
}

