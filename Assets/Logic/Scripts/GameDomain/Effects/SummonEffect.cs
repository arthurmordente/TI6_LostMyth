using System;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using UnityEngine;

[Serializable]
public class SummonEffect : AbilityEffect {
    [HideInInspector] public Vector3 _spawnPoint;

    public override void SetUp(Vector3 point) {
        base.SetUp(point);
        _spawnPoint = point;
    }

    public override void Execute(AbilityData data, IEffectable caster) {
        
    }
}
