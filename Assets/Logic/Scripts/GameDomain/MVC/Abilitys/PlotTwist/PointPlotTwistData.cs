using UnityEngine;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "PointPlotTwistData", menuName = "Scriptable Objects/PlotTwistData/Point")]
public class PointPlotTwistData : ScriptableObject, IPlotTwistData {
    public string Name;
    public string Description;
    public Sprite Icon;

    public AbilitySummon ObjectToSummon;

    public int Duration;
    public int HealAmount;

    [SerializeReference] private List<AbilityEffect> _effects = new List<AbilityEffect>();
    public List<AbilityEffect> Effects => _effects;

    public string GetName() {
        return Name;
    }
}