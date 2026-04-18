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

    private IPlayableUnit _currentCaster;
    private ISkillCastFlow _activeFlow;
    private bool _canUseAbility;
    private int _currentAbilityIndex = -1;
    private int _currentAbilityCost = 0;

    public Transform PlayerTransform;

    private IAudioService _audio;
    private readonly LegacySkillCastFlow _legacyFlow;
    private readonly PaschoalDefaultSkillCastFlow _paschoalFlow;

    public CastController(IUpdateSubscriptionService updateSubscriptionService, ICommandFactory commandFactory,
        IActionPointsService actionPointsService, ICheatController cheatController,
        PaschoalDefaultSkillCastFlow paschoalSkillCastFlow) {
        _subscriptionService = updateSubscriptionService;
        _commandFactory = commandFactory;
        _naraActionPointsService = actionPointsService;
        _cheatController = cheatController;
        _legacyFlow = new LegacySkillCastFlow(_subscriptionService, _commandFactory);
        _paschoalFlow = paschoalSkillCastFlow;
        try { _audio = ProjectContext.Instance.Container.Resolve<IAudioService>(); } catch { _audio = null; }
    }

    public void InitEntryPoint(INaraController naraController) {
        PlayerTransform = naraController.NaraViewGO.transform;
        _legacyFlow.InitEntryPoint(naraController);
        _paschoalFlow.InitEntryPoint(naraController);
    }

    public bool TryUseAbility(int index, IPlayableUnit caster) {
        ISkillCastFlow selectedFlow = SelectFlow(caster);
        if (selectedFlow == null) {
            Debug.LogWarning("[CastController] TryUseAbility — no cast flow available for caster.");
            return false;
        }

        if (!selectedFlow.TryPrepareCast(index, caster, out SkillCastPrepareResult prepareResult)) {
            Debug.LogWarning($"[CastController] TryUseAbility — flow {selectedFlow.GetType().Name} rejected ability index {index}.");
            return false;
        }

        var ap = caster.GetActionPoints() ?? _naraActionPointsService;
        int cost = prepareResult.Cost;
        bool canAfford = (ap == null || ap.CanSpend(cost)) || _cheatController.InfinityCast;
        if (!canAfford) {
            Debug.LogWarning($"[CastController] TryUseAbility — cannot afford ability (cost {cost}, AP {ap?.Current}).");
            selectedFlow.CancelPreparedCast(caster);
            return false;
        }

        _activeFlow = selectedFlow;
        _currentCaster = caster;
        _currentAbilityIndex = prepareResult.AbilityIndex;
        _currentAbilityCost = prepareResult.Cost;

        int attackType = prepareResult.AnimatorAttackType;
        caster.PlayAttackType(attackType);
        return true;
    }

    public void CancelAbilityUse() {
        _currentCaster?.TriggerCancel();
        _activeFlow?.CancelPreparedCast(_currentCaster);
        _activeFlow = null;
        _currentCaster = null;
        _currentAbilityIndex = -1;
        _currentAbilityCost = 0;
    }

    public void UseAbility(IPlayableUnit caster) {
        if (_activeFlow == null) return;

        _canUseAbility = true;

        if (_cheatController.InfinityCast == false) {
            // Deduct from whichever AP pool this caster owns (Nara's or Book's).
            var ap = caster?.GetActionPoints() ?? _naraActionPointsService;
            ap?.Spend(_currentAbilityCost);
        }

        caster?.TriggerExecute();
        PlayUsedSfxByIndex(_currentAbilityIndex);
        _activeFlow.ExecutePreparedCast(caster);
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

    private ISkillCastFlow SelectFlow(IPlayableUnit caster) {
        if (_legacyFlow.CanHandleCaster(caster)) return _legacyFlow;
        if (_paschoalFlow.CanHandleCaster(caster)) return _paschoalFlow;
        return _legacyFlow;
    }
}
