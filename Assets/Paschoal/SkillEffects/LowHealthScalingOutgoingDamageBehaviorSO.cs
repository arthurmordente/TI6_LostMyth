using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LowHealthScalingOutgoingDamageBehavior",
    menuName = "ScriptableObjects/Skills/Passive/Low Health Scaling Outgoing Damage")]
public class LowHealthScalingOutgoingDamageBehaviorSO : PassiveCombatBehaviorSO
{
    [SerializeField] private float _multiplierAtFullHealth = 1f;
    [SerializeField] private float _multiplierAtLowHealth = 2f;
    [Tooltip("At or below this HP ratio (actual/max), outgoing damage uses Multiplier At Low Health.")]
    [SerializeField] private float _healthRatioForMaxBonus = 0.25f;

    public override float ComputeOutgoingDamageMultiplier(float healthRatio)
    {
        healthRatio = Mathf.Clamp01(healthRatio);
        float threshold = Mathf.Clamp01(_healthRatioForMaxBonus);

        if (healthRatio <= threshold)
            return Mathf.Max(0f, _multiplierAtLowHealth);
        if (healthRatio >= 1f)
            return Mathf.Max(0f, _multiplierAtFullHealth);

        float t = (1f - healthRatio) / Mathf.Max(0.0001f, 1f - threshold);
        return Mathf.Lerp(_multiplierAtFullHealth, _multiplierAtLowHealth, t);
    }

    private void OnValidate()
    {
        if (_healthRatioForMaxBonus <= 0f) _healthRatioForMaxBonus = 0.25f;
        if (_healthRatioForMaxBonus > 1f) _healthRatioForMaxBonus = 1f;
        if (_multiplierAtLowHealth < _multiplierAtFullHealth)
            _multiplierAtLowHealth = _multiplierAtFullHealth;
    }
}
