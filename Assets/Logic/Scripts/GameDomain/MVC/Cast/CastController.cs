using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Echo;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Ui;
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
    private readonly ICloneUseLimiter _cloneUseLimiter;
    private readonly IGamePlayUiController _gamePlayUiController;

    private IPlayableUnit _currentCaster;
    private ISkillCastFlow _activeFlow;
    private bool _canUseAbility;
    private int _currentAbilityIndex = -1;
    private int _currentAbilityCost = 0;

    public Transform PlayerTransform;

    private IAudioService _audio;
    private readonly LegacySkillCastFlow _legacyFlow;
    private readonly NewSkillSystemDefaultSkillCastFlow _newSkillSystemCastFlow;

    public CastController(IUpdateSubscriptionService updateSubscriptionService, ICommandFactory commandFactory,
        IActionPointsService actionPointsService, ICheatController cheatController,
        NewSkillSystemDefaultSkillCastFlow newSkillSystemSkillCastFlow,
        [InjectOptional] ICloneUseLimiter cloneUseLimiter = null,
        [InjectOptional] IGamePlayUiController gamePlayUiController = null) {
        _subscriptionService = updateSubscriptionService;
        _commandFactory = commandFactory;
        _naraActionPointsService = actionPointsService;
        _cheatController = cheatController;
        _cloneUseLimiter = cloneUseLimiter;
        _gamePlayUiController = gamePlayUiController;
        _legacyFlow = new LegacySkillCastFlow(_subscriptionService, _commandFactory);
        _newSkillSystemCastFlow = newSkillSystemSkillCastFlow;
        try { _audio = ProjectContext.Instance.Container.Resolve<IAudioService>(); } catch { _audio = null; }
    }

    public void InitEntryPoint(INaraController naraController) {
        PlayerTransform = naraController.NaraViewGO.transform;
        _legacyFlow.InitEntryPoint(naraController);
        _newSkillSystemCastFlow.InitEntryPoint(naraController);
    }

    public bool TryUseAbility(int index, IPlayableUnit caster) {
        bool isBook = caster is IBookController;
        if (isBook && !_cheatController.InfinityCast && _cloneUseLimiter != null && !_cloneUseLimiter.CanUse()) {
            Debug.LogWarning("[CastController] TryUseAbility — Book already used its one skill this player turn.");
            return false;
        }

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
        bool canAfford = isBook
            || (ap == null || ap.CanSpend(cost))
            || _cheatController.InfinityCast;
        if (!canAfford) {
            Debug.LogWarning($"[CastController] TryUseAbility — cannot afford ability (cost {cost}, AP {ap?.Current}).");
            selectedFlow.CancelPreparedCast(caster);
            return false;
        }

        _activeFlow = selectedFlow;
        _currentCaster = caster;
        _currentAbilityIndex = prepareResult.AbilityIndex;
        _currentAbilityCost = isBook ? 0 : prepareResult.Cost;

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

        if (_cheatController.InfinityCast == false && caster is not IBookController) {
            var ap = caster?.GetActionPoints() ?? _naraActionPointsService;
            ap?.Spend(_currentAbilityCost);
        }

        if (caster is IBookController && !_cheatController.InfinityCast) {
            _cloneUseLimiter?.MarkUsed();
            _gamePlayUiController?.SyncBookCloneActionHud();
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
        if (_newSkillSystemCastFlow.CanHandleCaster(caster)) return _newSkillSystemCastFlow;
        return null;
    }
}
