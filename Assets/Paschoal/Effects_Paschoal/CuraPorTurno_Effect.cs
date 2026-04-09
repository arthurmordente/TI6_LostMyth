using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Effects/CuraPorTurno")]
public class CuraPorTurno_Effect : EffectSO
{
    public override void DoStuff(IEffectable effectable)
    {
        effectable.Heal(power);
    }
}
