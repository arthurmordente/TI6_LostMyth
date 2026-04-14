using UnityEngine;

public class PaschoalSkillLoadout : MonoBehaviour
{
    [SerializeField] private SkillDataSO[] _paschoalSkills = new SkillDataSO[5];

    public bool TryGetSkill(int index, out SkillDataSO skill)
    {
        skill = null;
        if (_paschoalSkills == null) return false;
        if (index < 0 || index >= _paschoalSkills.Length) return false;

        skill = _paschoalSkills[index];
        return skill != null;
    }

    public void SetSkills(SkillDataSO[] skills)
    {
        _paschoalSkills = skills ?? new SkillDataSO[0];
    }
}
