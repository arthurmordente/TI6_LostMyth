using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ProjectilePlotTwistData", menuName = "Scriptable Objects/PlotTwistData/Projectile")]
public class ProjectilePlotTwistData : ScriptableObject, IPlotTwistData {
    public string Name;
    public string Description;
    public Sprite Icon;

    public ProjectileController ProjectilePrefab;

    [Header("Parabolic Arc Settings")]
    public float ParabolicMaxHeight = 10f;
    public float ParabolicMaxRange = 50f;
    public float ParabolicMinRange = 3f;

    [SerializeReference] private List<AbilityEffect> _effects = new List<AbilityEffect>();
    public List<AbilityEffect> Effects => _effects;

    public string GetName() {
        return Name;
    }
}