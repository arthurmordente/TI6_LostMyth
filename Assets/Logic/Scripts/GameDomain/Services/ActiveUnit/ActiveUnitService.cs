using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.MVC.Book;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.GameDomain.Services.Skills;
using UnityEngine;
using Zenject;

namespace Logic.Scripts.GameDomain.Services.ActiveUnit
{
    public class ActiveUnitService : IActiveUnitService
    {
        private static bool SlotShowsManaCostUi(SkillDataSO skill) =>
            skill == null || skill.SkillType != SkillType.Passive;
        private readonly INaraController _naraController;
        private readonly IWorldCameraController _worldCamera;
        private readonly IGamePlayUiController _gamePlayUiController;
        private readonly INewSkillSystemSkillLoadoutService _newSkillSystemSkillLoadoutService;
        private IPlayableUnit _bookUnit;

        public IPlayableUnit ActiveUnit { get; private set; }
        public bool IsBookDeployed => _bookUnit != null;

        public ActiveUnitService(INaraController naraController, IWorldCameraController worldCameraController,
            IGamePlayUiController gamePlayUiController, [InjectOptional] INewSkillSystemSkillLoadoutService newSkillSystemSkillLoadoutService = null)
        {
            _naraController = naraController;
            _worldCamera = worldCameraController;
            _gamePlayUiController = gamePlayUiController;
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
                var bookView = ActiveUnit.UnitViewGO;
                var bookLoadout = bookView != null ? bookView.GetComponent<NewSkillSystemSkillLoadout>() : null;
                SkillDataSO skillAtSlot(int i)
                {
                    if (bookLoadout == null || !bookLoadout.TryGetSkill(i, out SkillDataSO s)) return null;
                    return s;
                }

                _gamePlayUiController.SetAbilityManaCosts(0, 0, 0, 0,
                    SlotShowsManaCostUi(skillAtSlot(0)),
                    SlotShowsManaCostUi(skillAtSlot(1)),
                    SlotShowsManaCostUi(skillAtSlot(2)),
                    SlotShowsManaCostUi(skillAtSlot(3)));
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
            _gamePlayUiController.SetSkillHudIcons(
                IconFrom(p, 0), IconFrom(p, 1), IconFrom(p, 2), IconFrom(p, 3),
                IconFrom(b, 0), IconFrom(b, 1), IconFrom(b, 2), IconFrom(b, 3));
        }

        private static Sprite IconFrom(SkillDataSO[] slots, int index)
        {
            if (slots == null || index < 0 || index >= slots.Length) return null;
            return slots[index] != null ? slots[index].Icon : null;
        }

        public void RegisterBook(IPlayableUnit book)
        {
            _bookUnit = book;
            _bookUnit.SetMovementActive(false);
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
        }

        public void ToggleActiveUnit()
        {
            if (!IsBookDeployed) return;

            var naraAsPlayable = _naraController as IPlayableUnit;
            if (ActiveUnit == naraAsPlayable)
                SetBookAsActiveUnit(_bookUnit);
            else
                SetNaraAsActiveUnit();
        }

        // Redirects the camera to orbit the unit that just became active.
        private void FollowActiveUnit()
        {
            if (_worldCamera == null || ActiveUnit == null) return;
            var target = ActiveUnit.UnitViewGO?.transform;
            if (target != null)
                _worldCamera.StartFollowTarget(target);
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
