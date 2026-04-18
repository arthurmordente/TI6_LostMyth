using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    /// <summary>
    /// Preview no prompt de rolagem: ativa 1–3 roots visuais conforme <see cref="LakiDiceAttackState.PlayerDiceCount"/>.
    /// Cada root pode ser um painel com Image + TMP filhos — só precisas arrastar o <b>GameObject pai</b> do dado; o texto é
    /// <see cref="TMP_Text"/> encontrado com <c>GetComponentInChildren</c> (em cache no <see cref="Awake"/>).
    /// Dica de clique: <see cref="_mouseHintObject"/> começa desativado; após <see cref="_mouseHintDelaySeconds"/> é ativado e
    /// faz um movimento vertical curto a cada <see cref="_mouseHintBobIntervalSeconds"/>.
    /// O temporizador reinicia quando <see cref="DiceAttackUIRuntime.NotifyPlayerRollIdleHintReset"/> dispara.
    /// </summary>
    public class DicePromptUI : MonoBehaviour
    {
        [Header("Preview — um root por dado (liga o pai: imagem + número como filhos)")]
        [SerializeField] private GameObject _dieVisualSlot1;
        [SerializeField] private GameObject _dieVisualSlot2;
        [SerializeField] private GameObject _dieVisualSlot3;
        [SerializeField] [Min(0.05f)] private float _stepSeconds = 0.12f;

        [Header("Legacy — TMP direto (só se o slot correspondente acima estiver vazio)")]
        [FormerlySerializedAs("_rollingValueText")]
        [SerializeField] private TMP_Text _dieFace1Text;
        [SerializeField] private TMP_Text _dieFace2Text;
        [SerializeField] private TMP_Text _dieFace3Text;

        [Header("Dica de clique")]
        [Tooltip("Objeto na hierarquia (começa desligado no arranque do prompt; ativado após o delay).")]
        [SerializeField] private GameObject _mouseHintObject;
        [Tooltip("Segundos até ativar a dica.")]
        [SerializeField] private float _mouseHintDelaySeconds = 5f;
        [Tooltip("Intervalo entre cada ciclo subir/descer (unscaled).")]
        [SerializeField] private float _mouseHintBobIntervalSeconds = 1f;
        [SerializeField] private float _mouseHintBobOffsetPixels = 10f;
        [SerializeField] private float _mouseHintBobHalfDuration = 0.15f;

        private readonly GameObject[] _resolvedRoots = new GameObject[3];
        private readonly TMP_Text[] _resolvedTexts = new TMP_Text[3];

        private Coroutine _cycle;
        private Coroutine _hintCycle;
        private RectTransform _mouseHintRect;

        private void Awake()
        {
            ResolveDieSlots();
            if (_mouseHintObject != null)
                _mouseHintRect = _mouseHintObject.GetComponent<RectTransform>();
        }

        private void ResolveDieSlots()
        {
            var slotRoots = new[] { _dieVisualSlot1, _dieVisualSlot2, _dieVisualSlot3 };
            var legacyTexts = new[] { _dieFace1Text, _dieFace2Text, _dieFace3Text };

            for (int i = 0; i < 3; i++)
            {
                if (slotRoots[i] != null)
                {
                    _resolvedRoots[i] = slotRoots[i];
                    _resolvedTexts[i] = slotRoots[i].GetComponentInChildren<TMP_Text>(true);
                }
                else if (legacyTexts[i] != null)
                {
                    _resolvedTexts[i] = legacyTexts[i];
                    _resolvedRoots[i] = legacyTexts[i].gameObject;
                }
                else
                {
                    _resolvedRoots[i] = null;
                    _resolvedTexts[i] = null;
                }
            }

            if (_resolvedTexts[0] == null && _resolvedTexts[1] == null && _resolvedTexts[2] == null)
            {
                var found = GetComponentInChildren<TMP_Text>(true);
                if (found != null)
                {
                    _resolvedTexts[0] = found;
                    _resolvedRoots[0] = found.gameObject;
                }
            }
        }

        private void OnEnable()
        {
            StopRollingCycle();
            StopHintCoroutineAndResetVisual();
            _cycle = StartCoroutine(CycleRollingValues());
            DiceAttackUIRuntime.OnPlayerRollIdleHintReset += RestartMouseHintTimer;
            RestartMouseHintTimer();
        }

        private void OnDisable()
        {
            DiceAttackUIRuntime.OnPlayerRollIdleHintReset -= RestartMouseHintTimer;
            StopRollingCycle();
            StopHintCoroutineAndResetVisual();
        }

        private void RestartMouseHintTimer()
        {
            if (!isActiveAndEnabled) return;
            StopHintCoroutineAndResetVisual();
            if (_mouseHintObject != null)
                _hintCycle = StartCoroutine(MouseHintRoutine());
        }

        private void StopRollingCycle()
        {
            if (_cycle != null)
            {
                StopCoroutine(_cycle);
                _cycle = null;
            }
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

        private IEnumerator CycleRollingValues()
        {
            var wait = new WaitForSeconds(_stepSeconds);
            while (true)
            {
                int min = LakiDiceAttackState.PlayerFaceMin;
                int max = LakiDiceAttackState.PlayerFaceMax;
                if (max < min) { int t = min; min = max; max = t; }

                int configured = Mathf.Clamp(LakiDiceAttackState.PlayerDiceCount, 1, 3);
                ApplyVisibleSlots(configured);

                for (int i = 0; i < configured; i++)
                {
                    var tmp = _resolvedTexts[i];
                    if (tmp != null)
                        tmp.SetText(Random.Range(min, max + 1).ToString());
                }

                yield return wait;
            }
        }

        private void ApplyVisibleSlots(int count)
        {
            for (int i = 0; i < 3; i++)
            {
                var root = _resolvedRoots[i];
                if (root != null)
                    root.SetActive(i < count);
            }
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
