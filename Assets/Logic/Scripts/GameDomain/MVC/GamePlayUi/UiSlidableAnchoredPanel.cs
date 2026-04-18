using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui
{
    /// <summary>
    /// Dois estados fixos no mesmo <see cref="_target"/>: pose <b>aberta</b> = valor no prefab; <b>fechado</b> = aberto + <see cref="_hiddenOffset"/>.
    /// Ex.: fechado 700 px à esquerda do aberto → offset (-700, 0).
    /// <see cref="_startExpanded"/>: após o 1.º frame, manter aberto ou animar para fechado.
    /// Botões: dois <see cref="Button"/> diferentes (recolher / expandir) ou um só — preencha só um campo, ou o mesmo <see cref="Button"/> em Toggle e Expand; nesse caso o clique alterna e os botões não são desativados.
    /// Modo dois botões: ao fechar com tween, o controlo de reabrir só é mostrado no fim (slide do painel principal).
    /// Opcionalmente esse controlo é outro <see cref="UiSlidableAnchoredPanel"/> (desliza para dentro/fora em vez de SetActive).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiSlidableAnchoredPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [Tooltip("Fechado = aberto + este vetor (ex.: (-700,0) desliza o fechado para a esquerda).")]
        [SerializeField] private Vector2 _hiddenOffset;
        [SerializeField] private float _duration = 0.35f;
        [SerializeField] private Ease _ease = Ease.OutQuad;
        [Tooltip("Prefab = pose ABERTA.\nLigado: após o 1.º frame mantém-se aberto.\nDesligado: após o 1.º frame anima para fechado.")]
        [SerializeField] private bool _startExpanded = true;
        [Tooltip("Recolher. Se Expand for outro Button: só fecha. Se Expand for o mesmo, só um dos dois, ou vazio com Expand preenchido: um botão alterna abrir/fechar.")]
        [SerializeField] private Button _toggleButton;
        [Tooltip("Expandir. Se for outro Button que Toggle: só abre. Mesmo que Toggle, ou só este preenchido: alterna abrir/fechar.")]
        [SerializeField] private Button _expandButton;
        [Tooltip("Só modo 2 botões. Slidable no mesmo objeto (ou pai) do botão de reabrir: entra em tween quando o menu principal fecha; sai quando abre. Vazio = usar SetActive no Expand Button.")]
        [SerializeField] private UiSlidableAnchoredPanel _expandHandleSlidable;

        private Vector2 _openPos;
        private Vector2 _closedPos;
        private bool _expanded;
        private bool _ready;
        private bool? _pendingExpanded;

        public bool IsExpanded => _expanded;

        /// <summary>Dois botões distintos: Toggle = recolher, Expand = abrir. Qualquer outra combinação = um único botão que alterna.</summary>
        private bool IsDualButtonMode() =>
            _toggleButton != null && _expandButton != null && _toggleButton != _expandButton;

        /// <summary>Botão único para alternar (só Toggle, só Expand, ou o mesmo nos dois campos).</summary>
        private Button ResolveSingleToggleButton()
        {
            if (IsDualButtonMode()) return null;
            if (_toggleButton != null && _expandButton != null) return _toggleButton;
            return _toggleButton != null ? _toggleButton : _expandButton;
        }

        private void Awake()
        {
            if (_target == null)
                _target = (RectTransform)transform;

            Vector2 prefabOpen = _target.anchoredPosition;
            _openPos = prefabOpen;
            _closedPos = prefabOpen + _hiddenOffset;

            _expanded = _startExpanded;
            // Sempre partir da pose aberta no ecrã; se começarmos fechados, a animação para fechado corre no Start (evita salto sem tween).
            _target.anchoredPosition = _openPos;

            WireButtons();
            SyncButtonVisibility(expandHandleInstant: true);
        }

        private void WireButtons()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(OnCollapseClicked);
                _toggleButton.onClick.RemoveListener(OnSharedToggleClicked);
            }

            if (_expandButton != null)
            {
                _expandButton.onClick.RemoveListener(OnExpandClicked);
                _expandButton.onClick.RemoveListener(OnSharedToggleClicked);
            }

            if (IsDualButtonMode())
            {
                _toggleButton.onClick.AddListener(OnCollapseClicked);
                _expandButton.onClick.AddListener(OnExpandClicked);
            }
            else
            {
                var single = ResolveSingleToggleButton();
                if (single != null)
                    single.onClick.AddListener(OnSharedToggleClicked);
            }
        }

        private void OnCollapseClicked() => SetExpanded(false);

        private void OnExpandClicked() => SetExpanded(true);

        private void OnSharedToggleClicked() => Toggle();

        private IEnumerator Start()
        {
            yield return null;
            _ready = true;
            if (_pendingExpanded.HasValue)
            {
                _expanded = _pendingExpanded.Value;
                _pendingExpanded = null;
            }

            if (_expanded)
                Apply(instant: true);
            else
                Apply(instant: false);
        }

        private void OnDestroy()
        {
            if (_toggleButton != null)
            {
                _toggleButton.onClick.RemoveListener(OnCollapseClicked);
                _toggleButton.onClick.RemoveListener(OnSharedToggleClicked);
            }

            if (_expandButton != null)
            {
                _expandButton.onClick.RemoveListener(OnExpandClicked);
                _expandButton.onClick.RemoveListener(OnSharedToggleClicked);
            }

            if (_target != null)
                DOTween.Kill(_target, true);
        }

        public void Toggle() => SetExpanded(!_expanded);

        public void SetExpanded(bool expanded, bool instant = false)
        {
            if (!_ready)
            {
                _pendingExpanded = expanded;
                return;
            }

            bool changed = _expanded != expanded;
            _expanded = expanded;
            if (!changed && !instant)
                return;
            Apply(instant);
        }

        private void Apply(bool instant)
        {
            Vector2 dest = _expanded ? _openPos : _closedPos;
            DOTween.Kill(_target, true);

            if (instant || _duration <= 0f)
            {
                _target.anchoredPosition = dest;
                SyncButtonVisibility(expandHandleInstant: true);
                return;
            }

            Tweener tweener = _target.DOAnchorPos(dest, _duration).SetEase(_ease).SetTarget(_target);

            if (IsDualButtonMode())
            {
                if (_expanded)
                    SyncButtonVisibility(expandHandleInstant: false);
                else
                {
                    _toggleButton.gameObject.SetActive(false);
                    if (_expandHandleSlidable != null)
                        _expandHandleSlidable.SetExpanded(false, true);
                    else
                        _expandButton.gameObject.SetActive(false);
                }

                tweener.OnComplete(OnDualButtonSlideComplete);
            }
        }

        private void OnDualButtonSlideComplete()
        {
            if (this == null) return;
            SyncButtonVisibility(expandHandleInstant: false);
        }

        /// <param name="expandHandleInstant">Quando o controlo de expandir usa <see cref="_expandHandleSlidable"/>, define se o slide dele é instantâneo.</param>
        private void SyncButtonVisibility(bool expandHandleInstant)
        {
            if (!IsDualButtonMode())
                return;
            _toggleButton.gameObject.SetActive(_expanded);

            if (_expandHandleSlidable != null)
                _expandHandleSlidable.SetExpanded(!_expanded, expandHandleInstant);
            else
                _expandButton.gameObject.SetActive(!_expanded);
        }
    }
}
