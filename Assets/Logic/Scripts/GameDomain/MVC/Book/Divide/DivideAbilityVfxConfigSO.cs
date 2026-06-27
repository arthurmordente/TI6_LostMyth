using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Book.Divide
{
    [CreateAssetMenu(
        fileName = "DivideAbilityVfxConfig",
        menuName = "Scriptable Objects/Book/Divide Ability VFX Config")]
    public sealed class DivideAbilityVfxConfigSO : ScriptableObject
    {
        [Header("Confirm / Cancel (one-shot at spawn point)")]
        [Tooltip("VFX ao confirmar o spawn do clone (mesma posição devolvida por LockAim).")]
        [SerializeField] private GameObject _confirmSpawnVfx;

        [Tooltip("VFX ao cancelar a mira (última posição do preview antes de o destruir).")]
        [SerializeField] private GameObject _cancelAimVfx;

        public GameObject ConfirmSpawnVfx => _confirmSpawnVfx;
        public GameObject CancelAimVfx => _cancelAimVfx;
    }
}
