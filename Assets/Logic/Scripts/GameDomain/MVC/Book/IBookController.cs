using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Book
{
    public interface IBookController : IPlayableUnit
    {
        bool IsDeployed { get; }

        /// <summary>Instantiates the book prefab at the given world position and starts tracking it.</summary>
        void CreateBook(Vector3 position);

        /// <summary>Destroys the book view and cleans up all state.</summary>
        void DestroyBook();

        /// <summary>Reset movement area after teleport/phase start (mirrors NaraTurnMovementController.ResetMovementArea).</summary>
        void ResetMovementArea();

        /// <summary>Gain AP for a new player turn.</summary>
        void GainTurnActionPoints();
    }
}
