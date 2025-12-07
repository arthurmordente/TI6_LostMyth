using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Nara;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public enum AimingMode {
    StraightAim,
    ParabolicArc
}

[Serializable]
public class ProjectileTargeting : TargetingStrategy {
    [HideInInspector] public Transform hitMarker;
    public int maxPoints = 50;
    public float increment = 0.025f;
    public float rayOverlap = 1.1f;
    private LineRenderer trajectoryLine;
    public AimingMode aimingMode = AimingMode.StraightAim;
    public LayerMask GroundLayerMask;

    private float currentLaunchSpeed;
    private ProjectilePlotTwistData _projectilePlotData;

    public override void Initialize(AbilityData data, IEffectable caster) {
        base.Initialize(data, caster);
        _projectilePlotData = data.PlotData as ProjectilePlotTwistData;
        if (_projectilePlotData != null && _projectilePlotData.ProjectilePrefab != null) {
            if (Caster is NaraController) {
                NaraController controller = (NaraController)Caster;
                trajectoryLine = controller.GetPointLineRenderer();
            }
            hitMarker = UnityEngine.Object.Instantiate(Caster.GetReferenceTargetPrefab()).transform;
            SetTrajectoryVisible(true);
            currentLaunchSpeed = _projectilePlotData.ProjectilePrefab.InitialSpeed;
        }
        Caster.GetTransformCastPoint().rotation = Quaternion.identity;
        Caster.GetReferenceTransform().rotation = Quaternion.identity;
        SubscriptionService.RegisterUpdatable(this);
    }

    public override void ManagedUpdate() {
        base.ManagedUpdate();

        UpdateNaraViewRotation();

        PredictTrajectory();
    }

