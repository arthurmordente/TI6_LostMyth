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
		[SerializeField] private TMP_Text _playerDiceText;
		[SerializeField] private TMP_Text _bossDiceText;

		private IChipService _chipService;
		private int _dispPlayer;
		private int _dispBoss;
		private bool _potAnimating;
		private int _potValue;
		private bool _hasPendingFinal;
		private int _pendingFinalPlayer;
		private int _pendingFinalBoss;
		[SerializeField] private float _chipTickBaseSeconds = 0.5f;

		private System.Collections.IEnumerator WaitTick(int tickIndex)
		{
			int accelSteps = tickIndex / 5;
			float factor = 1f;
			for (int i = 0; i < accelSteps; i++) factor *= 0.5f;
			yield return new WaitForSeconds(_chipTickBaseSeconds * factor);
		}

		public void SetRefs(Image playerIcon, TMP_Text playerText, Image bossIcon, TMP_Text bossText, TMP_Text potText, TMP_Text playerDiceText, TMP_Text bossDiceText, float stepInterval = 0.05f)
		{
			_playerIcon = playerIcon;
			_playerText = playerText;
			_bossIcon = bossIcon;
			_bossText = bossText;
			_potText = potText;
			_playerDiceText = playerDiceText;
			_bossDiceText = bossDiceText;
			_stepInterval = stepInterval;
			if (_chipService == null) TryResolveService();
			if (_chipService != null && _playerText != null && _bossText != null)
			{
				_dispPlayer = _chipService.PlayerChips;
				_dispBoss = _chipService.BossChips;
				_playerText.SetText(_dispPlayer.ToString());
				_bossText.SetText(_dispBoss.ToString());
				if (_potText != null) { _potValue = 0; _potText.SetText("0"); }
				if (_playerDiceText != null) { _playerDiceText.gameObject.SetActive(false); _playerDiceText.SetText("0"); }
				if (_bossDiceText != null) { _bossDiceText.gameObject.SetActive(false); _bossDiceText.SetText("0"); }
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
				_chipService.OnChipPurchased += OnChipPurchased;
				Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnProgress += OnDiceProgress;
				Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnFinalAnimation += OnDiceFinalAnimation;
				Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnReset += OnDiceReset;
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
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnProgress -= OnDiceProgress;
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnFinalAnimation -= OnDiceFinalAnimation;
			Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnReset -= OnDiceReset;
			if (_chipService != null) _chipService.OnChipPurchased -= OnChipPurchased;
		}

		private void OnChipsChanged(int player, int boss)
		{
			if (_potAnimating) { _hasPendingFinal = true; _pendingFinalPlayer = player; _pendingFinalBoss = boss; return; }
			UnityEngine.Debug.Log($"[Laki][ChipsUI] OnChipsChanged P={player} B={boss} (start anim)");
			StopAllCoroutines();
			StartCoroutine(AnimateCounts(player, boss));
		}

		private void OnDiceReset()
		{
			if (_playerDiceText == null || _bossDiceText == null) return;
			_playerDiceText.SetText("0");
			_bossDiceText.SetText("0");
			_playerDiceText.gameObject.SetActive(false);
			_bossDiceText.gameObject.SetActive(false);
			_playerDiceText.rectTransform.localScale = Vector3.one;
			_bossDiceText.rectTransform.localScale = Vector3.one;
			_playerDiceText.color = new Color(_playerDiceText.color.r, _playerDiceText.color.g, _playerDiceText.color.b, 1f);
			_bossDiceText.color = new Color(_bossDiceText.color.r, _bossDiceText.color.g, _bossDiceText.color.b, 1f);
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

		private void OnChipPurchased(bool isPlayer, int count, int hpPerChip)
		{
			if (_playerText == null || _bossText == null) return;
			StopAllCoroutines();
			StartCoroutine(AnimateChipPurchase(isPlayer, count));
		}

		private void OnDiceProgress(System.Collections.Generic.List<int> pRolls, int pSum, System.Collections.Generic.List<int> bRolls, int bSum)
		{
			if (_playerDiceText == null || _bossDiceText == null) return;
			_playerDiceText.gameObject.SetActive(true);
			_bossDiceText.gameObject.SetActive(true);
			_playerDiceText.SetText(FormatDice(pRolls, pSum));
			_bossDiceText.SetText(FormatDice(bRolls, bSum));
		}

		private void OnDiceFinalAnimation(int pSum, int bSum)
		{
			if (_playerDiceText == null || _bossDiceText == null) return;
			StopAllCoroutines();
			StartCoroutine(AnimateDiceFinal(pSum, bSum, 3f));
		}

		private System.Collections.IEnumerator AnimateCounts(int targetPlayer, int targetBoss)
		{
			UnityEngine.Debug.Log($"[Laki][ChipsUI] AnimateCounts begin from P={_dispPlayer} B={_dispBoss} to P={targetPlayer} B={targetBoss}");
			int tick = 0;
			while (_dispPlayer != targetPlayer || _dispBoss != targetBoss)
			{
				if (_dispPlayer < targetPlayer) _dispPlayer++;
				else if (_dispPlayer > targetPlayer) _dispPlayer--;
				if (_dispBoss < targetBoss) _dispBoss++;
				else if (_dispBoss > targetBoss) _dispBoss--;
				_playerText.SetText(_dispPlayer.ToString());
				_bossText.SetText(_dispBoss.ToString());
				yield return WaitTick(tick);
				tick++;
			}
			UnityEngine.Debug.Log($"[Laki][ChipsUI] AnimateCounts end P={_dispPlayer} B={_dispBoss}");
		}

		private System.Collections.IEnumerator AnimateBet(int playerBet, int bossBet)
		{
			_potAnimating = true;
			int pRemain = playerBet < 0 ? 0 : playerBet;
			int bRemain = bossBet < 0 ? 0 : bossBet;
			if (_potText != null) { _potValue = 0; _potText.SetText("0"); }
			int potTarget = pRemain + bRemain;
			int potAdded = 0;
			int tick = 0;
			while (potAdded < potTarget)
			{
				if (pRemain > 0 && _dispPlayer > 0) { _dispPlayer--; pRemain--; }
				if (bRemain > 0 && _dispBoss > 0) { _dispBoss--; bRemain--; }
				potAdded++;
				if (_potText != null) { _potValue += 1; _potText.SetText(_potValue.ToString()); }
				_playerText.SetText(_dispPlayer.ToString());
				_bossText.SetText(_dispBoss.ToString());
				yield return WaitTick(tick);
				tick++;
			}
			yield return null;
			_potAnimating = false;
		}

		private System.Collections.IEnumerator AnimatePotResolve(bool playerWon, int pot)
		{
			_potAnimating = true;
			int remain = pot < 0 ? 0 : pot;
			int tick = 0;
			while (remain > 0 && _potValue > 0)
			{
				_potValue--;
				if (playerWon) _dispPlayer++; else _dispBoss++;
				_playerText.SetText(_dispPlayer.ToString());
				_bossText.SetText(_dispBoss.ToString());
				if (_potText != null) _potText.SetText(_potValue.ToString());
				remain--;
				yield return WaitTick(tick);
				tick++;
			}
			_potAnimating = false;
			if (_hasPendingFinal)
			{
				_hasPendingFinal = false;
				StartCoroutine(AnimateCounts(_pendingFinalPlayer, _pendingFinalBoss));
			}
		}

		private System.Collections.IEnumerator AnimateChipPurchase(bool isPlayer, int count)
		{
			int tick = 0;
			int remain = count;
			while (remain > 0)
			{
				if (isPlayer) _dispPlayer++; else _dispBoss++;
				_playerText.SetText(_dispPlayer.ToString());
				_bossText.SetText(_dispBoss.ToString());
				yield return WaitTick(tick);
				tick++;
				remain--;
			}
		}
		private string FormatDice(System.Collections.Generic.List<int> rolls, int sum)
		{
			if (rolls == null || rolls.Count == 0) return "0";
			if (rolls.Count == 1) return rolls[0].ToString();
			System.Text.StringBuilder sb = new System.Text.StringBuilder();
			for (int i = 0; i < rolls.Count; i++)
			{
				if (i > 0) sb.Append(" + ");
				sb.Append(rolls[i]);
			}
			sb.Append(" = ");
			sb.Append(sum);
			return sb.ToString();
		}

		private System.Collections.IEnumerator AnimateDiceFinal(int pSum, int bSum, float duration)
		{
			_playerDiceText.SetText(pSum.ToString());
			_bossDiceText.SetText(bSum.ToString());
			Color pCol = _playerDiceText.color;
			Color bCol = _bossDiceText.color;
			Vector3 pStart = _playerDiceText.rectTransform.localScale;
			Vector3 bStart = _bossDiceText.rectTransform.localScale;
			Vector3 pEnd = pStart;
			Vector3 bEnd = bStart;
			bool playerWon = pSum >= bSum; // empate favorece Laki; UI só destaca player se estritamente maior
			if (pSum > bSum) { pEnd = pStart * 1.15f; bCol.a = 0.5f; }
			else if (bSum >= pSum) { bEnd = bStart * 1.15f; pCol.a = 0.5f; }
			float t = 0f;
			while (t < duration)
			{
				t += Time.deltaTime;
				float k = Mathf.Clamp01(t / duration);
				_playerDiceText.rectTransform.localScale = Vector3.Lerp(pStart, pEnd, k);
				_bossDiceText.rectTransform.localScale = Vector3.Lerp(bStart, bEnd, k);
				_playerDiceText.color = Color.Lerp(_playerDiceText.color, pCol, k);
				_bossDiceText.color = Color.Lerp(_bossDiceText.color, bCol, k);
				yield return null;
			}
			_playerDiceText.SetText("0");
			_bossDiceText.SetText("0");
			_playerDiceText.gameObject.SetActive(false);
			_bossDiceText.gameObject.SetActive(false);
			_playerDiceText.rectTransform.localScale = Vector3.one;
			_bossDiceText.rectTransform.localScale = Vector3.one;
			_playerDiceText.color = new Color(_playerDiceText.color.r, _playerDiceText.color.g, _playerDiceText.color.b, 1f);
			_bossDiceText.color = new Color(_bossDiceText.color.r, _bossDiceText.color.g, _bossDiceText.color.b, 1f);
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

