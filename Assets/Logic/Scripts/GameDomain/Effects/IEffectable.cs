using UnityEngine;

public interface IEffectable
{
    public Transform GetReferenceTransform();
    public Transform GetTransformCastPoint();
    public LineRenderer GetPointLineRenderer();
    public GameObject GetReferenceTargetPrefab();
    public void PreviewHeal(int healAmound);
    public void PreviewDamage(int damageAmound);
    public void ResetPreview();
    public void TakeDamage(int damageAmount);
    public void TakeDamagePerTurn(int damageAmount, int duration);
    public void Heal(int healAmount);
    public void HealPerTurn(int healAmount, int duration);
}
