using System;
using System.Collections.Generic;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Abilitys;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	public sealed class RouletteArenaService
	{
		// ─── Effect pools ─────────────────────────────────────────────────────────
		private readonly List<AbilityEffect> _largePositivePool = new List<AbilityEffect>(8);
		private readonly List<AbilityEffect> _smallPositivePool = new List<AbilityEffect>(8);
		private readonly List<AbilityEffect> _largeNegativePool = new List<AbilityEffect>(8);
		private readonly List<AbilityEffect> _smallNegativePool = new List<AbilityEffect>(8);

		// ─── Layout configs per tile colour ───────────────────────────────────────
		private TileTypeLayoutConfig _positiveLayoutConfig;
		private TileTypeLayoutConfig _neutralLayoutConfig;
		private TileTypeLayoutConfig _negativeLayoutConfig;

		// ─── Pre-assigned effects per tile (resolved on RerollTiles) ──────────────
		private AbilityEffect[][] _assignedEffects = new AbilityEffect[TILE_COUNT][];

		public enum TileEffectType { Neutral = 0, Positive = 1, Negative = 2 }

		public const float INNER_RADIUS_DEFAULT = 6f;
		public const float OUTER_RADIUS_DEFAULT = 12f;

		private const int SECTOR_COUNT  = 8;
		private const int RADIAL_BANDS  = 2;
		private const int TILE_COUNT    = SECTOR_COUNT * RADIAL_BANDS;

		private readonly float _innerRadius;
		private readonly float _outerRadius;
		private readonly float _radialSplit01;
		private readonly float _sectorAngleRad;
		private readonly float _arcStartRad;
		private readonly float _arcRad;

		private int             _lastRolledTurn    = int.MinValue;
		private TileEffectType[] _effectsCurrentTurn = new TileEffectType[TILE_COUNT];

		public RouletteArenaService(
			float innerRadius   = INNER_RADIUS_DEFAULT,
			float outerRadius   = OUTER_RADIUS_DEFAULT,
			float radialSplit01 = 0.6f,
			float arcStartDeg   = 0f,
			float arcDeg        = 180f)
		{
			_innerRadius     = Mathf.Max(0.01f, Mathf.Min(innerRadius, outerRadius * 0.999f));
			_outerRadius     = Mathf.Max(_innerRadius + 0.01f, outerRadius);
			_radialSplit01   = Mathf.Clamp01(radialSplit01);
			_arcStartRad     = arcStartDeg * Mathf.Deg2Rad;
			_arcRad          = Mathf.Clamp(arcDeg, 1f, 360f) * Mathf.Deg2Rad;
			_sectorAngleRad  = _arcRad / SECTOR_COUNT;

			for (int i = 0; i < TILE_COUNT; i++)
			{
				_effectsCurrentTurn[i] = TileEffectType.Neutral;
				_assignedEffects[i]    = Array.Empty<AbilityEffect>();
			}
		}

		public int   TileCount   => TILE_COUNT;
		public float InnerRadius => _innerRadius;
		public float OuterRadius => _outerRadius;
		public float SplitRadius => _innerRadius + _radialSplit01 * (_outerRadius - _innerRadius);

		// ─── Configuration ────────────────────────────────────────────────────────

		/// <summary>
		/// Configures the four effect pools and the weighted layout table for each tile colour.
		/// Call this before the first RerollTiles.
		/// </summary>
		public void SetLayoutConfigs(
			TileTypeLayoutConfig positiveConfig,
			TileTypeLayoutConfig neutralConfig,
			TileTypeLayoutConfig negativeConfig,
			IList<AbilityEffect> largePositive,
			IList<AbilityEffect> smallPositive,
			IList<AbilityEffect> largeNegative,
			IList<AbilityEffect> smallNegative)
		{
			_positiveLayoutConfig = positiveConfig;
			_neutralLayoutConfig  = neutralConfig;
			_negativeLayoutConfig = negativeConfig;

			_largePositivePool.Clear(); if (largePositive != null) _largePositivePool.AddRange(largePositive);
			_smallPositivePool.Clear(); if (smallPositive != null) _smallPositivePool.AddRange(smallPositive);
			_largeNegativePool.Clear(); if (largeNegative != null) _largeNegativePool.AddRange(largeNegative);
			_smallNegativePool.Clear(); if (smallNegative != null) _smallNegativePool.AddRange(smallNegative);
		}

		// ─── Tile rolling ─────────────────────────────────────────────────────────

		public void RerollTiles(int turnNumber, System.Random rng)
		{
			if (turnNumber == _lastRolledTurn) return;
			if (rng == null) rng = new System.Random();

			// Assign tile colour types (bag shuffle)
			int positives = 5;
			int negatives = 6;
			int neutrals  = TILE_COUNT - positives - negatives;

			var bag = new List<TileEffectType>(TILE_COUNT);
			for (int i = 0; i < positives; i++) bag.Add(TileEffectType.Positive);
			for (int i = 0; i < negatives; i++) bag.Add(TileEffectType.Negative);
			for (int i = 0; i < neutrals;  i++) bag.Add(TileEffectType.Neutral);

			for (int i = bag.Count - 1; i > 0; i--)
			{
				int j = rng.Next(i + 1);
				(bag[i], bag[j]) = (bag[j], bag[i]);
			}

			for (int i = 0; i < TILE_COUNT; i++) _effectsCurrentTurn[i] = bag[i];

			// Pre-assign specific effects per tile using a per-tile RNG seed.
			// This avoids same-layout streaks across tiles of the same colour.
			for (int i = 0; i < TILE_COUNT; i++)
			{
				int seed = (turnNumber + 1) * 73856093
					^ (i + 1) * 19349663
					^ ((int)_effectsCurrentTurn[i] + 1) * 83492791
					^ rng.Next();
				if (seed == 0) seed = 17;
				var tileRng = new System.Random(seed);
				_assignedEffects[i] = ResolveEffectsForTile(_effectsCurrentTurn[i], tileRng);
			}

			_lastRolledTurn = turnNumber;
		}

		/// <summary>Returns the pre-assigned AbilityEffects for a tile (resolved at roll time).</summary>
		public AbilityEffect[] GetTileAssignedEffects(int tileIndex)
		{
			if (tileIndex < 0 || tileIndex >= TILE_COUNT) return null;
			return _assignedEffects[tileIndex];
		}

		public TileEffectType GetTileEffect(int tileIndex)
		{
			if (tileIndex < 0 || tileIndex >= TILE_COUNT) return TileEffectType.Neutral;
			return _effectsCurrentTurn[tileIndex];
		}

		// ─── Effect application ───────────────────────────────────────────────────

		/// <summary>Applies all pre-assigned effects for this tile to the player.</summary>
		public string ApplyEffectToPlayer(IEffectable caster, INaraController nara, int tileIndex, int turnNumber)
		{
			if (nara == null || tileIndex < 0 || tileIndex >= TILE_COUNT) return null;

			var effects = _assignedEffects[tileIndex];
			if (effects == null || effects.Length == 0)
				return ApplyFallbackToPlayer(caster, nara, tileIndex);

			var asEffectable = nara as IEffectable;
			var names = new List<string>(effects.Length);
			foreach (var e in effects)
			{
				if (e == null) continue;
				e.Execute(caster, asEffectable);
				names.Add(e.Name ?? "");
			}
			return string.Join(", ", names);
		}

		/// <summary>Applies all pre-assigned effects for this tile to any IEffectable (e.g. the Book).</summary>
		public string ApplyEffectToEffectable(IEffectable caster, IEffectable target, int tileIndex, int turnNumber)
		{
			if (target == null || tileIndex < 0 || tileIndex >= TILE_COUNT) return null;

			var effects = _assignedEffects[tileIndex];
			if (effects == null || effects.Length == 0)
				return ApplyFallbackToEffectable(caster, target, tileIndex);

			var names = new List<string>(effects.Length);
			foreach (var e in effects)
			{
				if (e == null) continue;
				e.Execute(caster, target);
				names.Add(e.Name ?? "");
			}
			return string.Join(", ", names);
		}

		// ─── Visual scramble ──────────────────────────────────────────────────────

		/// <summary>
		/// Randomises tile colour types for visual animation and re-resolves tile effects,
		/// so tile canvases update while reroll animation is running.
		/// </summary>
		public void RandomizeVisualMapping(System.Random rng)
		{
			if (rng == null) rng = new System.Random();
			for (int i = 0; i < TILE_COUNT; i++)
			{
				_effectsCurrentTurn[i] = (TileEffectType)rng.Next(0, 3);
				_assignedEffects[i] = ResolveEffectsForTile(_effectsCurrentTurn[i], rng);
			}
		}

		// ─── Spatial query ────────────────────────────────────────────────────────

		public int ComputeTileIndex(Vector3 worldPos, Vector3 centerWorld)
		{
			Vector2 rel = new Vector2(worldPos.x - centerWorld.x, worldPos.z - centerWorld.z);
			float r = rel.magnitude;
			if (r < _innerRadius || r > _outerRadius) return -1;

			float theta = Mathf.Atan2(rel.y, rel.x);
			if (theta < 0f) theta += 2f * Mathf.PI;

			float relTheta = theta - _arcStartRad;
			if (relTheta < 0f) relTheta += 2f * Mathf.PI;
			if (relTheta >= _arcRad) return -1;

			int sectorIndex = Mathf.Clamp(Mathf.FloorToInt(relTheta / _sectorAngleRad), 0, SECTOR_COUNT - 1);
			int band = r < SplitRadius ? 0 : 1;
			return sectorIndex * RADIAL_BANDS + band;
		}

		// ─── Private helpers ──────────────────────────────────────────────────────

		private AbilityEffect[] ResolveEffectsForTile(TileEffectType type, System.Random rng)
		{
			var config = type switch
			{
				TileEffectType.Positive => _positiveLayoutConfig,
				TileEffectType.Negative => _negativeLayoutConfig,
				_                       => _neutralLayoutConfig,
			};

			if (config?.Layouts == null || config.Layouts.Length == 0)
				return Array.Empty<AbilityEffect>();

			var layout = PickWeightedLayout(config.Layouts, rng);
			if (layout?.Slots == null || layout.Slots.Length == 0)
				return Array.Empty<AbilityEffect>();

			var effects = new AbilityEffect[layout.Slots.Length];
			for (int s = 0; s < layout.Slots.Length; s++)
			{
				var pool = GetPool(layout.Slots[s].Pool);
				if (pool != null && pool.Count > 0)
					effects[s] = pool[rng.Next(pool.Count)];
			}
			return effects;
		}

		private List<AbilityEffect> GetPool(EffectPoolType pool) => pool switch
		{
			EffectPoolType.LargePositive => _largePositivePool,
			EffectPoolType.SmallPositive => _smallPositivePool,
			EffectPoolType.LargeNegative => _largeNegativePool,
			_                            => _smallNegativePool,
		};

		private static EffectLayoutDef PickWeightedLayout(EffectLayoutDef[] layouts, System.Random rng)
		{
			float total = 0f;
			foreach (var l in layouts) if (l != null) total += Mathf.Max(0f, l.Weight);
			if (total <= 0f) return layouts[0];

			float r = (float)rng.NextDouble() * total;
			float cumulative = 0f;
			foreach (var l in layouts)
			{
				if (l == null) continue;
				cumulative += Mathf.Max(0f, l.Weight);
				if (r <= cumulative) return l;
			}
			return layouts[layouts.Length - 1];
		}

		// Fallbacks used when no layout is configured
		private string ApplyFallbackToPlayer(IEffectable caster, INaraController nara, int tileIndex)
		{
			var target = nara as IEffectable;
			switch (_effectsCurrentTurn[tileIndex])
			{
				case TileEffectType.Positive:
					target?.Heal(5);
					(nara as IEffectableAction)?.AddActionPoints(1);
					return "Heal5_AP+1";
				case TileEffectType.Negative:
					target?.TakeDamage(5);
					(nara as IEffectableAction)?.SubtractActionPoints(1);
					return "Damage5_AP-1";
				default: return null;
			}
		}

		private string ApplyFallbackToEffectable(IEffectable caster, IEffectable target, int tileIndex)
		{
			switch (_effectsCurrentTurn[tileIndex])
			{
				case TileEffectType.Positive: target?.Heal(5);        return "Heal5";
				case TileEffectType.Negative: target?.TakeDamage(5);  return "Damage5";
				default: return null;
			}
		}
	}
}
