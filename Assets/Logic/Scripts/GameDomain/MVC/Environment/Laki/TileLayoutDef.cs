using System;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
	/// <summary>Identifies which of the four effect pools a slot in a layout draws from.</summary>
	public enum EffectPoolType
	{
		LargePositive,
		SmallPositive,
		LargeNegative,
		SmallNegative,
	}

	/// <summary>One slot inside an EffectLayoutDef – specifies which pool to draw one effect from.</summary>
	[Serializable]
	public struct EffectSlotRef
	{
		public EffectPoolType Pool;
	}

	/// <summary>
	/// One possible combination of effect slots with a relative probability weight.
	/// Example: Weight=50, Slots=[SmallPositive] → "50% chance of one small positive".
	/// All weights in a TileTypeLayoutConfig are summed to compute probabilities.
	/// </summary>
	[Serializable]
	public class EffectLayoutDef
	{
		[Range(0f, 100f)]
		[Tooltip("Relative probability weight. Proportion = this / sum of all weights in the config.")]
		public float Weight = 1f;

		[Tooltip("One entry per effect slot. Each slot draws independently from its pool.")]
		public EffectSlotRef[] Slots;
	}

	/// <summary>
	/// Weighted collection of EffectLayoutDefs that applies to one tile colour
	/// (positive / negative / neutral).
	/// </summary>
	[Serializable]
	public class TileTypeLayoutConfig
	{
		public EffectLayoutDef[] Layouts;
	}
}
