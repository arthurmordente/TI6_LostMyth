using UnityEngine;

[CreateAssetMenu(fileName = "AbilityPointData", menuName = "Scriptable Objects/AbilityPointData")]
public class AbilityPointData : ScriptableObject
{
    public int StartPoints;
    public int GainPerBossPoints;
}
