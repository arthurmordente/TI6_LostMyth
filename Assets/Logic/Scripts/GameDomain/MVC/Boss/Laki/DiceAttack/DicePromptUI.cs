using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    /// <summary>
    /// Preview no prompt de rolagem: ativa 1–3 roots visuais conforme <see cref="LakiDiceAttackState.PlayerDiceCount"/>.
    /// Cada root pode ser um painel com Image + texto fixo no prefab — só precisas arrastar o GameObject pai do dado.
    /// Dica de clique: <see cref="_mouseHintObject"/> começa desativado; após <see cref="_mouseHintDelaySeconds"/> é ativado e
    /// faz um movimento vertical curto a cada <see cref="_mouseHintBobIntervalSeconds"/>.
    /// O temporizador reinicia quando <see cref="DiceAttackUIRuntime.NotifyPlayerRollIdleHintReset"/> dispara.
    /// </summary>
    public class DicePromptUI : MonoBehaviour
    {
        [Header("Preview — um root por dado (liga o pai: imagem + texto fixo como filhos)")]
        [SerializeField] private GameObject _dieVisualSlot1;
        [SerializeField] private GameObject _dieVisualSlot2;
        [SerializeField] private GameObject _dieVisualSlot3;

        [Header("Dica de clique")]
        [Tooltip("Objeto na hierarquia (começa desligado no arranque do prompt; ativado após o delay).")]
        [SerializeField] private GameObject _mouseHintObject;
        [Tooltip("Segundos até ativar a dica.")]
        [SerializeField] private float _mouseHintDelaySeconds = 5f;
        [Tooltip("Intervalo entre cada ciclo subir/descer (unscaled).")]
        [SerializeField] private float _mouseHintBobIntervalSeconds = 1f;
        [SerializeField] private float _mouseHintBobOffsetPixels = 10f;
        [SerializeField] private float _mouseHintBobHalfDuration = 0.15f;

        private Coroutine _hintCycle;
        private RectTransform _mouseHintRect;

        private void Awake()
        {
            if (_mouseHintObject != null)
                _mouseHintRect = _mouseHintObject.GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            ApplyVisibleDieSlots();
            StopHintCoroutineAndResetVisual();
            DiceAttackUIRuntime.OnPlayerRollIdleHintReset += RestartMouseHintTimer;
            RestartMouseHintTimer();
        }

        private void OnDisable()
        {
            DiceAttackUIRuntime.OnPlayerRollIdleHintReset -= RestartMouseHintTimer;
            StopHintCoroutineAndResetVisual();
        }

        private void ApplyVisibleDieSlots()
        {
            int count = Mathf.Clamp(LakiDiceAttackState.PlayerDiceCount, 1, 3);
            SetSlotActive(_dieVisualSlot1, count >= 1);
            SetSlotActive(_dieVisualSlot2, count >= 2);
            SetSlotActive(_dieVisualSlot3, count >= 3);
        }

        static void SetSlotActive(GameObject slot, bool active)
        {
            if (slot != null)
                slot.SetActive(active);
        }

        private void RestartMouseHintTimer()
        {
            if (!isActiveAndEnabled) return;
            StopHintCoroutineAndResetVisual();
            if (_mouseHintObject != null)
                _hintCycle = StartCoroutine(MouseHintRoutine());
        }

        private void StopHintCoroutineAndResetVisual()
        {
            if (_hintCycle != null)
            {
                StopCoroutine(_hintCycle);
                _hintCycle = null;
            }
            if (_mouseHintRect != null)
                DOTween.Kill(_mouseHintRect, false);
            if (_mouseHintObject != null)
                _mouseHintObject.SetActive(false);
        }

        private IEnumerator MouseHintRoutine()
        {
            if (_mouseHintObject == null) yield break;
            if (_mouseHintRect == null)
                _mouseHintRect = _mouseHintObject.GetComponent<RectTransform>();
            if (_mouseHintRect == null) yield break;

            float threshold = Mathf.Max(0.05f, _mouseHintDelaySeconds);
            float idle = 0f;
            while (idle < threshold && gameObject.activeInHierarchy)
            {
                idle += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!gameObject.activeInHierarchy) yield break;

            _mouseHintObject.SetActive(true);
            Vector2 baseAnchored = _mouseHintRect.anchoredPosition;
            float y0 = baseAnchored.y;
            float half = Mathf.Max(0.02f, _mouseHintBobHalfDuration);
            float bob = _mouseHintBobOffsetPixels;
            float interval = Mathf.Max(half * 2f, _mouseHintBobIntervalSeconds);

            while (gameObject.activeInHierarchy && _mouseHintObject != null && _mouseHintObject.activeSelf)
            {
                var up = _mouseHintRect.DOAnchorPosY(y0 + bob, half)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
                yield return up.WaitForCompletion(true);

                var down = _mouseHintRect.DOAnchorPosY(y0, half)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true);
                yield return down.WaitForCompletion(true);

                float waitRemain = interval - 2f * half;
                if (waitRemain > 0f)
                {
                    float w = 0f;
                    while (w < waitRemain && gameObject.activeInHierarchy)
                    {
                        w += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }
        }
    }
}
