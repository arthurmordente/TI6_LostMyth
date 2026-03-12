using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.UpdateService;
using Logic.Scripts.Turns;
using UnityEngine;
using Zenject;

public class CastController : ICastController {
    private readonly IUpdateSubscriptionService _subscriptionService;
    private readonly ICommandFactory _commandFactory;
    private readonly ICheatController _cheatController;
    // Nara's AP injected directly as a reliable fallback for when EnsureApService() hasn't
    // resolved yet on the very first ability use.
    private readonly IActionPointsService _naraActionPointsService;

    private AbilityData _currentAbility;
    private IPlayableUnit _currentCaster;
    private bool _canUseAbility;
    private int _currentAbilityIndex = -1;

    public Transform PlayerTransform;

    private IAudioService _audio;

    public CastController(IUpdateSubscriptionService updateSubscriptionService, ICommandFactory commandFactory,
        IActionPointsService actionPointsService, ICheatController cheatController) {
        _subscriptionService = updateSubscriptionService;
        _commandFactory = commandFactory;
        _naraActionPointsService = actionPointsService;
        _cheatController = cheatController;
        try { _audio = ProjectContext.Instance.Container.Resolve<IAudioService>(); } catch { _audio = null; }
    }

    public void InitEntryPoint(INaraController naraController) {
        PlayerTransform = naraController.NaraViewGO.transform;
        // Set up Nara's abilities.  The Book shares the same AbilityData instances initially
        // so there is no need to call SetUp twice (it is already handled here).
        foreach (AbilityData ability in naraController.GetAbilities())
            ability.SetUp(_subscriptionService, _commandFactory);
    }

    public bool TryUseAbility(int index, IPlayableUnit caster) {
        var abilities = caster?.GetAbilities();
        if (abilities == null || index < 0 || index >= abilities.Length) return false;

        // Use the caster's own AP.  For Nara the lazy EnsureApService() is the primary path;
        // _naraActionPointsService is an injected fallback in case it hasn't resolved yet.
        // Book returns its own BookActionPoints directly.
        var ap = caster.GetActionPoints() ?? _naraActionPointsService;
        bool canAfford = (ap == null || ap.CanSpend(abilities[index].GetCost()))
                         || _cheatController.InfinityCast;
        if (!canAfford) return false;

        abilities[index].Aim(caster);
        _currentAbility = abilities[index];
        _currentCaster = caster;
        _currentAbilityIndex = index;

        int attackType = abilities[index].AnimatorAttackType;
        caster.PlayAttackType(attackType);
        return true;
    }

    public void CancelAbilityUse() {
        _currentCaster?.TriggerCancel();
        _currentAbility?.Cancel();
        _currentAbility = null;
        _currentCaster = null;
        _currentAbilityIndex = -1;
    }

    public void UseAbility(IPlayableUnit caster) {
        if (_currentAbility == null) return;

        _canUseAbility = true;

        if (_cheatController.InfinityCast == false) {
            // Deduct from whichever AP pool this caster owns (Nara's or Book's).
            var ap = caster?.GetActionPoints() ?? _naraActionPointsService;
            ap?.Spend(_currentAbility.GetCost());
        }

        caster?.TriggerExecute();
        PlayUsedSfxByIndex(_currentAbilityIndex);
        _currentAbility.Cast(caster);
        CancelAbilityUse();
    }

    public bool GetCanUseAbility() => _canUseAbility;
    public void SetCanUseAbility(bool b) => _canUseAbility = b;

    private void PlayUsedSfxByIndex(int index) {
        if (_audio == null) return;
        AudioClipType clip = MapUsedClip(index);
        _audio.PlayAudio(clip, AudioChannelType.Fx, AudioPlayType.OneShot);
    }

    private static AudioClipType MapUsedClip(int index) {
        switch (index) {
            case 0: return AudioClipType.AbilityUsed1SFX;
            case 1: return AudioClipType.AbilityUsed2SFX;
            case 2: return AudioClipType.AbilityUsed3SFX;
            case 3: return AudioClipType.AbilityUsed4SFX;
            default: return AudioClipType.AbilityUsed5SFX;
        }
    }
}
