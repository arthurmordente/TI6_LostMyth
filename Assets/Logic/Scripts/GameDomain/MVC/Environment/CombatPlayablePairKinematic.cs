using Logic.Scripts.GameDomain.MVC.Shared;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment
{
    /// <summary>
    /// When Nara and Book are both in combat, only the actively controlled unit stays dynamic.
    /// The other is kinematic so the pair does not push each other via physics.
    /// </summary>
    public static class CombatPlayablePairKinematic
    {
        public static void Sync(IPlayableUnit nara, IPlayableUnit book, bool bookDeployed, bool bookIsActiveUnit)
        {
            var naraRb = RigidbodyFrom(nara);
            if (!bookDeployed || book == null)
            {
                if (naraRb != null)
                    naraRb.isKinematic = false;
                return;
            }

            var bookRb = RigidbodyFrom(book);
            if (bookIsActiveUnit)
            {
                SetDynamic(bookRb);
                SetKinematic(naraRb);
            }
            else
            {
                SetDynamic(naraRb);
                SetKinematic(bookRb);
            }
        }

        private static Rigidbody RigidbodyFrom(IPlayableUnit unit)
        {
            if (unit?.UnitViewGO == null) return null;
            return unit.UnitViewGO.GetComponentInChildren<Rigidbody>();
        }

        private static void SetDynamic(Rigidbody rb)
        {
            if (rb == null) return;
            rb.isKinematic = false;
        }

        private static void SetKinematic(Rigidbody rb)
        {
            if (rb == null) return;
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }
    }
}
