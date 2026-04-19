using UnityEngine;

namespace Logic.Scripts.GameDomain.Exploration
{
    /// <summary>
    /// Quando suprimido, a ação <c>Exploration.Interact</c> (ex.: F junto ao NPC) não executa o comando de interação.
    /// O <c>Player.Interact</c> no gameplay não é afetado. Use <see cref="Push"/> / <see cref="Pop"/> para aninhar.
    /// </summary>
    public static class ExplorationInteractInputGate
    {
        private static int _suppressDepth;

        public static bool IsSuppressed => _suppressDepth > 0;

        public static void Push() => _suppressDepth++;

        public static void Pop() => _suppressDepth = Mathf.Max(0, _suppressDepth - 1);
    }
}
