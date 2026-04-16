using Logic.Scripts.Core.Mvc.WorldCamera;
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
        private readonly INaraController _naraController;
        private readonly IWorldCameraController _worldCamera;
        private readonly IGamePlayUiController _gamePlayUiController;
        private readonly IPaschoalSkillLoadoutService _paschoalSkillLoadoutService;
        private IPlayableUnit _bookUnit;

        public IPlayableUnit ActiveUnit { get; private set; }
        public bool IsBookDeployed => _bookUnit != null;

        public ActiveUnitService(INaraController naraController, IWorldCameraController worldCameraController,
            IGamePlayUiController gamePlayUiController, [InjectOptional] IPaschoalSkillLoadoutService paschoalSkillLoadoutService = null)
        {
            _naraController = naraController;
            _worldCamera = worldCameraController;
            _gamePlayUiController = gamePlayUiController;
            _paschoalSkillLoadoutService = paschoalSkillLoadoutService;
            ActiveUnit = naraController as IPlayableUnit;
        }

        public void RefreshHudAbilityCosts() => PushAbilityCostsToHud();

        private void PushAbilityCostsToHud()
        {
            if (_gamePlayUiController == null || ActiveUnit == null) return;
            ReloadPaschoalLoadoutForUnit(ActiveUnit);

            var unitView = ActiveUnit.UnitViewGO;
            if (unitView != null)
            {
                var legacyToggle = unitView.GetComponent<LegacySkillSystemToggle>();
                bool useLegacy = legacyToggle != null && legacyToggle.UseLegacySkillSystem;
                if (!useLegacy)
                {
                    var paschoalLoadout = unitView.GetComponent<PaschoalSkillLoadout>();
                    if (paschoalLoadout != null)
                    {
                        int paschoalCostAt(int i)
                        {
                            if (!paschoalLoadout.TryGetSkill(i, out SkillDataSO skill) || skill == null) return 0;
                            return Mathf.Max(0, skill.Cost);
                        }
                        _gamePlayUiController.SetAbilityManaCosts(paschoalCostAt(0), paschoalCostAt(1), paschoalCostAt(2), paschoalCostAt(3));
                        return;
                    }
                }
            }

            var abs = ActiveUnit.GetAbilities();
            int legacyCostAt(int i) => abs != null && i < abs.Length && abs[i] != null ? abs[i].GetCost() : 0;
            _gamePlayUiController.SetAbilityManaCosts(legacyCostAt(0), legacyCostAt(1), legacyCostAt(2), legacyCostAt(3));
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

        private void ReloadPaschoalLoadoutForUnit(IPlayableUnit unit)
        {
            if (unit == null || _paschoalSkillLoadoutService == null) return;
            var unitView = unit.UnitViewGO;
            if (unitView == null) return;

            var paschoalLoadout = unitView.GetComponent<PaschoalSkillLoadout>();
            if (paschoalLoadout == null) return;

            paschoalLoadout.SetSkills(_paschoalSkillLoadoutService.BuildRuntimeSlotsArray());
        }
    }
}
