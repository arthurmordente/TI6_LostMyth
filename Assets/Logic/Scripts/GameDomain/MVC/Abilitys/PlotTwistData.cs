using UnityEngine;

[CreateAssetMenu(fileName = "PlotTwistData", menuName = "Scriptable Objects/PlotTwistData")]
public class PlotTwistData : ScriptableObject {
    public string Name;
    public string Description;
    public Sprite Icon;
    public ProjectileController ProjectilePrefab;
}