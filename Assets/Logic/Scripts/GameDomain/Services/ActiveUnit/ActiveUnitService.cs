using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.MVC.Nara;
using UnityEngine;

namespace Logic.Scripts.GameDomain.Services.ActiveUnit
{
    public class ActiveUnitService : IActiveUnitService
    {
        private readonly INaraController _naraController;
        private IPlayableUnit _bookUnit;

        public IPlayableUnit ActiveUnit { get; private set; }
        public bool IsBookDeployed => _bookUnit != null;

        public ActiveUnitService(INaraController naraController)
        {
            _naraController = naraController;
            ActiveUnit = naraController as IPlayableUnit;
        }

        public void RegisterBook(IPlayableUnit book)
        {
            _bookUnit = book;
            // Book starts with movement disabled; TAB or explicit call enables it
            _bookUnit.SetMovementActive(false);
        }

        public void UnregisterBook()
        {
            _bookUnit = null;
            // Ensure Nara is active again
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
        }

        public void SetBookAsActiveUnit(IPlayableUnit book)
        {
            if (!IsBookDeployed || book == null) return;

            ActiveUnit?.SetMovementActive(false);
            ActiveUnit?.OnBecomeInactive();
            ActiveUnit = book;
            ActiveUnit?.SetMovementActive(true);
            ActiveUnit?.OnBecomeActive();
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
    }
}
