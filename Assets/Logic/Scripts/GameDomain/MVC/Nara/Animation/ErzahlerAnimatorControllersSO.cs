using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Nara.Animation
{
    [CreateAssetMenu(fileName = "ErzahlerAnimatorControllers", menuName = "ScriptableObjects/Animation/Erzahler Animator Controllers")]
    public class ErzahlerAnimatorControllersSO : ScriptableObject
    {
        private const string ResourcesName = "ErzahlerAnimatorControllers";

        public static ErzahlerAnimatorControllersSO LoadDefault()
        {
            return Resources.Load<ErzahlerAnimatorControllersSO>(ResourcesName);
        }

        [Tooltip("Default player rig: Erzahler holding the book (no clone deployed).")]
        public RuntimeAnimatorController ErzahlerWithBook;

        [Tooltip("Player rig when the Book clone is deployed (Erzahler solo).")]
        public RuntimeAnimatorController ErzahlerSolo;

        [Tooltip("Deployed Book of Cagliostro clone unit.")]
        public RuntimeAnimatorController BookClone;

        [Tooltip("Madam Laki boss.")]
        public RuntimeAnimatorController LakiBoss;
    }
}
