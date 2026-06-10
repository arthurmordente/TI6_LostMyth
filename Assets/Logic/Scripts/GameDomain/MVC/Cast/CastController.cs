using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Echo;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.GameDomain.Services.Skills;
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
    private bool _deferredArenaSyncAfterProjectileCast;
    private bool _lastCastWasMovementSkill;
    private int _currentAbilityIndex = -1;
    private int _currentAbilityCost = 0;
    private int _currentAnimatorAttackType = 1;
    private SkillCastAnimationStyle _currentCastAnimationStyle = SkillCastAnimationStyle.Slow;

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

        if (IsPassiveNewSkillSlot(caster, index))
            return false;

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
        _currentAnimatorAttackType = prepareResult.AnimatorAttackType;
        _currentCastAnimationStyle = prepareResult.CastAnimationStyle;

        var loadout = caster.UnitViewGO != null ? caster.UnitViewGO.GetComponent<NewSkillSystemSkillLoadout>() : null;
        SkillDataSO skillForPreview = null;
        if (loadout != null)
            loadout.TryGetSkill(prepareResult.AbilityIndex, out skillForPreview);

        bool showManaPreview = !isBook && !_cheatController.InfinityCast;
        _gamePlayUiController?.BeginSkillCastAimPreview(caster, skillForPreview, prepareResult.Cost, showManaPreview && ap != null, ap?.Current ?? 0, ap?.Max ?? 0);

        caster.PlayAttackType(prepareResult.AnimatorAttackType, prepareResult.CastAnimationStyle);
        return true;
    }

    public void CancelAbilityUse() {
        var c = _currentCaster;
        _currentCaster?.TriggerCancel();
        _activeFlow?.CancelPreparedCast(c);
        if (c != null)
            _gamePlayUiController?.EndSkillCastAimPreviewCancel(c);
        _activeFlow = null;
        _currentCaster = null;
        _currentAbilityIndex = -1;
        _currentAbilityCost = 0;
        _currentAnimatorAttackType = 1;
        _currentCastAnimationStyle = SkillCastAnimationStyle.Slow;
    }

    public void UseAbility(IPlayableUnit caster) {
        if (_activeFlow == null) return;

        _canUseAbility = true;

        _gamePlayUiController?.EndSkillCastAimPreviewCommit(caster);

        if (_cheatController.InfinityCast == false && caster is not IBookController) {
            var ap = caster?.GetActionPoints() ?? _naraActionPointsService;
            ap?.Spend(_currentAbilityCost);
        }

        if (caster is IBookController && !_cheatController.InfinityCast) {
            _cloneUseLimiter?.MarkUsed();
            _gamePlayUiController?.SyncBookCloneActionHud();
        }

        caster?.TriggerExecute();
        PlayCastSfx(caster);
        PlayBookPageSfx(caster);

        _deferredArenaSyncAfterProjectileCast = false;
        _lastCastWasMovementSkill = false;
        SkillDataSO preparedSkill = null;
        _activeFlow.TryGetPreparedSkill(out preparedSkill);
        if (preparedSkill != null) {
            if (preparedSkill.ShouldDeferArenaSyncUntilProjectileHit())
                _deferredArenaSyncAfterProjectileCast = true;
            if (preparedSkill.SkillType == Logic.Scripts.GameDomain.Services.Skills.SkillType.Movement)
                _lastCastWasMovementSkill = true;
        }

        _activeFlow.ExecutePreparedCast(caster);
        CancelAbilityUse();
    }

    public bool GetCanUseAbility() => _canUseAbility;
    public void SetCanUseAbility(bool b) => _canUseAbility = b;

    public bool ConsumeDeferredArenaSyncAfterProjectileCast()
    {
        if (!_deferredArenaSyncAfterProjectileCast) return false;
        _deferredArenaSyncAfterProjectileCast = false;
        _lastCastWasMovementSkill = false;
        return true;
    }

    public bool ConsumeLastCastWasMovementSkill()
    {
        if (!_lastCastWasMovementSkill) return false;
        _lastCastWasMovementSkill = false;
        return true;
    }

    private void PlayCastSfx(IPlayableUnit caster) {
        if (_audio == null) return;
        _audio.PlaySfx(MapCastClip(caster), AudioChannelType.SfxCombat);
    }

    private void PlayBookPageSfx(IPlayableUnit caster) {
        if (_audio == null || caster is not IBookController) return;
        string clip = _currentAnimatorAttackType > 1 ? SfxIds.Livro_Paginas : SfxIds.Livro_Pagina;
        _audio.PlaySfx(clip, AudioChannelType.SfxCombat);
    }

    private static string MapCastClip(IPlayableUnit caster) {
        return caster is IBookController ? SfxIds.Livro_Cast : SfxIds.Erza_Cast;
    }

    private ISkillCastFlow SelectFlow(IPlayableUnit caster) {
        if (_newSkillSystemCastFlow.CanHandleCaster(caster)) return _newSkillSystemCastFlow;
        return null;
    }

    /// <summary>Passive loadout slots never start a cast (no warning — intentional no-op).</summary>
    private static bool IsPassiveNewSkillSlot(IPlayableUnit caster, int index) {
        if (caster?.UnitViewGO == null) return false;
        var legacyToggle = caster.UnitViewGO.GetComponent<LegacySkillSystemToggle>();
        if (legacyToggle != null && legacyToggle.UseLegacySkillSystem) return false;
        var loadout = caster.UnitViewGO.GetComponent<NewSkillSystemSkillLoadout>();
        if (loadout == null || !loadout.TryGetSkill(index, out SkillDataSO skill) || skill == null) return false;
        return skill.SkillType == SkillType.Passive;
    }
}
