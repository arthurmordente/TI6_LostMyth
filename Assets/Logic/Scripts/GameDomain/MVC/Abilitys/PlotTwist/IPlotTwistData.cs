using Logic.Scripts.GameDomain.MVC.Abilitys;
using System.Collections.Generic;

public interface IPlotTwistData {
    string GetName();
    List<AbilityEffect> Effects { get; }
}