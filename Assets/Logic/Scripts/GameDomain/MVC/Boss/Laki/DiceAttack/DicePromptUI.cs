using System.Collections;
using TMPro;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    /// <summary>
    /// While this object is active, updates <see cref="_rollingValueText"/> every <see cref="_stepSeconds"/>
    /// cycling through valid player die faces from <see cref="LakiDiceAttackState"/>.
    /// </summary>
    public class DicePromptUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rollingValueText;
        [SerializeField] [Min(0.05f)] private float _stepSeconds = 0.3f;

        private Coroutine _cycle;

        private void Awake()
        {
            if (_rollingValueText == null)
                _rollingValueText = GetComponentInChildren<TMP_Text>(true);
        }

        private void OnEnable()
        {
            if (_cycle != null)
            {
                StopCoroutine(_cycle);
                _cycle = null;
            }
            _cycle = StartCoroutine(CycleRollingValues());
        }

        private void OnDisable()
        {
            if (_cycle != null)
            {
                StopCoroutine(_cycle);
                _cycle = null;
            }
        }

        private IEnumerator CycleRollingValues()
        {
            var wait = new WaitForSeconds(_stepSeconds);
            int cursor = 0;

            while (true)
            {
                int min = LakiDiceAttackState.PlayerFaceMin;
                int max = LakiDiceAttackState.PlayerFaceMax;
                if (max < min) { int t = min; min = max; max = t; }
                int span = max - min + 1;
                int value = min + (cursor % span);
                cursor++;

                if (_rollingValueText != null)
                    _rollingValueText.SetText(value.ToString());

                yield return wait;
            }
        }
    }
}
