using UnityEngine;

public class LegacySkillSystemToggle : MonoBehaviour
{
    [SerializeField] private bool _useLegacySkillSystem = false;

    public bool UseLegacySkillSystem => _useLegacySkillSystem;
}
