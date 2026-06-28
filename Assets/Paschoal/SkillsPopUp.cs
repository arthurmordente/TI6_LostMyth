using UnityEngine;
using Zenject;
using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.GameDomain.MVC.ExplorationLoadout;
using TMPro;
using UnityEngine.UI;

public class SkillsPopUp : MonoBehaviour
{
    public INewSkillSystemSkillLoadoutService _loadout;
    public Image icon;
    public TextMeshProUGUI nome, descricao, custo, tipo, skillEffect;
    SkillLoadoutUnitType _loadoutUnitType;
    int _loadoutIndex;
    SkillDataSO _displayedSkill;

    [InjectOptional] IRandomTurnPassiveService _randomTurnPassive;

    [Inject] private void Construct(
        INewSkillSystemSkillLoadoutService loadout,
        [InjectOptional] IRandomTurnPassiveService randomTurnPassive = null)
    {
        _loadout = loadout;
        _randomTurnPassive = randomTurnPassive;
    }

    void OnEnable()
    {
        if (_randomTurnPassive != null)
            _randomTurnPassive.OnTurnEffectRolled += HandleTurnEffectRolled;
    }

    void OnDisable()
    {
        if (_randomTurnPassive != null)
            _randomTurnPassive.OnTurnEffectRolled -= HandleTurnEffectRolled;
    }

    private void Update()
    {
        transform.position = Input.mousePosition;
    }

    public void UnitType(bool player)
    {
        if (player)
            _loadoutUnitType = SkillLoadoutUnitType.Player;
        else
            _loadoutUnitType = SkillLoadoutUnitType.Book;
    }

    public void SkillsIndex(int i)
    {
        _loadoutIndex = i;
    }

    public void MudaTexto()
    {
        if (!_loadout.TryGetSelectedSkill(_loadoutUnitType, _loadoutIndex, out SkillDataSO skill) || skill == null)
            return;

        _displayedSkill = skill;
        nome.text = skill.SkillName;
        descricao.richText = true;
        descricao.text = SkillDescriptionRichTextFormatter.Format(skill);
        custo.text = skill.Cost.ToString();
        icon.sprite = skill.Icon;
        if (tipo != null)
            tipo.text = ExplorationLoadoutSkillFilterUtil.DisplayLabel(skill.SkillType);

        RefreshSkillEffectLine(skill);
    }

    void HandleTurnEffectRolled()
    {
        if (!isActiveAndEnabled || _displayedSkill == null)
            return;
        if (_displayedSkill.CombatPopupSkillEffectSource != SkillCombatPopupEffectSource.RandomTurnPassiveRoll)
            return;

        RefreshSkillEffectLine(_displayedSkill);
    }

    void RefreshSkillEffectLine(SkillDataSO skill)
    {
        if (skillEffect == null)
            return;

        if (skill.CombatPopupSkillEffectSource != SkillCombatPopupEffectSource.RandomTurnPassiveRoll)
        {
            skillEffect.gameObject.SetActive(false);
            skillEffect.text = string.Empty;
            return;
        }

        skillEffect.gameObject.SetActive(true);
        skillEffect.richText = true;
        string rollText = _randomTurnPassive?.ActiveRollDisplayText;
        skillEffect.text = string.IsNullOrEmpty(rollText) ? "—" : rollText;
    }
}
