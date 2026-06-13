using UnityEngine;

namespace Logic.Scripts.GameDomain.VisualFeedback.FloatingCombatNumbers
{
    public class FloatingCombatNumberService : IFloatingCombatNumberService
    {
        readonly FloatingCombatNumberView _prefab;
        readonly Transform _root;

        public FloatingCombatNumberService(FloatingCombatNumberView prefab)
        {
            _prefab = prefab;
            var rootGo = new GameObject("FloatingCombatNumbers");
            Object.DontDestroyOnLoad(rootGo);
            _root = rootGo.transform;
        }

        public void Show(Transform anchor, int amount, FloatingCombatNumberKind kind)
        {
            if (anchor == null || amount <= 0) return;

            FloatingCombatNumberView view;
            if (_prefab != null)
                view = Object.Instantiate(_prefab, _root);
            else
                view = FloatingCombatNumberView.CreateRuntimeFallback();

            if (view.transform.parent != _root)
                view.transform.SetParent(_root, false);

            view.Play(anchor, amount, kind);
        }
    }
}
