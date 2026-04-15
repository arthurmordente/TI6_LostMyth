using Logic.Scripts.GameDomain.MVC.Shared;

namespace Logic.Scripts.GameDomain.Services.ActiveUnit
{
    public interface IActiveUnitService
    {
        IPlayableUnit ActiveUnit { get; }
        bool IsBookDeployed { get; }

        void SetNaraAsActiveUnit();
        void SetBookAsActiveUnit(IPlayableUnit book);
        void ToggleActiveUnit();
        void RegisterBook(IPlayableUnit book);
        void UnregisterBook();

        /// <summary>Updates the four skill mana labels from the active unit's abilities.</summary>
        void RefreshHudAbilityCosts();
    }
}
