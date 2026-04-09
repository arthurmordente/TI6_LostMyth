using System.Collections.Generic;
using UnityEngine;

public class AoE_Effect : MonoBehaviour
{
    public EffectSO effect;
    public List<IEffectable> effectables;

    private void OnTriggerEnter(Collider other)
    {
       if(other.TryGetComponent<IEffectable>(out IEffectable e))
        {
            effectables.Add(e);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        {
            if (other.TryGetComponent<IEffectable>(out IEffectable e))
            {
                effectables.Remove(e);
            }
        }
    }
    public void DoStuf()
    {
        foreach (IEffectable e in effectables)
        {
            effect.DoStuff(e);
        }
    }
}
