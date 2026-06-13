using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.Effects;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	public sealed class RouletteArenaService
	{
		public const int TileEffectStaggerMs = 500;
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

		/// <summary>Inside edge of the annulus (hole). Each band extends <see cref="TILE_RADIAL_DEPTH"/> outward from here (inner) or after the gap (outer).</summary>
		public const float INNER_RADIUS_DEFAULT = 8.5f;

		/// <summary>Radial length of inner and outer tile meshes (same as the cube scale reference 8.5).</summary>
		public const float TILE_RADIAL_DEPTH = 8.5f;

		/// <summary>Radial gap between inner and outer bands = this × outer radius (2.5% reserved for divisória).</summary>
		public const float BAND_DIVIDER_FRACTION_OF_OUTER = 0.025f;

		/// <summary>Default outer rim: <c>(hole + 2×<see cref="TILE_RADIAL_DEPTH"/>) / (1 − <see cref="BAND_DIVIDER_FRACTION_OF_OUTER"/>)</c>. Must stay a <c>const</c> for default ctor params.</summary>
		public const float OUTER_RADIUS_DEFAULT =
			(INNER_RADIUS_DEFAULT + 2f * TILE_RADIAL_DEPTH) / (1f - BAND_DIVIDER_FRACTION_OF_OUTER);

		/// <summary>Each tile mesh spans (arc/sectors)×this angle; the rest is left for angular dividers (0.95 ⇒ 5% removed from uniform opening).</summary>
		public const float TILE_ANGULAR_OPENING_KEEP = 0.95f;

		public static float BandDividerGap(float outerRadius) =>
			BAND_DIVIDER_FRACTION_OF_OUTER * Mathf.Max(0.01f, outerRadius);

		public static float OuterRadiusForHole(float holeInnerRadius) =>
			(holeInnerRadius + 2f * TILE_RADIAL_DEPTH) / (1f - BAND_DIVIDER_FRACTION_OF_OUTER);

		/// <summary>Mid-gap radius between inner and outer bands (for <c>r &lt; split</c> ⇒ inner tile).</summary>
		public static float ComputeSplitRadius(float holeInnerRadius, float outerRadius) =>
			holeInnerRadius + TILE_RADIAL_DEPTH + 0.5f * BandDividerGap(outerRadius);

		public static float ComputeTileAngularHalfGapDeg(float arcDeg, int sectorCount)
		{
			float slot = arcDeg / Mathf.Max(1, sectorCount);
			return 0.5f * slot * (1f - TILE_ANGULAR_OPENING_KEEP);
		}

		/// <param name="radialInset">Extra inset on both edges of the band (e.g. <c>_radialGap</c> on the view).</param>
		public static void ComputeBandRadialExtents(
			float holeInnerRadius,
			float outerRadius,
			int band,
			float radialInset,
			out float rMin,
			out float rMax)
		{
			float g = BandDividerGap(outerRadius);
			float rInnerEnd = holeInnerRadius + TILE_RADIAL_DEPTH;
			float inset = Mathf.Max(0f, radialInset);
			if (band == 0)
			{
				rMin = holeInnerRadius + inset;
				rMax = rInnerEnd - inset;
			}
			else
			{
				rMin = rInnerEnd + g + inset;
				rMax = outerRadius - inset;
			}
			if (rMax <= rMin) rMax = rMin + 0.005f;
		}

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
		private LakiArenaTileDisposition _tileDisposition = LakiArenaTileDisposition.Default;

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
		public float SplitRadius => ComputeSplitRadius(_innerRadius, _outerRadius);
		public LakiArenaTileDisposition CurrentTileDisposition => _tileDisposition;

		public void SetTileDisposition(LakiArenaTileDisposition disposition) =>
			_tileDisposition = disposition.NormalizeTo(TILE_COUNT);

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

		/// <summary>
		/// All tiles neutral, no assigned effects; <see cref="RerollTiles"/> will run again on the next call.
		/// Used at fight start so the board can stay visually blank until the first environment reveal.
		/// </summary>
		public void ResetTilesBlank()
		{
			for (int i = 0; i < TILE_COUNT; i++)
			{
				_effectsCurrentTurn[i] = TileEffectType.Neutral;
				_assignedEffects[i] = Array.Empty<AbilityEffect>();
			}
			_lastRolledTurn = int.MinValue;
		}

		public void RerollTiles(int turnNumber, System.Random rng)
		{
			if (turnNumber == _lastRolledTurn) return;
			if (rng == null) rng = new System.Random();

			// Assign tile colour types (bag shuffle)
			var disposition = _tileDisposition.NormalizeTo(TILE_COUNT);
			int positives = disposition.PositiveCount;
			int negatives = disposition.NegativeCount;
			int neutrals  = disposition.NeutralCount;

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
		public Task<string> ApplyEffectToPlayerAsync(IEffectable caster, INaraController nara, int tileIndex, int turnNumber) =>
			ApplyEffectToPlayerInternalAsync(caster, nara, tileIndex, turnNumber);

		/// <summary>Applies all pre-assigned effects for this tile to any IEffectable (e.g. the Book).</summary>
		public Task<string> ApplyEffectToEffectableAsync(IEffectable caster, IEffectable target, int tileIndex, int turnNumber) =>
			ApplyEffectToEffectableInternalAsync(caster, target, tileIndex, turnNumber);

		async Task<string> ApplyEffectToPlayerInternalAsync(IEffectable caster, INaraController nara, int tileIndex, int turnNumber)
		{
			if (nara == null || tileIndex < 0 || tileIndex >= TILE_COUNT)
			{
				LakiTileEffectApplyDebug.LogManaSkipped("Player", "Apply", $"nara={(nara != null)} tileIndex={tileIndex}");
				return null;
			}

			var tileType = _effectsCurrentTurn[tileIndex];
			var effects = _assignedEffects[tileIndex];
			bool usingFallback = effects == null || effects.Length == 0;
			LakiTileEffectApplyDebug.LogApplyStart("Player", turnNumber, tileIndex, tileType, effects?.Length ?? 0, usingFallback);

			if (usingFallback)
			{
				string fallback = ApplyFallbackToPlayer(caster, nara, tileIndex);
				LakiTileEffectApplyDebug.LogApplyComplete("Player", turnNumber, tileIndex, fallback);
				return fallback;
			}

			var names = new List<string>(effects.Length);
			for (int i = 0; i < effects.Length; i++)
			{
				if (i > 0)
					await Task.Delay(TileEffectStaggerMs);

				var e = effects[i];
				if (e == null)
				{
					Debug.LogWarning($"[LakiTile][Player] casa={tileIndex} efeito[{i}] é null — ignorado");
					continue;
				}

				LakiTileEffectApplyDebug.LogEffectStep("Player", tileIndex, i, e.GetType().Name, e.Name ?? "");
				if (TryApplyPlayerTileEffect(caster, nara, e, out string appliedLabel))
					names.Add(appliedLabel);
			}

			string summary = string.Join(", ", names);
			LakiTileEffectApplyDebug.LogApplyComplete("Player", turnNumber, tileIndex, summary);
			return summary;
		}

		async Task<string> ApplyEffectToEffectableInternalAsync(IEffectable caster, IEffectable target, int tileIndex, int turnNumber)
		{
			if (target == null || tileIndex < 0 || tileIndex >= TILE_COUNT) return null;

			const string unitLabel = "Clone";
			var tileType = _effectsCurrentTurn[tileIndex];
			var effects = _assignedEffects[tileIndex];
			bool usingFallback = effects == null || effects.Length == 0;
			LakiTileEffectApplyDebug.LogApplyStart(unitLabel, turnNumber, tileIndex, tileType, effects?.Length ?? 0, usingFallback);

			if (usingFallback)
			{
				string fallback = ApplyFallbackToEffectable(caster, target, tileIndex);
				LakiTileEffectApplyDebug.LogApplyComplete(unitLabel, turnNumber, tileIndex, fallback);
				return fallback;
			}

			var names = new List<string>(effects.Length);
			for (int i = 0; i < effects.Length; i++)
			{
				if (i > 0)
					await Task.Delay(TileEffectStaggerMs);

				var e = effects[i];
				if (e == null) continue;

				LakiTileEffectApplyDebug.LogEffectStep(unitLabel, tileIndex, i, e.GetType().Name, e.Name ?? "");
				e.Execute(caster, target);
				names.Add(e.Name ?? e.GetType().Name);
			}

			string summary = string.Join(", ", names);
			LakiTileEffectApplyDebug.LogApplyComplete(unitLabel, turnNumber, tileIndex, summary);
			return summary;
		}

		[Obsolete("Use ApplyEffectToPlayerAsync for staggered application.")]
		public string ApplyEffectToPlayer(IEffectable caster, INaraController nara, int tileIndex, int turnNumber)
		{
			return ApplyEffectToPlayerAsync(caster, nara, tileIndex, turnNumber).GetAwaiter().GetResult();
		}

		[Obsolete("Use ApplyEffectToEffectableAsync for staggered application.")]
		public string ApplyEffectToEffectable(IEffectable caster, IEffectable target, int tileIndex, int turnNumber)
		{
			return ApplyEffectToEffectableAsync(caster, target, tileIndex, turnNumber).GetAwaiter().GetResult();
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
		static bool TryApplyPlayerTileEffect(IEffectable caster, INaraController nara, AbilityEffect e, out string appliedLabel)
		{
			appliedLabel = null;
			if (e == null) return false;

			e.Execute(caster, nara);
			appliedLabel = e.Name ?? e.GetType().Name;
			return true;
		}

		private string ApplyFallbackToPlayer(IEffectable caster, INaraController nara, int tileIndex)
		{
			var target = nara as IEffectable;
			switch (_effectsCurrentTurn[tileIndex])
			{
				case TileEffectType.Positive:
					target?.Heal(5);
					if (nara is IEffectableAction positiveAction)
						positiveAction.AddActionPoints(1);
					return "Heal5_AP+1";
				case TileEffectType.Negative:
					target?.TakeDamage(5);
					if (nara is IEffectableAction negativeAction)
						negativeAction.SubtractActionPoints(1);
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
