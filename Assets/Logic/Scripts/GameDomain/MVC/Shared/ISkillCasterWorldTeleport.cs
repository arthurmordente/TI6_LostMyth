using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Shared
{
    /// <summary>
    /// Skill-driven world teleport for units whose physics position is driven by a Rigidbody.
    /// Does not update arena movement radius — pairing cast flow calls <see cref="IPlayableUnit.SyncArenaMovementAfterMovementSkillDisplacement"/> for movement skills.
    /// </summary>
    public interface ISkillCasterWorldTeleport
    {
        void TeleportToWorldPosition(Vector3 worldPosition);
    }
}
