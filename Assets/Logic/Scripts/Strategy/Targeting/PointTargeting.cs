using Logic.Scripts.GameDomain.MVC.Abilitys;
using UnityEngine;

public class PointTargeting : TargetingStrategy {
    public LayerMask GroundLayerMask;
    private Transform _previewTransform;
    public override void Initialize(AbilityData data, IEffectable caster) {
        base.Initialize(data, caster);
        SubscriptionService.RegisterUpdatable(this);
        PointPlotTwistData plotData = data.PlotData as PointPlotTwistData;
        if (plotData != null && plotData.ObjectToSummon != null) {
            _previewTransform = Object.Instantiate(plotData.ObjectToSummon.VisualRoot).transform;
        }
    }
    public override void ManagedUpdate() {
        base.ManagedUpdate();
        if (_previewTransform != null) {
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, float.MaxValue, GroundLayerMask)) {
                _previewTransform.transform.position = hit.point;
            }

            Vector3 directionToLook = _previewTransform.transform.position - Caster.GetReferenceTransform().position;
            Vector3 previewDirectionToLook = Caster.GetReferenceTransform().position - _previewTransform.transform.position;

            if (directionToLook != Vector3.zero) {
                Caster.GetReferenceTransform().rotation = Quaternion.LookRotation(directionToLook);
                _previewTransform.rotation = Quaternion.LookRotation(directionToLook);
            }
        }
    }

    public override Vector3 LockAim(out IEffectable[] targets) {
        base.LockAim(out targets);
        PointPlotTwistData plotData = Ability.PlotData as PointPlotTwistData;
        AbilitySummon summonObject = Object.Instantiate(plotData.ObjectToSummon, _previewTransform.position, _previewTransform.rotation);
        CommandFactory.CreateCommandVoid<SummonSkillCommand>().SetData(new SummonSkillCommandData(summonObject)).Execute();
        summonObject.SetUp(plotData.Duration, plotData.HealAmount, Caster);
        return _previewTransform.position;
    }

    public override void Cancel() {
        base.Cancel();
        UnityEngine.Object.Destroy(_previewTransform.gameObject);
    }
}
