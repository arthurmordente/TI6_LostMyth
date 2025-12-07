using Logic.Scripts.GameDomain.MVC.Abilitys;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AoeTargeting : TargetingStrategy {
    public LayerMask GroundLayerMask;
    public LayerMask HittableLayerMask;
    public float Radius;

    private GameObject previewInstance;

    public override void Initialize(AbilityData data, IEffectable caster) {
        base.Initialize(data, caster);
        AoePlotTwistData plotData = data.PlotData as AoePlotTwistData;
        if (plotData != null && plotData.AoePrefab != null) {
            previewInstance = GameObject.Instantiate(plotData.AoePrefab, new Vector3(0f, 0.1f, 0f), Quaternion.identity);
        }
        SubscriptionService.RegisterUpdatable(this);
    }

    public override void ManagedUpdate() {
        //To-Do Alterar para new input system
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, float.MaxValue, GroundLayerMask)) {
            Aim(hit);
        }
    }

    private void Aim(RaycastHit hit) {
        if (previewInstance != null) {
            Vector3 casterOrigin = Caster.GetReferenceTransform().position;
            Vector3 startPos = Caster.GetTransformCastPoint().position;
            Vector3 directionFromCaster = hit.point - casterOrigin;

            if (Physics.Raycast(Caster.GetTransformCastPoint().position, directionFromCaster, out RaycastHit hitFromCastPoint, float.MaxValue, HittableLayerMask)) {
                hit = hitFromCastPoint;
            }

            Vector3 directionFromCasterXZ = new Vector3(hit.point.x - casterOrigin.x, 0, hit.point.z - casterOrigin.z);

            float distanceXZ = directionFromCasterXZ.magnitude;

            Vector3 finalAimDirection;
            finalAimDirection = directionFromCasterXZ.normalized;

            if (finalAimDirection.sqrMagnitude > 0.001f) {
                Caster.GetTransformCastPoint().rotation = Quaternion.LookRotation(finalAimDirection);
            }

            float distance = directionFromCaster.magnitude;
            Vector3 clampedTargetPoint;
            if (distance > Ability.GetRange()) {
                clampedTargetPoint = casterOrigin + (directionFromCaster.normalized * Ability.GetRange());
            }
            else if (distance < 0.5f) {
                clampedTargetPoint = casterOrigin + (directionFromCaster.normalized * 0.5f);
            }
            else {
                clampedTargetPoint = hit.point;
            }
            previewInstance.transform.position = new Vector3(clampedTargetPoint.x, (clampedTargetPoint.y + 0.15f), clampedTargetPoint.z);
            finalAimDirection.y = 0f;
            Caster.GetReferenceTransform().rotation = Quaternion.LookRotation(finalAimDirection.normalized);
        }
    }

    public override void Cancel() {
        if (previewInstance != null) {
            UnityEngine.Object.Destroy(previewInstance);
        }
        base.Cancel();
    }

    public override Vector3 LockAim(out IEffectable[] targets) {
        //To-Do Alterar para new input system
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, float.MaxValue, GroundLayerMask)) {
            Collider[] colliders = Physics.OverlapSphere(hit.point, (Radius));
            List<IEffectable> targetsList = new List<IEffectable>();
            foreach (Collider collider in colliders) {
                if (collider.TryGetComponent<IEffectable>(out IEffectable effectable)) {
                    targetsList.Add(effectable);
                }
            }
            targets = targetsList.ToArray();
            Cancel();
            return hit.point;
        }
        else {
            Cancel();
            return base.LockAim(out targets);
        }
    }
}
