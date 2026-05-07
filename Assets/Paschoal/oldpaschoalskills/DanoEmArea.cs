using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using UnityEngine;
using Logic.Scripts.GameDomain.Services.Skills;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/BolaDeFogo", order = 2)]
public class DanoEmArea : SkillDataSO
{
    Collider[] colliders;

    protected override SkillCastMode GetDefaultCastMode()
    {
        return SkillCastMode.Area;
    }

    public override void OnCast(IEffectable caster, Transform target)
    {
        Vector3 center = target.transform.position;
        colliders = Physics.OverlapSphere(center, GetAreaRadius());
        foreach (Collider col in colliders)
        {
            var f = col.GetComponentInParent<IEffectable>();
            if (f != null) {
                if (f is DiceActor dice)
                    Debug.Log($"[Paschoal][DanoEmArea→DiceActor] {dice.name} power={Power} sphereCenter={center} r={AreaOfEffect}");
                f.TakeDamage(Power);
                f.PreviewDamage(Power);
            }
        }
    }
}
