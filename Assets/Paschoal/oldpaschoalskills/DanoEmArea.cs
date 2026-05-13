using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Skills/BolaDeFogo", order = 2)]
public class DanoEmArea : SkillDataSO
{
    Collider[] colliders;

    public override void OnCast(IEffectable caster, Transform target)
    {
        Vector3 center = target.transform.position;
        float r = GetAreaRadius();
        colliders = Physics.OverlapSphere(center, r);
        foreach (Collider col in colliders)
        {
            var f = col.GetComponentInParent<IEffectable>();
            if (f != null) {
                if (f is DiceActor dice)
                    Debug.Log($"[NewSkillSystemLegacy][DanoEmArea→DiceActor] {dice.name} power={Power} sphereCenter={center} r={r}");
                f.TakeDamage(Power);
                f.PreviewDamage(Power);
            }
        }
    }
}
