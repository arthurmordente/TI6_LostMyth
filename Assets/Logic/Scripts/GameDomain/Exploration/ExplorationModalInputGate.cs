using UnityEngine;

namespace Logic.Scripts.GameDomain.Exploration
{
    /// <summary>
    /// Quando suprimido, inputs de exploração (movimento, câmara, interação) não disparam comandos.
    /// O botão Pause/ESC continua ativo. Use <see cref="Push"/> / <see cref="Pop"/> para aninhar modais.
    /// </summary>
    public static class ExplorationModalInputGate
    {
        private static int _suppressDepth;

        public static bool IsSuppressed => _suppressDepth > 0;

        public static void Push() => _suppressDepth++;

        public static void Pop() => _suppressDepth = Mathf.Max(0, _suppressDepth - 1);
    }
}
