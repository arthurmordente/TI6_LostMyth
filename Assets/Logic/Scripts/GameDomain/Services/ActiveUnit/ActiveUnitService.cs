using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Ui;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.ActiveUnit
{
    public class ActiveUnitService : IActiveUnitService
    {
        private readonly INaraController _naraController;
        private readonly IWorldCameraController _worldCamera;
        private readonly IGamePlayUiController _gamePlayUiController;
        private IPlayableUnit _bookUnit;

        public IPlayableUnit ActiveUnit { get; private set; }
        public bool IsBookDeployed => _bookUnit != null;

        public ActiveUnitService(INaraController naraController, IWorldCameraController worldCameraController,
            IGamePlayUiController gamePlayUiController)
        {
            _naraController = naraController;
            _worldCamera = worldCameraController;
            _gamePlayUiController = gamePlayUiController;
            ActiveUnit = naraController as IPlayableUnit;
        }

        public void RefreshHudAbilityCosts() => PushAbilityCostsToHud();

        private void PushAbilityCostsToHud()
        {
            if (_gamePlayUiController == null || ActiveUnit == null) return;
            var abs = ActiveUnit.GetAbilities();
            int c(int i) => abs != null && i < abs.Length && abs[i] != null ? abs[i].GetCost() : 0;
            _gamePlayUiController.SetAbilityManaCosts(c(0), c(1), c(2), c(3));
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
                PushAbilityCostsToHud();
                FollowActiveUnit();
                return;
            }

            ActiveUnit?.SetMovementActive(false);
            ActiveUnit?.OnBecomeInactive();
            ActiveUnit = naraPlayable;
            ActiveUnit?.SetMovementActive(true);
            ActiveUnit?.OnBecomeActive();

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
    }
}
