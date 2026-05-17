using UnityEngine;

namespace Logic.Scripts.GameDomain.Exploration.QuitConfirmation
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
