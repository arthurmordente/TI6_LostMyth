using UnityEngine;

public abstract class EffectSO : ScriptableObject
{
    public int duration;
    public int power;
    public bool repetitive;
    public EffectSO secondaryEffect;

    public abstract void DoStuff(IEffectable effectable);
}
