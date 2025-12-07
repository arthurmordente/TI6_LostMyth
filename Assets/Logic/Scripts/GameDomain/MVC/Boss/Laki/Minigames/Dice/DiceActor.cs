using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.Turns;
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
		private int _value;
		private Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView _arena;
		private int _tileIndex;
		private System.Collections.IEnumerator _moveRoutine;
		private readonly System.Random _rng = new System.Random();
		private TextMeshPro[] _faceLabels;
		private bool _labelsCreated;
		public bool RemoveAfterRun => true;

		public void Init(IDiceCallbacks callbacks, bool isBoss, int maxValue, int hp, int initialValue,
			Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView arena, int targetTileIndex, Vector3 spawnPosition)
		{
			_callbacks = callbacks;
			_isBoss = isBoss;
			_maxValue = maxValue > 0 ? maxValue : 6;
			_hp = hp > 0 ? hp : 99;
			_value = Mathf.Clamp(initialValue, 1, _maxValue);
			_arena = arena;
			_tileIndex = Mathf.Max(0, targetTileIndex);
			transform.position = spawnPosition;
			UnityEngine.Debug.Log($"[Laki][Die] Init value={_value} isBoss={_isBoss}");
			CreateOrUpdateFaceLabels();
			if (_arena != null)
			{
				Vector3 target = _arena.GetTileWorldCenter(_tileIndex);
				StartMove(target, 2.0f);
			}
		}

		public async Task ExecuteAsync()
		{
			UnityEngine.Debug.Log($"[Laki][Die] Execute roll value={_value} isBoss={_isBoss}");
			_callbacks?.OnDiceRolled(_isBoss, _value);
			Destroy(gameObject);
			await Task.CompletedTask;
		}

		public Transform GetReferenceTransform() { return transform; }
		public Transform GetTransformCastPoint() { return transform; }
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
			UnityEngine.Debug.Log($"[Laki][Die] Reroll value={_value} isBoss={_isBoss}");
			_callbacks?.OnDieValueChanged(_isBoss, _value);

			// Move to adjacent tile with tumble
			if (_arena != null)
			{
				int tileCount = _arena.TileCount;
				int bands = 2;
				int sectorCount = tileCount / bands;
				// estimate nearest tile index to current position
				int nearest = 0;
				float best = float.MaxValue;
				for (int i = 0; i < tileCount; i++)
				{
					Vector3 c = _arena.GetTileWorldCenter(i);
					float d = (c - transform.position).sqrMagnitude;
					if (d < best) { best = d; nearest = i; }
				}
				int band = nearest % bands;
				int sector = nearest / bands;
				int dir = (Random.value < 0.5f) ? -1 : 1;
				int newSector = (sector + dir + sectorCount) % sectorCount;
				_tileIndex = newSector * bands + band;
				Vector3 target = _arena.GetTileWorldCenter(_tileIndex);
				StartMove(target, 1.0f);
			}
			if (_hp <= 0) Destroy(gameObject);
			CreateOrUpdateFaceLabels();
		}
		public void TakeDamagePerTurn(int damageAmount, int duration) { }
		public void Heal(int healAmount) { _hp += Mathf.Max(0, healAmount); }
		public void HealPerTurn(int healAmount, int duration) { }

		private void StartMove(Vector3 target, float duration)
		{
			if (_moveRoutine != null) StopCoroutine(_moveRoutine);
			target.y = 8.2f;
			_moveRoutine = AnimateMove(target, duration);
			StartCoroutine(_moveRoutine);
		}

		private System.Collections.IEnumerator AnimateMove(Vector3 target, float duration)
		{
			Vector3 start = transform.position;
			float t = 0f;
			// random tumble axes/speeds
			Vector3 axis = Vector3.Normalize(new Vector3((float)_rng.NextDouble() - 0.5f, (float)_rng.NextDouble() - 0.5f, (float)_rng.NextDouble() - 0.5f));
			if (axis == Vector3.zero) axis = Vector3.up;
			float angSpeed = Random.Range(360f, 900f);
			// ensure starting rotation is zeroed
			transform.rotation = Quaternion.identity;
			// jitter the displayed number while rolling
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
				// randomize label periodically during roll
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
			// ensure final rotation is zeroed
			transform.rotation = Quaternion.identity;
			_callbacks?.OnDieAnimationComplete(_isBoss, _value);
			// restore actual value on labels
			UpdateFaceLabels(_value);
		}

		private void CreateOrUpdateFaceLabels()
		{
			// Try to pick up existing labels first
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
						new Vector3(+localExt.x + pad, 0f, 0f), // +X
						new Vector3(-localExt.x - pad, 0f, 0f), // -X
						new Vector3(0f, +localExt.y + pad, 0f), // +Y
						new Vector3(0f, -localExt.y - pad, 0f), // -Y
						new Vector3(0f, 0f, +localExt.z + pad), // +Z
						new Vector3(0f, 0f, -localExt.z - pad)  // -Z
					};
					// Final per-face eulers provided by user
					Vector3[] faceFinalEuler = new Vector3[] {
						new Vector3(0f, -90f, 0f),  // +X
						new Vector3(0f,  90f, 0f),  // -X
						new Vector3(90f,  0f, 0f),  // +Y
						new Vector3(-90f, 0f, 0f),  // -Y
						new Vector3(0f, 180f, 0f),  // +Z
						new Vector3(0f,   0f, 0f)   // -Z
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

