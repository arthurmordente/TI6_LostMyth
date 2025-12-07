using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.Turns;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Diamond
{
	public class DiamondActor : MonoBehaviour, IEnvironmentTurnActor, IEffectable
	{
		private IDiamondCallbacks _callbacks;
		private IEnvironmentActorsRegistry _envReg;
		private int _hp;
		private bool _exploded;
		private Vector3 _center;
		public bool RemoveAfterRun => true;

		public void Init(IDiamondCallbacks callbacks, IEnvironmentActorsRegistry envReg, int hp, Vector3 center)
		{
			_callbacks = callbacks;
			_envReg = envReg;
			_hp = hp;
			_center = center;
		}

		public async Task ExecuteAsync()
		{
			if (_exploded) { return; }
			_exploded = true;
			UnityEngine.Debug.Log("[Laki] DiamondActor: explode on EnviromentAct");
			_callbacks?.OnDiamondExploded();
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
			UnityEngine.Debug.Log($"[Laki] DiamondActor: took {damageAmount} damage, hp now={_hp}");
			if (_hp <= 0)
			{
				UnityEngine.Debug.Log("[Laki] DiamondActor: destroyed by player");
				_callbacks?.OnDiamondDestroyed();
				Destroy(gameObject);
			}
		}
		public void TakeDamagePerTurn(int damageAmount, int duration) { }
		public void Heal(int healAmount) { _hp += Mathf.Max(0, healAmount); }
		public void HealPerTurn(int healAmount, int duration) { }
	}
}


