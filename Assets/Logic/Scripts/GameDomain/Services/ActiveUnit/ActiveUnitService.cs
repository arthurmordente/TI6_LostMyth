using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Environment;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.Services.AudioService;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.Services.ActiveUnit
{
    public class ActiveUnitService : IActiveUnitService
    {
        private static bool SlotShowsManaCostUi(SkillDataSO skill) =>
            skill == null || skill.SkillType != SkillType.Passive;
        private readonly INaraController _naraController;
        private readonly ICameraFocusService _cameraFocus;
        private readonly IGamePlayUiController _gamePlayUiController;
        private readonly INewSkillSystemSkillLoadoutService _newSkillSystemSkillLoadoutService;
        private readonly IAudioService _audioService;
        private IPlayableUnit _bookUnit;

        public IPlayableUnit ActiveUnit { get; private set; }
        public bool IsBookDeployed => _bookUnit != null;

        public ActiveUnitService(INaraController naraController, ICameraFocusService cameraFocusService,
            IGamePlayUiController gamePlayUiController, IAudioService audioService,
            [InjectOptional] INewSkillSystemSkillLoadoutService newSkillSystemSkillLoadoutService = null)
        {
            _naraController = naraController;
            _cameraFocus = cameraFocusService;
            _gamePlayUiController = gamePlayUiController;
            _audioService = audioService;
            _newSkillSystemSkillLoadoutService = newSkillSystemSkillLoadoutService;
            ActiveUnit = naraController as IPlayableUnit;
        }

        public void RefreshHudAbilityCosts() => PushAbilityCostsToHud();

        private void PushAbilityCostsToHud()
        {
            if (_gamePlayUiController == null || ActiveUnit == null) return;
            ReloadNewSkillSystemLoadoutForUnit(ActiveUnit);
            PushNewSkillSystemSkillIconsToHud();

            if (ActiveUnit is IBookController)
            {
                _gamePlayUiController.SetAbilityManaCosts(0, 0, 0, 0, false, false, false, false);
                return;
            }

            var unitView = ActiveUnit.UnitViewGO;
            if (unitView != null)
            {
                var legacyToggle = unitView.GetComponent<LegacySkillSystemToggle>();
                bool useLegacy = legacyToggle != null && legacyToggle.UseLegacySkillSystem;
                if (!useLegacy)
                {
                    var newSkillSystemLoadout = unitView.GetComponent<NewSkillSystemSkillLoadout>();
                    if (newSkillSystemLoadout != null)
                    {
                        int newSkillSystemCostAt(int i)
                        {
                            if (!newSkillSystemLoadout.TryGetSkill(i, out SkillDataSO skill) || skill == null) return 0;
                            return Mathf.Max(0, skill.Cost);
                        }

                        SkillDataSO skillAt(int i)
                        {
                            if (!newSkillSystemLoadout.TryGetSkill(i, out SkillDataSO s)) return null;
                            return s;
                        }

                        _gamePlayUiController.SetAbilityManaCosts(
                            newSkillSystemCostAt(0), newSkillSystemCostAt(1), newSkillSystemCostAt(2), newSkillSystemCostAt(3),
                            SlotShowsManaCostUi(skillAt(0)),
                            SlotShowsManaCostUi(skillAt(1)),
                            SlotShowsManaCostUi(skillAt(2)),
                            SlotShowsManaCostUi(skillAt(3)));
                        return;
                    }
                }
            }

            var abs = ActiveUnit.GetAbilities();
            int legacyCostAt(int i) => abs != null && i < abs.Length && abs[i] != null ? abs[i].GetCost() : 0;
            _gamePlayUiController.SetAbilityManaCosts(legacyCostAt(0), legacyCostAt(1), legacyCostAt(2), legacyCostAt(3),
                true, true, true, true);
        }

        private void PushNewSkillSystemSkillIconsToHud()
        {
            if (_gamePlayUiController == null || _newSkillSystemSkillLoadoutService == null) return;
            SkillDataSO[] p = _newSkillSystemSkillLoadoutService.BuildRuntimeSlotsArray(SkillLoadoutUnitType.Player);
            SkillDataSO[] b = _newSkillSystemSkillLoadoutService.BuildRuntimeSlotsArray(SkillLoadoutUnitType.Book);
            _gamePlayUiController.SetSkillHudVisuals(
                SkillAt(p, 0), SkillAt(p, 1), SkillAt(p, 2), SkillAt(p, 3),
                SkillAt(b, 0), SkillAt(b, 1), SkillAt(b, 2), SkillAt(b, 3));
        }

        private static SkillDataSO SkillAt(SkillDataSO[] slots, int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return null;
            return slots[index];
        }

        public void RegisterBook(IPlayableUnit book)
        {
            _bookUnit = book;
            _bookUnit.SetMovementActive(false);
            SyncPlayableKinematicState();
        }

        public void UnregisterBook()
        {
            _bookUnit = null;
            SetNaraAsActiveUnit();
        }

        public void SetNaraAsActiveUnit()
        {
            var naraPlayable = _naraController as IPlayableUnit;
            if (ActiveUnit == naraPlayable)
            {
                // Active unit already is Nara: still refresh visual state (circle/line).
                ActiveUnit?.SetMovementActive(true);
                ActiveUnit?.OnBecomeActive();
                _gamePlayUiController.ShowBookSkillsTheme(false);
                PushAbilityCostsToHud();
                FollowActiveUnit();
                SyncPlayableKinematicState();
                return;
            }

            ActiveUnit?.SetMovementActive(false);
            ActiveUnit?.OnBecomeInactive();
            ActiveUnit = naraPlayable;
            ActiveUnit?.SetMovementActive(true);
            ActiveUnit?.OnBecomeActive();

            _gamePlayUiController.ShowBookSkillsTheme(false);
            PushAbilityCostsToHud();
            FollowActiveUnit();
            SyncPlayableKinematicState();
        }

        public void SetBookAsActiveUnit(IPlayableUnit book)
        {
            if (!IsBookDeployed || book == null) return;

            ActiveUnit?.SetMovementActive(false);
            ActiveUnit?.OnBecomeInactive();
            ActiveUnit = book;
            ActiveUnit?.SetMovementActive(true);
            ActiveUnit?.OnBecomeActive();

            _gamePlayUiController.ShowBookSkillsTheme(true);
            PushAbilityCostsToHud();
            FollowActiveUnit();
            SyncPlayableKinematicState();
        }

        public void ToggleActiveUnit()
        {
            if (!IsBookDeployed) return;

            _audioService?.PlaySfx(SfxIds.Ezra_Trocar_personagem, AudioChannelType.SfxCombat);

            var naraAsPlayable = _naraController as IPlayableUnit;
            if (ActiveUnit == naraAsPlayable)
                SetBookAsActiveUnit(_bookUnit);
            else
                SetNaraAsActiveUnit();
        }

        private void SyncPlayableKinematicState()
        {
            bool bookActive = IsBookDeployed && _bookUnit != null && ActiveUnit == _bookUnit;
            CombatPlayablePairKinematic.Sync(
                _naraController as IPlayableUnit,
                _bookUnit,
                IsBookDeployed,
                bookActive);
        }

        private void FollowActiveUnit()
        {
            if (_cameraFocus == null || ActiveUnit == null) return;
            var target = ActiveUnit.UnitViewGO?.transform;
            if (target != null)
                _cameraFocus.SetDefaultFollow(target);
        }

        private void ReloadNewSkillSystemLoadoutForUnit(IPlayableUnit unit)
        {
            if (unit == null || _newSkillSystemSkillLoadoutService == null) return;
            var unitView = unit.UnitViewGO;
            if (unitView == null) return;

            var newSkillSystemLoadout = unitView.GetComponent<NewSkillSystemSkillLoadout>();
            if (newSkillSystemLoadout == null) return;

            var unitType = ResolveUnitType(unit);
            newSkillSystemLoadout.SetSkills(_newSkillSystemSkillLoadoutService.BuildRuntimeSlotsArray(unitType));
        }

        private SkillLoadoutUnitType ResolveUnitType(IPlayableUnit unit)
        {
            if (unit == null) return SkillLoadoutUnitType.Player;
            var naraPlayable = _naraController as IPlayableUnit;
            return unit == naraPlayable ? SkillLoadoutUnitType.Player : SkillLoadoutUnitType.Book;
        }
    }
}
