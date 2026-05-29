using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(
    fileName = "DamageStackMovementPassiveBehavior",
    menuName = "ScriptableObjects/Skills/Passive/Damage Stack Movement")]
public class DamageStackMovementPassiveBehaviorSO : PassiveOnDamageTakenBehaviorSO
{
    [SerializeField] private float _movementRadiusMultiplierPerStack = 1.2f;

    public override float MovementRadiusMultiplierPerStack =>
        Mathf.Max(1f, _movementRadiusMultiplierPerStack);

    private void OnValidate()
    {
        if (_movementRadiusMultiplierPerStack < 1f)
            _movementRadiusMultiplierPerStack = 1f;
    }
}
