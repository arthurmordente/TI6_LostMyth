using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.Core.Mvc.LoadingScreen
{
    [CreateAssetMenu(fileName = "LoadingTipPool", menuName = "Scriptable Objects/Loading/Loading Tip Pool")]
    public sealed class LoadingTipPoolSO : ScriptableObject
    {
        [SerializeField] private List<LoadingTipCanvasView> _tipPrefabs = new();

        public IReadOnlyList<LoadingTipCanvasView> TipPrefabs => _tipPrefabs;

        public LoadingTipCanvasView PickRandom()
        {
            if (_tipPrefabs == null || _tipPrefabs.Count == 0)
                return null;

            int validCount = 0;
            for (int i = 0; i < _tipPrefabs.Count; i++)
            {
                if (_tipPrefabs[i] != null)
                    validCount++;
            }

            if (validCount == 0)
                return null;

            int pick = Random.Range(0, validCount);
            for (int i = 0; i < _tipPrefabs.Count; i++)
            {
                if (_tipPrefabs[i] == null)
                    continue;

                if (pick == 0)
                    return _tipPrefabs[i];

                pick--;
            }

            return null;
        }
    }
}
