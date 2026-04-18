using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui
{
    /// <summary>
    /// Painel que desliza em <see cref="_target"/> por <c>anchoredPosition</c>.
    /// <b>Modo clássico (padrão):</b> dois estados — aberto = pose do prefab; fechado = aberto + <see cref="_hiddenOffset"/>.
    /// <b>Modo multi-estado (opcional):</b> N offsets a partir da pose aberta — índice 0 = mais fechado, último = totalmente aberto (offset zero).
    /// Botões: ver doc do modo clássico; em multi-estado, <see cref="SetExpanded(bool)"/> mapeia só fechado (0) e último índice (aberto).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiSlidableAnchoredPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform _target;
        [Tooltip("Modo clássico: fechado = aberto + este vetor. Ignorado em modo multi-estado (usa primeiro offset da lista).")]
        [SerializeField] private Vector2 _hiddenOffset;
        [SerializeField] private float _duration = 0.35f;
        [SerializeField] private Ease _ease = Ease.OutQuad;
        [Tooltip("Prefab = pose ABERTA (último estado em multi-estado).\nLigado: após o 1.º frame mantém-se no estado inicial escolhido.\nDesligado: após o 1.º frame anima para fechado (índice 0).")]
        [SerializeField] private bool _startExpanded = true;
        [Tooltip("Recolher. Se Expand for outro Button: só fecha. Se Expand for o mesmo, só um dos dois, ou vazio com Expand preenchido: um botão alterna abrir/fechar.")]
        [SerializeField] private Button _toggleButton;
        [Tooltip("Expandir. Se for outro Button que Toggle: só abre. Mesmo que Toggle, ou só este preenchido: alterna abrir/fechar.")]
        [SerializeField] private Button _expandButton;
        [Tooltip("Só modo 2 botões. Slidable no mesmo objeto (ou pai) do botão de reabrir: entra em tween quando o menu principal fecha; sai quando abre. Vazio = usar SetActive no Expand Button.")]
        [SerializeField] private UiSlidableAnchoredPanel _expandHandleSlidable;

        [Header("Multi-estado (opcional)")]
        [Tooltip("Se ligado, usa _multiStateOffsetsFromOpen em vez de _hiddenOffset. Comprimento ≥ 2.")]
        [SerializeField] private bool _multiStateMode;
        [Tooltip("Pose aberta = prefab. Estado i = aberto + offset[i]. [0] = mais escondido; último = tipicamente (0,0) para totalmente visível.")]
        [SerializeField] private Vector2[] _multiStateOffsetsFromOpen = new Vector2[0];

        private Vector2 _openPos;
        private bool _expanded;
        private bool _ready;
        private bool? _pendingExpanded;
        private int? _pendingStateIndex;
        private int _stateIndex;

        public bool IsExpanded => _expanded;

        /// <summary>Índice do estado atual em modo multi-estado; em modo clássico, 0 = fechado, 1 = aberto.</summary>
        public int CurrentStateIndex => _multiStateMode && MultiStateCount > 0 ? _stateIndex : (_expanded ? 1 : 0);

        public int StateCount => _multiStateMode && MultiStateCount > 0 ? MultiStateCount : 2;

        private int MultiStateCount => _multiStateOffsetsFromOpen != null ? _multiStateOffsetsFromOpen.Length : 0;

        private bool IsDualButtonMode() =>
            _toggleButton != null && _expandButton != null && _toggleButton != _expandButton;

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

            if (UseMultiStateEffective())
            {
                _stateIndex = _startExpanded ? MultiStateCount - 1 : 0;
                _expanded = _stateIndex >= MultiStateCount - 1;
            }
            else
                _expanded = _startExpanded;

            // Igual ao modo clássico: 1.ª pose = aberto (prefab); Start() anima para fechado se necessário.
            _target.anchoredPosition = _openPos;

            WireButtons();
            SyncButtonVisibility(expandHandleInstant: true);
        }

        private bool UseMultiStateEffective() => _multiStateMode && MultiStateCount >= 2;

        private Vector2 GetAnchoredPosForStateIndex(int index)
        {
            if (UseMultiStateEffective())
            {
                index = Mathf.Clamp(index, 0, MultiStateCount - 1);
                return _openPos + _multiStateOffsetsFromOpen[index];
            }
            return _expanded ? _openPos : _openPos + _hiddenOffset;
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
            if (_pendingStateIndex.HasValue)
            {
                int idx = _pendingStateIndex.Value;
                _pendingStateIndex = null;
                SetStateIndex(idx, instant: true);
            }
            else if (_pendingExpanded.HasValue)
            {
                _expanded = _pendingExpanded.Value;
                _pendingExpanded = null;
                if (UseMultiStateEffective())
                    _stateIndex = _expanded ? MultiStateCount - 1 : 0;
                Apply(instant: true);
            }
            else
            {
                if (_expanded)
                    Apply(instant: true);
                else
                    Apply(instant: false);
            }
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

        public void Toggle()
        {
            if (UseMultiStateEffective())
            {
                int last = MultiStateCount - 1;
                SetStateIndex(_stateIndex >= last ? 0 : last);
            }
            else
                SetExpanded(!_expanded);
        }

        /// <summary>
        /// Modo clássico: false = fechado, true = aberto.
        /// Multi-estado: false = índice 0, true = último índice (aberto máximo).
        /// </summary>
        public void SetExpanded(bool expanded, bool instant = false)
        {
            if (UseMultiStateEffective())
            {
                SetStateIndex(expanded ? MultiStateCount - 1 : 0, instant);
                return;
            }

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

        /// <summary>
        /// Multi-estado: 0 = mais fechado, <see cref="StateCount"/>-1 = aberto. Modo clássico: 0 = fechado, 1 = aberto (outros valores clampeados).
        /// </summary>
        public void SetStateIndex(int stateIndex, bool instant = false)
        {
            if (!UseMultiStateEffective())
            {
                SetExpanded(stateIndex > 0, instant);
                return;
            }

            if (!_ready)
            {
                _pendingStateIndex = stateIndex;
                return;
            }

            stateIndex = Mathf.Clamp(stateIndex, 0, MultiStateCount - 1);
            bool changed = _stateIndex != stateIndex;
            _stateIndex = stateIndex;
            _expanded = _stateIndex >= MultiStateCount - 1;
            if (!changed && !instant)
                return;
            Apply(instant);
        }

        private void Apply(bool instant)
        {
            Vector2 dest = UseMultiStateEffective()
                ? GetAnchoredPosForStateIndex(_stateIndex)
                : (_expanded ? _openPos : _openPos + _hiddenOffset);
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
