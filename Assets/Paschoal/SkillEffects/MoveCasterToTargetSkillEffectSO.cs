using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;

[CreateAssetMenu(fileName = "MoveCasterToTargetEffect", menuName = "ScriptableObjects/Skills/Effects/MoveCasterToTarget")]
public class MoveCasterToTargetSkillEffectSO : SkillEffectSO
{
    [SerializeField] private float _yOffset;

    public override void Execute(in SkillExecutionContext context)
    {
        if (context.Caster == null) return;
        Transform root = context.Caster.GetReferenceTransform();
        if (root == null) return;
        Vector3 destination = context.TargetPoint;
        destination.y += _yOffset;
        root.position = destination;
    }
}
