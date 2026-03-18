using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/Faca", order = 2)]
public class Faca : SkillDataSO
{
    public override void OnCast(IEffectable caster, Transform target)
    {
        Instantiate(AttackPrefab, target.position, target.rotation);
    }
}

