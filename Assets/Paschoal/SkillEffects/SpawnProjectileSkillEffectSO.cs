using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

/// <summary>
/// Optional asset: same spawn as <see cref="SkillProjectileSpawn.ExecuteSpawn"/> for non-declarative pipelines or extra ordering.
/// Declarative projectile skills call <see cref="SkillProjectileSpawn"/> directly; runner skips duplicate <see cref="ISpawnProjectileSkillEffect"/>.
/// </summary>
[CreateAssetMenu(fileName = "SpawnProjectileEffect", menuName = "ScriptableObjects/Skills/Effects/SpawnProjectile")]
public class SpawnProjectileSkillEffectSO : SkillEffectSO, ISpawnProjectileSkillEffect
{
    public override void Execute(in SkillExecutionContext context) => SkillProjectileSpawn.ExecuteSpawn(in context);
}
