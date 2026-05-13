using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/Faca", order = 2)]
public class Faca : SkillDataSO
{
    public override void OnCast(IEffectable caster, Transform target)
    {
        if (ProjectilePrefab == null || target == null) return;

        Vector3 origin;
        if (caster != null && caster.GetTransformCastPoint() != null)
            origin = caster.GetTransformCastPoint().position;
        else if (caster != null && caster.GetReferenceTransform() != null)
            origin = caster.GetReferenceTransform().position;
        else
            origin = target.position;

        Vector3 dir = target.position - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-8f && caster != null && caster.GetReferenceTransform() != null) {
            dir = Vector3.ProjectOnPlane(caster.GetReferenceTransform().forward, Vector3.up);
        }
        if (dir.sqrMagnitude < 1e-8f) dir = Vector3.forward;
        Object.Instantiate(ProjectilePrefab, origin, Quaternion.LookRotation(dir.normalized, Vector3.up));
    }
}
