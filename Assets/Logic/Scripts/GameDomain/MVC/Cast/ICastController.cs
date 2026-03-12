using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Nara;

public interface ICastController {
    /// <summary>Sets up all abilities with the given unit as the owner/caster context.</summary>
    public void InitEntryPoint(INaraController naraController);
    public bool TryUseAbility(int index, IPlayableUnit caster);
    public void UseAbility(IPlayableUnit caster);
    public void CancelAbilityUse();
    public bool GetCanUseAbility();
    public void SetCanUseAbility(bool b);
}
