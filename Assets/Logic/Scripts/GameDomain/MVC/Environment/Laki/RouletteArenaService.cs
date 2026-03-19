using System;
using System.Collections.Generic;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Nara;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	public sealed class RouletteArenaService
	{
		private readonly System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _positivePool =
			new System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect>(8);
		private readonly System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> _negativePool =
			new System.Collections.Generic.List<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect>(8);

		public enum TileEffectType
		{
			Neutral = 0,
			Positive = 1,
			Negative = 2
		}

		public const float INNER_RADIUS_DEFAULT = 6f;
		public const float OUTER_RADIUS_DEFAULT = 12f;

		private const int SECTOR_COUNT = 8;
		private const int RADIAL_BANDS = 2;
		private const int TILE_COUNT = SECTOR_COUNT * RADIAL_BANDS;

		private readonly float _innerRadius;
		private readonly float _outerRadius;
		private readonly float _radialSplit01;
		private readonly float _sectorAngleRad;
		private readonly float _arcStartRad;
		private readonly float _arcRad;

		private int _lastRolledTurn = int.MinValue;
		private TileEffectType[] _effectsCurrentTurn = new TileEffectType[TILE_COUNT];
		

		public RouletteArenaService(
			float innerRadius = INNER_RADIUS_DEFAULT,
			float outerRadius = OUTER_RADIUS_DEFAULT,
			float radialSplit01 = 0.6f,
			float arcStartDeg = 0f,
			float arcDeg = 180f)
		{
			_innerRadius = Mathf.Max(0.01f, Mathf.Min(innerRadius, outerRadius * 0.999f));
			_outerRadius = Mathf.Max(_innerRadius + 0.01f, outerRadius);
			_radialSplit01 = Mathf.Clamp01(radialSplit01);
			_arcStartRad = arcStartDeg * Mathf.Deg2Rad;
			_arcRad = Mathf.Clamp(arcDeg, 1f, 360f) * Mathf.Deg2Rad;
			_sectorAngleRad = _arcRad / SECTOR_COUNT;
			for (int i = 0; i < TILE_COUNT; i++) _effectsCurrentTurn[i] = TileEffectType.Neutral;
		}

		public int TileCount => TILE_COUNT;
		public float InnerRadius => _innerRadius;
		public float OuterRadius => _outerRadius;
		public float SplitRadius => _innerRadius + _radialSplit01 * (_outerRadius - _innerRadius);

		public void RerollTiles(int turnNumber, System.Random rng)
		{
			if (turnNumber == _lastRolledTurn) return;
			if (rng == null) rng = new System.Random();

			int positives = 5;
			int negatives = 6;
			int neutrals = TILE_COUNT - positives - negatives;

			List<TileEffectType> bag = new List<TileEffectType>(TILE_COUNT);
			for (int i = 0; i < positives; i++) bag.Add(TileEffectType.Positive);
			for (int i = 0; i < negatives; i++) bag.Add(TileEffectType.Negative);
			for (int i = 0; i < neutrals; i++) bag.Add(TileEffectType.Neutral);

			for (int i = bag.Count - 1; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				(bag[i], bag[j]) = (bag[j], bag[i]);
			}

			for (int i = 0; i < TILE_COUNT; i++) _effectsCurrentTurn[i] = bag[i];
			_lastRolledTurn = turnNumber;
		}

		public TileEffectType GetTileEffect(int tileIndex)
		{
			if (tileIndex < 0 || tileIndex >= TILE_COUNT) return TileEffectType.Neutral;
			return _effectsCurrentTurn[tileIndex];
		}

		public int ComputeTileIndex(Vector3 worldPos, Vector3 centerWorld)
		{
			Vector2 rel = new Vector2(worldPos.x - centerWorld.x, worldPos.z - centerWorld.z);
			float r = rel.magnitude;
			if (r < _innerRadius || r > _outerRadius) return -1;

			float theta = Mathf.Atan2(rel.y, rel.x);
			if (theta < 0f) theta += 2f * Mathf.PI;

			// Normalize theta relative to arc start, wrapping into [0, 2π)
			float arcStart = _arcStartRad;
			float relTheta = theta - arcStart;
			if (relTheta < 0f) relTheta += 2f * Mathf.PI;

			// Outside the active arc → not on any tile
			if (relTheta >= _arcRad) return -1;

			int sectorIndex = Mathf.Clamp(Mathf.FloorToInt(relTheta / _sectorAngleRad), 0, SECTOR_COUNT - 1);
			float split = SplitRadius;
			int band = r < split ? 0 : 1;
			return sectorIndex * RADIAL_BANDS + band;
		}

		public void SetEffectPools(
			System.Collections.Generic.IEnumerable<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> positive,
			System.Collections.Generic.IEnumerable<Logic.Scripts.GameDomain.MVC.Abilitys.AbilityEffect> negative)
		{
			_positivePool.Clear();
			_negativePool.Clear();
			if (positive != null) _positivePool.AddRange(positive);
			if (negative != null) _negativePool.AddRange(negative);
		}

		public string ApplyEffectToPlayer(IEffectable caster, INaraController nara, int tileIndex, int turnNumber)
		{
			if (nara == null) return null;
			if (tileIndex < 0 || tileIndex >= TILE_COUNT) return null;

			TileEffectType effect = _effectsCurrentTurn[tileIndex];
			var asEffectable = nara as IEffectable;

			const int healAmount = 5;
			const int damageAmount = 5;
			const int apDelta = 1;
			string appliedName = null;

			switch (effect)
			{
				case TileEffectType.Positive:
					if (_positivePool.Count > 0)
					{
						int idx = DeterministicIndex(turnNumber, tileIndex, _positivePool.Count);
						var eff = _positivePool[idx];
						appliedName = eff != null ? eff.Name : null;
						eff?.Execute(caster, asEffectable);
					}
					else
					{
						asEffectable?.Heal(healAmount);
						(nara as IEffectableAction)?.AddActionPoints(apDelta);
						appliedName = "Heal5_AP+1";
					}
					break;
				case TileEffectType.Negative:
					if (_negativePool.Count > 0)
					{
						int idxN = DeterministicIndex(turnNumber, tileIndex, _negativePool.Count);
						var effN = _negativePool[idxN];
						appliedName = effN != null ? effN.Name : null;
						effN?.Execute(caster, asEffectable);
					}
					else
					{
						asEffectable?.TakeDamage(damageAmount);
						(nara as IEffectableAction)?.SubtractActionPoints(apDelta);
						appliedName = "Damage5_AP-1";
					}
					break;
				case TileEffectType.Neutral:
				default:
					break;
			}
			return appliedName;
		}

		/// <summary>
	/// Applies the current tile effect to any IEffectable target (e.g. the Book).
	/// Mirrors ApplyEffectToPlayer but without needing INaraController.
	/// </summary>
	public string ApplyEffectToEffectable(IEffectable caster, IEffectable target, int tileIndex, int turnNumber)
	{
		if (target == null) return null;
		if (tileIndex < 0 || tileIndex >= TILE_COUNT) return null;

		TileEffectType effect = _effectsCurrentTurn[tileIndex];
		const int healAmount = 5;
		const int damageAmount = 5;
		string appliedName = null;

		switch (effect)
		{
			case TileEffectType.Positive:
				if (_positivePool.Count > 0)
				{
					int idx = DeterministicIndex(turnNumber, tileIndex, _positivePool.Count);
					var eff = _positivePool[idx];
					appliedName = eff != null ? eff.Name : null;
					eff?.Execute(caster, target);
				}
				else
				{
					target.Heal(healAmount);
					appliedName = "Heal5";
				}
				break;
			case TileEffectType.Negative:
				if (_negativePool.Count > 0)
				{
					int idxN = DeterministicIndex(turnNumber, tileIndex, _negativePool.Count);
					var effN = _negativePool[idxN];
					appliedName = effN != null ? effN.Name : null;
					effN?.Execute(caster, target);
				}
				else
				{
					target.TakeDamage(damageAmount);
					appliedName = "Damage5";
				}
				break;
		}
		return appliedName;
	}

	private static int DeterministicIndex(int turnNumber, int tileIndex, int count)
		{
			if (count <= 0) return 0;
			int seed = turnNumber * 73856093 ^ tileIndex * 19349663;
			if (seed == 0) seed = 17;
			var rng = new System.Random(seed);
			return rng.Next(0, count);
		}

		public void RandomizeVisualMapping(System.Random rng)
		{
			if (rng == null) rng = new System.Random();
			for (int i = 0; i < TILE_COUNT; i++)
			{
				int v = rng.Next(0, 3); // 0=Neutral,1=Positive,2=Negative
				_effectsCurrentTurn[i] = (TileEffectType)v;
			}
		}
	}
}


