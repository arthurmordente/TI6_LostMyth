using UnityEngine;
using UnityEngine.Serialization;

public class NewSkillSystemSkillLoadout : MonoBehaviour
{
    [FormerlySerializedAs("_paschoalSkills")]
    [SerializeField] private SkillDataSO[] _newSkillSystemSkills = new SkillDataSO[5];

    public bool TryGetSkill(int index, out SkillDataSO skill)
    {
        skill = null;
        if (_newSkillSystemSkills == null) return false;
        if (index < 0 || index >= _newSkillSystemSkills.Length) return false;

        skill = _newSkillSystemSkills[index];
        return skill != null;
    }

    public void SetSkills(SkillDataSO[] skills)
    {
        _newSkillSystemSkills = skills ?? new SkillDataSO[0];
    }
}
