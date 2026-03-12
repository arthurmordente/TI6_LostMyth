using Logic.Scripts.Core.Mvc.WorldCamera;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.ActiveUnit
{
    public class ActiveUnitService : IActiveUnitService
    {
        private readonly INaraController _naraController;
        private readonly IWorldCameraController _worldCamera;
        private IPlayableUnit _bookUnit;

        public IPlayableUnit ActiveUnit { get; private set; }
        public bool IsBookDeployed => _bookUnit != null;

        public ActiveUnitService(INaraController naraController, IWorldCameraController worldCameraController)
        {
            _naraController = naraController;
            _worldCamera = worldCameraController;
            ActiveUnit = naraController as IPlayableUnit;
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
            if (ActiveUnit == _naraController as IPlayableUnit) return;

            ActiveUnit?.SetMovementActive(false);
            ActiveUnit?.OnBecomeInactive();
            ActiveUnit = _naraController as IPlayableUnit;
            ActiveUnit?.SetMovementActive(true);
            ActiveUnit?.OnBecomeActive();

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
