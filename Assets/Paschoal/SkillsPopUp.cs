using UnityEngine;
using Zenject;
using Logic.Scripts.GameDomain.Services.Skills;
using TMPro;
using UnityEngine.UI;

public class SkillsPopUp : MonoBehaviour
{
    public INewSkillSystemSkillLoadoutService _loadout;
    public Image icon;
    public TextMeshProUGUI nome,descricao,custo;
    SkillLoadoutUnitType _loadoutUnitType;
    int _loadoutIndex;


    [Inject] private void Construct(INewSkillSystemSkillLoadoutService loadout)
    {
        _loadout = loadout;
    }

    private void Update()
    {
        transform.position = Input.mousePosition;
    }
    public void UnitType(bool player)
    {
        if(player)
        {
            _loadoutUnitType = SkillLoadoutUnitType.Player;
        }
        else
        {
            _loadoutUnitType = SkillLoadoutUnitType.Book;
        }
    }
    public void SkillsIndex(int i)
    {
        _loadoutIndex = i;
    }
    public void MudaTexto()
    {
        _loadout.TryGetSelectedSkill(_loadoutUnitType, _loadoutIndex, out SkillDataSO skill);
        nome.text = skill.name;
        descricao.text = skill.Description;
        custo.text = skill.Cost.ToString();
        icon.sprite = skill.Icon;
    }
}