    private void UpdateNaraViewRotation() {
        if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out RaycastHit hit, float.MaxValue, GroundLayerMask)) {
            if (Vector3.Distance(hit.point, Caster.GetReferenceTransform().position) > _projectilePlotData.ParabolicMinRange) {
                switch (aimingMode) {
                    case AimingMode.StraightAim:
                        AimStraight(hit.point);
                        break;

                    case AimingMode.ParabolicArc:
                        AimParabolic(hit.point);
                        break;
                }
            }
        }
    }

    private void AimStraight(Vector3 targetPoint) {
        Transform referenceTransform = Caster.GetReferenceTransform();
        Vector3 casterOrigin = referenceTransform.position;
        Vector3 lookTargetXZ = targetPoint;
        Vector3 directionToTarget = lookTargetXZ - casterOrigin;
        if (directionToTarget.sqrMagnitude > 0) {
            directionToTarget.y = 0f;
            Caster.GetReferenceTransform().rotation = Quaternion.LookRotation(directionToTarget);
            directionToTarget.y = Caster.GetTransformCastPoint().rotation.y;
            Caster.GetTransformCastPoint().rotation = Quaternion.LookRotation(directionToTarget);
        }
    }

    private void AimParabolic(Vector3 targetPoint) {
        Vector3 startPos = Caster.GetTransformCastPoint().position;
        float g = Physics.gravity.y * -1;

        Vector3 casterOrigin = Caster.GetReferenceTransform().position;
        Vector3 directionFromCaster = targetPoint - casterOrigin;
        float distance = directionFromCaster.magnitude;

        Vector3 clampedTargetPoint;
        if (distance > Ability.GetRange()) {
            clampedTargetPoint = casterOrigin + (directionFromCaster.normalized * Ability.GetRange());
        }
        else {
            clampedTargetPoint = targetPoint;
        }

        Vector3 deltaXZ_vec = new Vector3(clampedTargetPoint.x - startPos.x, 0, clampedTargetPoint.z - startPos.z);
        float deltaX = deltaXZ_vec.magnitude;
        float deltaY = clampedTargetPoint.y - startPos.y;

        float distanceRatio = Mathf.Clamp01(deltaX / _projectilePlotData.ParabolicMaxRange);
        float h;
        if (Vector3.Distance(clampedTargetPoint, startPos) < _projectilePlotData.ParabolicMinRange) h = _projectilePlotData.ParabolicMaxHeight * distanceRatio;
        else h = _projectilePlotData.ParabolicMaxHeight;
        if (h < 0.1f) h = 0.1f;

        float Vy = Mathf.Sqrt(2 * g * h);

        float a = 0.5f * g;
        float b = -Vy;
        float c = deltaY;

        float discriminant = (b * b) - (4 * a * c);

        if (discriminant < 0) {
            AimStraight(clampedTargetPoint);
            return;
        }

        float t_total = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        if (t_total <= 0) {
            AimStraight(clampedTargetPoint);
            return;
        }

        float Vx = deltaX / t_total;

        Vector3 launchVelocity = (deltaXZ_vec.normalized * Vx) + (Vector3.up * Vy);
        Vector3 CasterLook = launchVelocity;
        CasterLook.y = 0f;

        Caster.GetReferenceTransform().rotation = Quaternion.LookRotation(CasterLook.normalized);
        Caster.GetTransformCastPoint().rotation = Quaternion.LookRotation(launchVelocity.normalized);

        currentLaunchSpeed = launchVelocity.magnitude;
    }

    public override Vector3 LockAim(out IEffectable[] targets) {
        base.LockAim(out targets);
        if (_projectilePlotData != null && _projectilePlotData.ProjectilePrefab != null) {
            Rigidbody thrownObject = UnityEngine.Object.Instantiate(_projectilePlotData.ProjectilePrefab, Caster.GetTransformCastPoint().position, Quaternion.identity).GetComponent<Rigidbody>();
            if (thrownObject.TryGetComponent<ProjectileController>(out ProjectileController controller)) {
                controller.Initialize(Caster.GetTransformCastPoint(), Caster, Ability);
            }
            thrownObject.AddForce(Caster.GetTransformCastPoint().forward * currentLaunchSpeed, ForceMode.Impulse);
            return Caster.GetTransformCastPoint().position;
        }
        else {
            return Vector3.zero;
        }
    }

    public override void Cancel() {
        SetTrajectoryVisible(false);
        UnityEngine.Object.Destroy(hitMarker.gameObject);
        base.Cancel();
    }

    #region TrajectoryPrediction_Helpers
    private void UpdateLineRender(int count, (int point, Vector3 pos) pointPos) {
        if (trajectoryLine == null) return;
        trajectoryLine.positionCount = count;
        trajectoryLine.SetPosition(pointPos.point, pointPos.pos);
    }

    private Vector3 CalculateNewVelocity(Vector3 velocity, float drag, float increment) {
        velocity += Physics.gravity * increment;
        velocity *= Mathf.Clamp01(1f - drag * increment);
        return velocity;
    }

    private void MoveHitMarker(RaycastHit hit) {
        if (hitMarker == null) return;
        hitMarker.gameObject.SetActive(true);

        float offset = 0.025f;
        hitMarker.position = hit.point + hit.normal * offset;
        hitMarker.rotation = Quaternion.LookRotation(hit.normal, Vector3.up);
    }

    public void SetTrajectoryVisible(bool visible) {
        if (trajectoryLine != null)
            trajectoryLine.enabled = visible;
        if (hitMarker != null)
            hitMarker.gameObject.SetActive(visible);
    }
    #endregion

    public void PredictTrajectory() {
        Vector3 velocity = Caster.GetTransformCastPoint().forward * (currentLaunchSpeed / _projectilePlotData.ProjectilePrefab.GetRigidbody.mass);
        Vector3 position = Caster.GetTransformCastPoint().position;
        Vector3 nextPosition;
        float overlap;

        UpdateLineRender(maxPoints, (0, position));
        if (aimingMode == AimingMode.ParabolicArc) {
            for (int i = 1; i < maxPoints; i++) {
                velocity = CalculateNewVelocity(velocity, _projectilePlotData.ProjectilePrefab.GetRigidbody.linearDamping, increment);
                nextPosition = position + velocity * increment;

                overlap = Vector3.Distance(position, nextPosition) * rayOverlap;

                if (Physics.Raycast(position, velocity.normalized, out RaycastHit hit, overlap)) {
                    UpdateLineRender(i, (i - 1, hit.point));
                    MoveHitMarker(hit);
                    break;
                }

                if (hitMarker != null)
                    hitMarker.gameObject.SetActive(false);

                position = nextPosition;
                UpdateLineRender(maxPoints, (i, position));
            }
        }
        else if (aimingMode == AimingMode.StraightAim) {
            velocity = velocity = Caster.GetTransformCastPoint().forward * currentLaunchSpeed;
            for (int i = 1; i < maxPoints; i++) {
                nextPosition = position + velocity * increment;

                overlap = Vector3.Distance(position, nextPosition) * rayOverlap;

                if (Physics.Raycast(position, velocity.normalized, out RaycastHit hit, overlap)) {
                    UpdateLineRender(i, (i - 1, hit.point));
                    MoveHitMarker(hit);
                    break;
                }

                if (hitMarker != null)
                    hitMarker.gameObject.SetActive(false);

                position = nextPosition;
                UpdateLineRender(maxPoints, (i, position));
            }
        }
    }
}