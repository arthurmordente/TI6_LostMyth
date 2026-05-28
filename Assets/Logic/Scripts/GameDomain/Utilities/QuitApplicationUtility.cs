using UnityEngine;

namespace Logic.Scripts.GameDomain.Utilities
{
    public static class QuitApplicationUtility
    {
        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
