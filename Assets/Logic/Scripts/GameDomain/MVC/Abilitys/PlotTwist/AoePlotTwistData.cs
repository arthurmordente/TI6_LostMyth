using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "AoePlotTwistData", menuName = "Scriptable Objects/PlotTwistData/Aoe")]
public class AoePlotTwistData : ScriptableObject, IPlotTwistData {
    public string Name;
    public string Description;
    public Sprite Icon;

    public GameObject AoePrefab;

    [SerializeReference] private List<AbilityEffect> _effects = new List<AbilityEffect>();
    public List<AbilityEffect> Effects => _effects;

    public string GetName() {
        return Name;
    }
}