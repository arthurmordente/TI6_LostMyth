using UnityEngine;

public class Effects
{
    public int count;
    public EffectSO effect, secondaryEffect;
    public bool repetitiveEffect, firstTime = true;
    public Effects(EffectSO effect)
    {
        count = effect.duration;
        this.effect = effect;
        secondaryEffect = effect.secondaryEffect;
        repetitiveEffect = effect.repetitive;
    }
    public bool DoStuf(IEffectable effectable)
    {  
        if(repetitiveEffect)
        {
            effect.DoStuff(effectable);
        }
        else if (firstTime)
        {
            effect.DoStuff(effectable);
            firstTime = false;
        }
        count--;
        if (count <= 0)
        {
            if(secondaryEffect != null)secondaryEffect.DoStuff(effectable);
            return true;
        }
        return false;
    }
}
