namespace Logic.Scripts.GameDomain.MVC.Book.Divide
{
    public interface IDivideAbilityHandler
    {
        bool IsBookDeployed { get; }
        bool IsAiming { get; }
        bool CanUseDivideCommandThisTurn { get; }

        /// <summary>
        /// Called when the player presses the Dividir button (C).
        /// - If book not deployed and command available: starts aiming mode.
        /// - If book deployed and command available: recalls the book immediately.
        /// - If already used this turn: no-op.
        /// </summary>
        void Activate();

        /// <summary>
        /// Confirms book placement at the world point under the mouse cursor.
        /// Only effective while IsAiming is true.
        /// </summary>
        void ConfirmPlacement();

        /// <summary>Cancels the current aiming state without deploying the book.</summary>
        void CancelAim();

        /// <summary>
        /// Must be called at the start of each player turn.
        /// Ticks down the cooldown and grants the book its turn action points.
        /// </summary>
        void OnPlayerTurnStart();

        /// <summary>Called at the end of a player turn so the book's movement area can be reset next turn.</summary>
        void OnPlayerTurnEnd();
    }
}
