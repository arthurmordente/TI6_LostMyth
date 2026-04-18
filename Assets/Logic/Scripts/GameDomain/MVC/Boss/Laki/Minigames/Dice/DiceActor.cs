using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.Turns;
using Logic.Scripts.GameDomain.VisualFeedback;
using TMPro;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice
{
	public class DiceActor : MonoBehaviour, IEnvironmentTurnActor, IEffectable
	{
		[SerializeField] private bool _isBoss;
		[SerializeField] private int _maxValue = 6;
		[SerializeField] private int _hp = 99;
		[SerializeField] private bool _incrementOnDamage;

		private IDiceCallbacks _callbacks;
		private bool _reportRollOnEnvironmentExecute = true;
		private int _rollSlotIndex;
		private int _value;
		private Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView _arena;
		/// <summary>Committed tile after the last move finished (or spawn tile before first move completes).</summary>
		private int _tileIndex;
		/// <summary>While moving: destination tile reserved so other dice cannot pick it in the same frame.</summary>
		private bool _moveInProgress;
		private int _moveTargetTileIndex;
		private System.Collections.IEnumerator _moveRoutine;
		private readonly System.Random _rng = new System.Random();
		private TextMeshPro[] _faceLabels;
		private bool _labelsCreated;
		public bool RemoveAfterRun => true;

		/// <param name="reportRollOnEnvironmentExecute">
		/// When true (default), <see cref="OnDiceRolled"/> runs in <see cref="ExecuteAsync"/> (Environment phase).
		/// When false, it runs at spawn so HUD can show totals during Boss/Player turns (DiceAttack flow).
		/// </param>
		public void Init(IDiceCallbacks callbacks, bool isBoss, int maxValue, int hp, int initialValue,
			Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView arena, int targetTileIndex, Vector3 spawnPosition,
			int rollSlotIndex = 0, bool reportRollOnEnvironmentExecute = true)
		{
			_callbacks = callbacks;
			_rollSlotIndex = rollSlotIndex;
			_reportRollOnEnvironmentExecute = reportRollOnEnvironmentExecute;
			_isBoss = isBoss;
			_maxValue = maxValue > 0 ? maxValue : 6;
			_hp = hp > 0 ? hp : 99;
			_value = Mathf.Clamp(initialValue, 1, _maxValue);
			_arena = arena;
			_tileIndex = Mathf.Max(0, targetTileIndex);
			_moveInProgress = false;
			_moveTargetTileIndex = -1;
			transform.position = spawnPosition;
			CreateOrUpdateFaceLabels();
			if (_arena != null)
			{
				Vector3 target = _arena.GetTileWorldCenter(_tileIndex);
				StartMove(target, 2.0f, _tileIndex);
			}
			if (!_reportRollOnEnvironmentExecute)
				_callbacks?.OnDiceRolled(_isBoss, _rollSlotIndex, _value);
		}

		public async Task ExecuteAsync()
		{
			if (_reportRollOnEnvironmentExecute)
				_callbacks?.OnDiceRolled(_isBoss, _rollSlotIndex, _value);
			Destroy(gameObject);
			await Task.CompletedTask;
		}

		public Transform GetReferenceTransform() { return transform; }
		public Transform GetTransformCastPoint() { return transform; }
		public LineRenderer GetPointLineRenderer() => null;
		public GameObject GetReferenceTargetPrefab() { return gameObject; }
		public void PreviewHeal(int healAmound) { }
		public void PreviewDamage(int damageAmound) { }
		public void ResetPreview() { }
		public void TakeDamage(int damageAmount)
		{
			_hp -= Mathf.Max(0, damageAmount);
			if (_incrementOnDamage)
			{
				int inc = Mathf.Max(1, damageAmount);
				_value = (((_value - 1) + inc) % _maxValue) + 1;
			}
			else
			{
				_value = Random.Range(1, _maxValue + 1);
			}
			_callbacks?.OnDieValueChanged(_isBoss, _rollSlotIndex, _value);

			if (_arena != null)
			{
				int newTile = PickRerollTileAvoidingOccupied();
				Vector3 target = _arena.GetTileWorldCenter(newTile);
				StartMove(target, 1.0f, newTile);
			}
			if (_hp <= 0) Destroy(gameObject);
			CreateOrUpdateFaceLabels();
		}
		public void TakeDamagePerTurn(int damageAmount, int duration) { }
		public void Heal(int healAmount) { _hp += Mathf.Max(0, healAmount); }
		public void HealPerTurn(int healAmount, int duration) { }

		public void SetSkillTargetingHighlight(bool active) {
			SkillTargetingHighlightBridge.SetHighlighted(this, active);
		}

		private void StartMove(Vector3 target, float duration, int targetTileIndex)
		{
			if (_moveRoutine != null) StopCoroutine(_moveRoutine);
			_moveInProgress = true;
			_moveTargetTileIndex = targetTileIndex;
			target.y = target.y + 1f;
			_moveRoutine = AnimateMove(target, duration);
			StartCoroutine(_moveRoutine);
		}

		private System.Collections.IEnumerator AnimateMove(Vector3 target, float duration)
		{
			Vector3 start = transform.position;
			float t = 0f;
			Vector3 axis = Vector3.Normalize(new Vector3((float)_rng.NextDouble() - 0.5f, (float)_rng.NextDouble() - 0.5f, (float)_rng.NextDouble() - 0.5f));
			if (axis == Vector3.zero) axis = Vector3.up;
			float angSpeed = Random.Range(360f, 900f);
			transform.rotation = Quaternion.identity;
			float jitterTimer = 0f;
			while (t < duration)
			{
				t += Time.deltaTime;
				float k = Mathf.Clamp01(t / duration);
				Vector3 pos = Vector3.Lerp(start, target, k);
				float hop = 6f * 4f * k * (1f - k);
				pos.y = Mathf.Lerp(start.y, target.y, k) + hop;
				transform.position = pos;
				transform.Rotate(axis, angSpeed * Time.deltaTime, Space.World);
				jitterTimer -= Time.deltaTime;
				if (jitterTimer <= 0f)
				{
					int tempVal = Random.Range(1, _maxValue + 1);
					UpdateFaceLabels(tempVal);
					jitterTimer = 0.05f;
				}
				yield return null;
			}
			transform.position = target;
			transform.rotation = Quaternion.identity;
			_tileIndex = _moveTargetTileIndex;
			_moveInProgress = false;
			_moveTargetTileIndex = -1;
			_callbacks?.OnDieAnimationComplete(_isBoss, _rollSlotIndex, _value);
			UpdateFaceLabels(_value);
		}

		private int PickRerollTileAvoidingOccupied()
		{
			int tileCount = _arena.TileCount;
			if (tileCount <= 0) return _tileIndex;

			var blocked = new HashSet<int>();
			CollectOccupiedTilesFromOtherDice(this, blocked);

			int radialBands = _arena.RadialBands;
			int nearest = NearestTileIndexToPosition(transform.position, tileCount);
			int band = nearest % radialBands;
			int sectorCount = Mathf.Max(1, tileCount / radialBands);
			int sector = nearest / radialBands;

			for (int pass = 0; pass < 2; pass++)
			{
				bool avoidSameTile = pass == 0;

				for (int radius = 1; radius <= sectorCount; radius++)
				{
					for (int sign = -1; sign <= 1; sign += 2)
					{
						int ns = sector + sign * radius;
						ns = (ns % sectorCount + sectorCount) % sectorCount;
						int candidate = ns * radialBands + band;
						if (IsFreeRerollTile(candidate, blocked, avoidSameTile))
							return candidate;
					}
				}

				for (int s = 0; s < sectorCount; s++)
				{
					int candidate = s * radialBands + band;
					if (IsFreeRerollTile(candidate, blocked, avoidSameTile))
						return candidate;
				}

				for (int i = 0; i < tileCount; i++)
				{
					if (IsFreeRerollTile(i, blocked, avoidSameTile))
						return i;
				}
			}

			return nearest;
		}

		private bool IsFreeRerollTile(int candidate, HashSet<int> blockedByOthers, bool avoidSameTileAsSelf)
		{
			if (blockedByOthers.Contains(candidate)) return false;
			if (avoidSameTileAsSelf && candidate == _tileIndex) return false;
			return true;
		}

		private int NearestTileIndexToPosition(Vector3 worldPos, int tileCount)
		{
			int best = 0;
			float bestD = float.MaxValue;
			for (int i = 0; i < tileCount; i++)
			{
				Vector3 c = _arena.GetTileWorldCenter(i);
				float d = (c - worldPos).sqrMagnitude;
				if (d < bestD) { bestD = d; best = i; }
			}
			return best;
		}

		private void CollectOccupiedTilesFromOtherDice(DiceActor self, HashSet<int> into)
		{
			into.Clear();
			var reg = EnvironmentActorsRegistryService.Instance;
			if (reg != null)
			{
				foreach (var a in reg.Snapshot())
				{
					if (a is not DiceActor d || d == self || d._arena != _arena) continue;
					int t = d._moveInProgress ? d._moveTargetTileIndex : d._tileIndex;
					if (t >= 0) into.Add(t);
				}
				return;
			}
			foreach (var d in Object.FindObjectsByType<DiceActor>(FindObjectsSortMode.None))
			{
				if (d == self || d._arena != _arena) continue;
				int t = d._moveInProgress ? d._moveTargetTileIndex : d._tileIndex;
				if (t >= 0) into.Add(t);
			}
		}

		private void CreateOrUpdateFaceLabels()
		{
			if ((_faceLabels == null || !_labelsCreated))
			{
				var existing = GetComponentsInChildren<TextMeshPro>(true);
				if (existing != null && existing.Length > 0)
				{
					_faceLabels = existing;
					_labelsCreated = true;
				}
				else
				{
					_faceLabels = new TextMeshPro[6];
					var mf = GetComponent<MeshFilter>();
					Vector3 localExt = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds.extents : new Vector3(0.5f, 0.5f, 0.5f);
					float pad = 0.01f;
					Vector3[] offs = new Vector3[] {
						new Vector3(+localExt.x + pad, 0f, 0f),
						new Vector3(-localExt.x - pad, 0f, 0f),
						new Vector3(0f, +localExt.y + pad, 0f),
						new Vector3(0f, -localExt.y - pad, 0f),
						new Vector3(0f, 0f, +localExt.z + pad),
						new Vector3(0f, 0f, -localExt.z - pad)
					};
					Vector3[] faceFinalEuler = new Vector3[] {
						new Vector3(0f, -90f, 0f),
						new Vector3(0f,  90f, 0f),
						new Vector3(90f,  0f, 0f),
						new Vector3(-90f, 0f, 0f),
						new Vector3(0f, 180f, 0f),
						new Vector3(0f,   0f, 0f)
					};
					for (int i = 0; i < 6; i++)
					{
						GameObject go = new GameObject("DieFaceText_" + i);
						go.transform.SetParent(transform, false);
						go.transform.localPosition = offs[i];
						go.transform.localRotation = Quaternion.Euler(faceFinalEuler[i]);
						var tmp = go.AddComponent<TextMeshPro>();
						tmp.alignment = TextAlignmentOptions.Center;
						tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
						tmp.fontSize = 10f;
						tmp.enableAutoSizing = false;
						tmp.color = _isBoss ? Color.black : Color.white;
						float s = Mathf.Min(transform.localScale.x, transform.localScale.y, transform.localScale.z) * 0.4f;
						go.transform.localScale = Vector3.one * Mathf.Max(0.05f, s);
						_faceLabels[i] = tmp;
					}
					_labelsCreated = true;
				}
			}
			UpdateFaceLabels(_value);
		}

		private void UpdateFaceLabels(int shownValue)
		{
			if (_faceLabels == null) return;
			for (int i = 0; i < _faceLabels.Length; i++)
			{
				if (_faceLabels[i] != null) _faceLabels[i].SetText(shownValue.ToString());
			}
		}
	}
}
