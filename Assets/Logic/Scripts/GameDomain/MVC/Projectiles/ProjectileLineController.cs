using Logic.Scripts.GameDomain.MVC.Abilitys;
using UnityEngine;

public class ProjectileLineController : ProjectileController {
    public override void ManagedFixedUpdate() {
    }

    private void OnTriggerEnter(Collider other) {
        if (other.TryGetComponent<IEffectable>(out IEffectable target)) {
            IPlotTwistData plotData = Data.PlotData as IPlotTwistData;
            foreach (AbilityEffect effect in plotData.Effects) {
                effect.Execute(Data, Caster, target);
            }
        }
    }
}
