using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SelfPlotTwistData", menuName = "Scriptable Objects/PlotTwistData/Self")]
public class SelfPlotTwistData : ScriptableObject, IPlotTwistData {
    public string Name;
    public string Description;
    public Sprite Icon;

    public GameObject SelfCastPrefab;

    [SerializeReference] private List<AbilityEffect> _effects = new List<AbilityEffect>();
    public List<AbilityEffect> Effects => _effects;

    public string GetName() {
        return Name;
    }
}