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

    /// <summary>
    /// Skill targeting / AoE preview highlight (e.g. fresnel). Implementations should delegate to
    /// <see cref="Logic.Scripts.GameDomain.VisualFeedback.SkillTargetingHighlightBridge.SetHighlighted"/>.
    /// Add <see cref="Logic.Scripts.GameDomain.VisualFeedback.SkillTargetingHighlightPresenter"/> on the prefab to show visuals.
    /// New skill system offensive aim skips <see cref="Logic.Scripts.GameDomain.MVC.Shared.IPlayableUnit"/> until friendly AoE preview is implemented.
    /// </summary>
    public void SetSkillTargetingHighlight(bool active);
}
