using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.MVC.Echo;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Services.UpdateService;
using Logic.Scripts.Turns;
using UnityEngine;
using Zenject;
using Logic.Scripts.Services.AudioService;

public class CastController : ICastController
{
    private readonly IActionPointsService _actionPointsService;
    private readonly IUpdateSubscriptionService _subscriptionService;
    private readonly ICommandFactory _commandFactory;
    private readonly AbilityData[] _abilities;

    private AbilityData _currentAbility;
    private IEffectable _currentCaster;
    private bool _canUseAbility;
    private int _currentAbilityIndex = -1;

    public Transform PlayerTransform;

    private IAudioService _audio;

    public CastController(IUpdateSubscriptionService updateSubscriptionService, ICommandFactory commandFactory,
        IActionPointsService actionPointsService, AbilityData[] abilities)
    {
        _subscriptionService = updateSubscriptionService;
        _actionPointsService = actionPointsService;
        _commandFactory = commandFactory;
        _abilities = abilities;

        try { _audio = ProjectContext.Instance.Container.Resolve<IAudioService>(); } catch { _audio = null; }
    }

    public void InitEntryPoint(INaraController naraController)
    {
        PlayerTransform = naraController.NaraViewGO.transform;
        foreach (AbilityData ability in _abilities)
            ability.SetUp(_subscriptionService, _commandFactory);
    }

    public bool TryUseAbility(int index, IEffectable caster)
    {
        if (_actionPointsService.CanSpend(_abilities[index].GetCost()))
        {
            _abilities[index].Aim(caster);
            _currentAbility = _abilities[index];
            _currentCaster = caster;
            _currentAbilityIndex = index;

            if (caster is INaraController naraController)
            {
                int attackType = _abilities[index] != null ? _abilities[index].AnimatorAttackType : 1;
                naraController.PlayAttackType(attackType);
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    public void CancelAbilityUse()
    {
        if (_currentCaster is INaraController naraController)
            naraController.TriggerCancel();

        _currentAbility?.Cancel();
        _currentAbility = null;
        _currentCaster = null;
        _currentAbilityIndex = -1;
    }

    public void UseAbility(IEffectable caster)
    {
        if (_currentAbility == null) return;

        _canUseAbility = true;
        _actionPointsService.Spend(_currentAbility.GetCost());

        if (caster is INaraController naraController)
        {
            naraController.TriggerExecute();
        }

        PlayUsedSfxByIndex(_currentAbilityIndex);

        _currentAbility.Cast(caster);
        CancelAbilityUse();
    }

    public bool GetCanUseAbility() => _canUseAbility;
    public void SetCanUseAbility(bool b) => _canUseAbility = b;

    public void UseFastEcho(IEchoController echoController, Transform caster) =>
        echoController.CreateFastEcho(caster);

    public void UseSlowEcho(IEchoController echoController, Transform caster) =>
        echoController.CreateSlowEcho(caster);

    private void PlayUsedSfxByIndex(int index)
    {
        if (_audio == null) return;

        AudioClipType clip = MapUsedClip(index);
        _audio.PlayAudio(clip, AudioChannelType.Fx, AudioPlayType.OneShot);
    }

    private static AudioClipType MapUsedClip(int index)
    {
        switch (index)
        {
            case 0: return AudioClipType.AbilityUsed1SFX;
            case 1: return AudioClipType.AbilityUsed2SFX;
            case 2: return AudioClipType.AbilityUsed3SFX;
            case 3: return AudioClipType.AbilityUsed4SFX;
            default: return AudioClipType.AbilityUsed5SFX;
        }
    }
}
