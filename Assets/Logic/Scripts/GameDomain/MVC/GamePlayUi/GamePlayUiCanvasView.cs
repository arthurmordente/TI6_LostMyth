using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.GameDomain.MVC.Shared;
using Logic.Scripts.GameDomain.Services.Skills;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui
{
    /// <summary>
    /// uGUI fight HUD. HP/AP fills use 0–1 on the Image; HP/AP numbers tween to match.
    /// Mana (Nara): opcional imagem de preview por baixo do fill principal do frasco — durante aim de cast anima até à mana após gastar (sombra), como o preview de HP.
    /// Dice score area is shown only while <see cref="DiceAttackRuntimeService"/> is active.
    /// </summary>
    public sealed class GamePlayUiCanvasView : MonoBehaviour, IGamePlayHudView
    {
        [Header("Root")]
        [SerializeField] private RectTransform _hudRoot;

        [Header("Controls — gaveta (opcional)")]
        [Tooltip("Painel de controles: use UiSlidableAnchoredPanel no conteúdo que desliza; o botão fica fora do Slide Target.")]
        [SerializeField] private UiSlidableAnchoredPanel _controlsSlidablePanel;

        [Header("Skills — gaveta (opcional)")]
        [Tooltip("UiSlidableAnchoredPanel no SkillsRoot (ou equivalente). Arrastar aqui; TurnFlow abre no PlayerAct e fecha ao fim do turno do jogador. Outros sistemas podem usar SetSkillsSlidableExpanded.")]
        [SerializeField] private UiSlidableAnchoredPanel _skillsSlidablePanel;

        [Header("Dice score")]
        [Tooltip("Painel de pontuação do minigame de dados (ex.: DiceScore_Menu). Com Slidable, o root fica ativo e só o slide abre/fecha.")]
        [SerializeField] private GameObject _diceScoreAreaRoot;
        [Tooltip("Opcional. Se atribuído, mostrar/ocultar dados anima em vez de SetActive no root.")]
        [SerializeField] private UiSlidableAnchoredPanel _diceSlidablePanel;
        [Tooltip("Opcional. Se vazio, procura GamePlayDiceAttackPanelView dentro de Dice Score Area Root (incl. inativo).")]
        [SerializeField] private GamePlayDiceAttackPanelView _diceAttackPanel;

        [Header("Boss")]
        [SerializeField] private TMP_Text _bossNameText;
        [SerializeField] private Image _bossHpFillImage;
        [SerializeField] private Image _bossPreviewHpFillImage;
        [SerializeField] private TMP_Text _bossCurrentLifeText;

        [Header("Player")]
        [SerializeField] private Image _playerHpFillImage;
        [SerializeField] private Image _playerPreviewHpFillImage;
        [SerializeField] private TMP_Text _playerCurrentHealthText;
        [SerializeField] private Image _playerApFillImage;
        [Tooltip("Opcional. Camada por baixo do fill principal do frasco de mana (mesmo Rect/Fill Method). Cor mais escura/alpha — durante prepare cast anima até à mana após gastar; fora do cast fica alinhada ao valor real.")]
        [SerializeField] private Image _playerPreviewApFillImage;
        [SerializeField] private TMP_Text _playerActionPointsText;

        [Header("Player — next hit shield")]
        [Tooltip("Opcional. Visível enquanto a Nara tiver escudo de próximo golpe.")]
        [SerializeField] private GameObject _playerNextHitShieldIndicator;

        [Header("Mana flask — Nara (jogador)")]
        [Tooltip("Root do ManaFlask da Nara. O Button já deve ser o _toggleButton / _expandButton do _skillsSlidablePanel no Inspector — não duplicar listener em código.")]
        [SerializeField] private GameObject _naraManaFlaskRoot;

        [Header("Mana flask — Livro (custo universal 0→1)")]
        [Tooltip("Cópia do frasco do Livro: Button no root; o script só liga este ao Toggle de _skillsSlidablePanel (o da Nara já vem do UiSlidableAnchoredPanel).")]
        [SerializeField] private GameObject _bookManaFlaskRoot;
        [Tooltip("Image do líquido do Livro — fillAmount 0 a 1, animado conforme ICloneUseLimiter (1 = ação disponível, 0 = já usou). Mesmo tipo/Fill Method que o ManaFlask_Fill da Nara.")]
        [SerializeField] private Image _bookUniversalActionFillImage;
        [Tooltip("Opcional. Mostra 0 ou 1 alinhado ao limite de uma skill por turno (custos dos slots do Livro no HUD ficam em 0).")]
        [SerializeField] private TMP_Text _bookUniversalActionText;

        [Header("Skills Theme Roots")]
        [Tooltip("Background completo das skills da Nara/Erza. O codigo resolve o filho 'container' e os 4 botoes.")]
        [SerializeField] private GameObject _erzaSkillsBackground;
        [Tooltip("Background completo das skills do Book. O codigo resolve o filho 'container' e os 4 botoes.")]
        [SerializeField] private GameObject _bookSkillsBackground;

        [Header("Skill Buttons (Optional Inspector Lists)")]
        [Tooltip("Botoes de skill da Nara/Erza. Lista colapsavel para evitar poluicao.")]
        [SerializeField] private List<Button> _erzaSkillButtons = new List<Button>(4);
        [Tooltip("Botoes de skill do Book. Lista colapsavel para evitar poluicao.")]
        [SerializeField] private List<Button> _bookSkillButtons = new List<Button>(4);

        [Header("Skill Cost Labels (Optional Inspector Lists)")]
        [Tooltip("Custos das 4 skills da Nara/Erza.")]
        [SerializeField] private List<TMP_Text> _erzaSkillCostTexts = new List<TMP_Text>(4);
        [Tooltip("Custos das 4 skills do Book.")]
        [SerializeField] private List<TMP_Text> _bookSkillCostTexts = new List<TMP_Text>(4);

        [Header("Skill cost UI roots (optional)")]
        [Tooltip("Se preenchido por slot, esconde o objeto inteiro para passivas; senão esconde só o TMP do custo.")]
        [SerializeField] private List<GameObject> _erzaSkillCostDisplayRoots = new List<GameObject>(4);
        [SerializeField] private List<GameObject> _bookSkillCostDisplayRoots = new List<GameObject>(4);

        [Header("Skill keybind UI (optional)")]
        [Tooltip("Por slot: icon_Keybind / img_Keybind. Oculto em passivas. Auto-resolve por nome se vazio.")]
        [SerializeField] private List<GameObject> _erzaSkillKeybindDisplayRoots = new List<GameObject>(4);
        [SerializeField] private List<GameObject> _bookSkillKeybindDisplayRoots = new List<GameObject>(4);

        [Header("Cast feedback — tremor de mana")]
        [Tooltip("Opcional. Ícone de mana do jogador (Nara) a tremer. Fallback: root ManaFlask (Nara).")]
        [SerializeField] private RectTransform _naraPlayerManaShakeTarget;
        [Tooltip("Opcional. Ícone de mana/cast do Livro a tremer. Fallback: root ManaFlask (Livro).")]
        [SerializeField] private RectTransform _bookPlayerManaShakeTarget;

        [Header("Skill Icons (Optional — filhos dos botões)")]
        [Tooltip("Image de ícone por slot (ordem 0–3). Preenchido a partir de SkillDataSO.Icon ao refrescar o loadout.")]
        [SerializeField] private List<Image> _erzaSkillIconImages = new List<Image>(4);
        [SerializeField] private List<Image> _bookSkillIconImages = new List<Image>(4);

        readonly List<SkillSlotVisualView> _erzaSkillVisuals = new List<SkillSlotVisualView>(4);
        readonly List<SkillSlotVisualView> _bookSkillVisuals = new List<SkillSlotVisualView>(4);
        readonly List<CanvasGroup> _erzaSkillCanvasGroups = new List<CanvasGroup>(4);
        readonly List<CanvasGroup> _bookSkillCanvasGroups = new List<CanvasGroup>(4);
        readonly SkillDataSO[] _erzaHudSkills = new SkillDataSO[4];
        readonly SkillDataSO[] _bookHudSkills = new SkillDataSO[4];

        [Header("Skills — cast affordance")]
        [Tooltip("Alpha dos slots de skill quando não há mana (Erza) ou cast único (Livro). Igual ao loadout em drag inválido.")]
        [SerializeField] private float _unaffordableSkillAlpha = 0.35f;

        [Header("Buttons")]
        [SerializeField] private Button _nextTurnButton;
        [Tooltip("Abre o menu de pausa (mesmo que ESC / PauseGameplayInputCommand).")]
        [SerializeField] private Button _openPauseMenuButton;
        [Header("Dica — passar turno (só 1º turno da luta)")]
        [Tooltip("Igual DicePromptUI: após este delay (unscaled) o botão faz bob vertical até o jogador encerrar o turno.")]
        [SerializeField] private float _firstTurnPassTurnHintDelaySeconds = 5f;
        [SerializeField] private float _firstTurnPassTurnBobIntervalSeconds = 1f;
        [SerializeField] private float _firstTurnPassTurnBobOffsetPixels = 10f;
        [SerializeField] private float _firstTurnPassTurnBobHalfDuration = 0.15f;
        [SerializeField] private Button _skill1Button;
        [SerializeField] private Button _skill2Button;
        [SerializeField] private Button _skill3Button;
        [SerializeField] private Button _skill4Button;

        [SerializeField] private float _tweenDuration = 0.35f;
        [SerializeField] private Ease _tweenEase = Ease.OutQuad;

        [Header("Anúncio de turno (PlayerAct)")]
        [Tooltip("Opcional. Desativado no fim da sequência.")]
        [SerializeField] private GameObject _turnAnnouncementRoot;
        [Tooltip("Controla o alpha do painel (obrigatório para o fade).")]
        [SerializeField] private CanvasGroup _turnAnnouncementCanvasGroup;
        [SerializeField] private TMP_Text _turnAnnouncementText;
        [Tooltip("Rect que escala no abrir/fechar; se vazio, usa o RectTransform do texto.")]
        [SerializeField] private RectTransform _turnAnnouncementScaleTarget;
        [SerializeField] private float _turnAnnouncementOpenDuration = 0.35f;
        [SerializeField] private float _turnAnnouncementHoldDuration = 1.25f;
        [SerializeField] private float _turnAnnouncementCloseDuration = 0.3f;
        [SerializeField] private float _turnAnnouncementScaleFrom = 0.75f;
        [SerializeField] private Ease _turnAnnouncementOpenEase = Ease.OutBack;
        [SerializeField] private Ease _turnAnnouncementCloseEase = Ease.InQuad;

        private float _playerHpDisplayFloat;
        private float _playerPreviewHpDisplayFloat;
        private float _bossHpDisplayFloat;
        private float _playerApDisplayFloat;
        private float _playerApPreviewDisplayFloat;

        private GamePlayDiceAttackPanelView _dicePanelResolved;
        private Action _onSkill1;
        private Action _onSkill2;
        private Action _onSkill3;
        private Action _onSkill4;
        private bool _showBookSkillsTheme;
        private int _currentActionPoints;
        private bool _bookCloneAvailable = true;
        private Sequence _turnAnnouncementSequence;

        private Coroutine _firstTurnPassTurnHintRoutine;
        private RectTransform _nextTurnButtonRect;
        private Vector2 _nextTurnButtonAnchoredRestore;
        private bool _hasNextTurnButtonAnchoredRestore;
        private bool _firstTurnPassTurnHintRunning;

        private bool _castAimPreviewActive;
        private IPlayableUnit _aimPreviewCaster;
        private IPlayableUnit _aimPreviewPresentationUnit;
        private bool _playerHpSelfDamageAimActive;
        private bool _apManaAimPreviewActive;
        private int _apAimBaseline;
        private int _apAimCost;
        private int _apAimMax;

        private void Awake()
        {
            if (_nextTurnButton != null)
                _nextTurnButtonRect = _nextTurnButton.transform as RectTransform;
            if (_diceSlidablePanel != null && _diceScoreAreaRoot != null)
                _diceScoreAreaRoot.SetActive(true);
            if (_diceSlidablePanel != null)
                _diceSlidablePanel.SetExpanded(false, true);
            else
                SetDiceScoreAreaActive(false);

            ResetTurnAnnouncementHiddenImmediate();
            WireManaFlaskButtonsToSkillsSlidable();
        }

        private void OnDestroy()
        {
            UnwireManaFlaskButtonsFromSkillsSlidable();
            KillTurnAnnouncementSequence();
            EndFirstTurnPassTurnHint();
        }

        private void OnEnable()
        {
            DiceAttackRuntimeService.OnDiceAttackBegan += OnDiceAttackBegan;
            DiceAttackRuntimeService.OnDiceAttackEnded += OnDiceAttackEnded;
            DiceAttackRuntimeService.OnDeferredScoreboardDismiss += OnDeferredScoreboardDismiss;
            DiceUiRuntime.OnProgress += OnDiceUiProgress;
            DiceUiRuntime.OnScoreboardCelebration += OnDiceScoreboardCelebration;
        }

        private void OnDisable()
        {
            DiceAttackRuntimeService.OnDiceAttackBegan -= OnDiceAttackBegan;
            DiceAttackRuntimeService.OnDiceAttackEnded -= OnDiceAttackEnded;
            DiceAttackRuntimeService.OnDeferredScoreboardDismiss -= OnDeferredScoreboardDismiss;
            DiceUiRuntime.OnProgress -= OnDiceUiProgress;
            DiceUiRuntime.OnScoreboardCelebration -= OnDiceScoreboardCelebration;
        }

        private void OnDiceAttackBegan()
        {
            SetDiceScoreAreaActive(true);
            ResolveDicePanel()?.PrepareRoundStart();
        }

        private void OnDiceAttackEnded()
        {
            if (DiceAttackRuntimeService.IsScoreboardDismissDeferred) return;
            DismissDiceScoreboard();
        }

        private void OnDeferredScoreboardDismiss() => DismissDiceScoreboard();

        private void OnDiceScoreboardCelebration(int playerSum, int bossSum, bool playerWon, bool isTie)
        {
            SetDiceScoreAreaActive(true);
            ResolveDicePanel()?.PlayResolutionCelebration(playerWon, isTie);
        }

        public void DismissDiceScoreboard()
        {
            if (_diceSlidablePanel != null)
                _diceSlidablePanel.SetExpanded(false);
            else
                SetDiceScoreAreaActive(false);
        }

        private void OnDiceUiProgress(DiceUiProgressPayload payload)
        {
            if (!DiceAttackRuntimeService.IsActive && !MinigameRuntimeService.IsActive) return;
            SetDiceScoreAreaActive(true);
            ResolveDicePanel()?.ApplyProgress(payload);
        }

        private GamePlayDiceAttackPanelView ResolveDicePanel()
        {
            if (_diceAttackPanel != null) return _diceAttackPanel;
            if (_diceScoreAreaRoot == null) return null;
            if (_dicePanelResolved == null)
                _dicePanelResolved = _diceScoreAreaRoot.GetComponentInChildren<GamePlayDiceAttackPanelView>(true);
            return _dicePanelResolved;
        }

        /// <summary>Ativa ou desativa o painel de dados (útil se precisares forçar estado a partir de outro fluxo).</summary>
        public void SetDiceScoreAreaActive(bool active)
        {
            if (_diceSlidablePanel != null)
            {
                if (_diceScoreAreaRoot != null && !_diceScoreAreaRoot.activeSelf)
                    _diceScoreAreaRoot.SetActive(true);
                _diceSlidablePanel.SetExpanded(active);
            }
            else if (_diceScoreAreaRoot != null)
                _diceScoreAreaRoot.SetActive(active);
        }

        public void InitStartPoint()
        {
            if (_hudRoot == null) _hudRoot = GetComponent<RectTransform>();
            EnsureCombatSkillVisuals();
            EnsureCombatSkillAffordanceGroups();
        }

        void EnsureCombatSkillAffordanceGroups()
        {
            EnsureThemeAffordanceGroups(_erzaSkillButtons, _erzaSkillsBackground, _erzaSkillCanvasGroups);
            EnsureThemeAffordanceGroups(_bookSkillButtons, _bookSkillsBackground, _bookSkillCanvasGroups);
        }

        static void EnsureThemeAffordanceGroups(List<Button> configured, GameObject background, List<CanvasGroup> groups)
        {
            if (groups.Count > 0) return;
            for (int i = 0; i < 4; i++)
            {
                Button button = ResolveSkillButton(i, configured, background);
                groups.Add(button != null ? GetOrAddCanvasGroup(button.gameObject) : null);
            }
        }

        static CanvasGroup GetOrAddCanvasGroup(GameObject go)
        {
            if (go == null) return null;
            if (!go.TryGetComponent(out CanvasGroup group))
                group = go.AddComponent<CanvasGroup>();
            return group;
        }

        void EnsureCombatSkillVisuals()
        {
            if (_erzaSkillVisuals.Count == 0)
                PopulateSkillVisuals(_erzaSkillButtons, _erzaSkillVisuals);
            if (_bookSkillVisuals.Count == 0)
                PopulateSkillVisuals(_bookSkillButtons, _bookSkillVisuals);
        }

        static void PopulateSkillVisuals(List<Button> buttons, List<SkillSlotVisualView> target)
        {
            target.Clear();
            if (buttons == null) return;
            for (int i = 0; i < buttons.Count; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    target.Add(null);
                    continue;
                }

                SkillSlotVisualView visual = button.GetComponent<SkillSlotVisualView>()
                    ?? button.gameObject.AddComponent<SkillSlotVisualView>();
                target.Add(visual);
            }
        }

        /// <summary>Abre ou fecha o painel de controles quando o slidable estiver atribuído no inspector.</summary>
        public void SetControlsPanelExpanded(bool expanded, bool instant = false)
        {
            if (_controlsSlidablePanel != null)
                _controlsSlidablePanel.SetExpanded(expanded, instant);
        }

        public void RegisterCallbacks(Action onNextTurn, Action onSkill1, Action onSkill2, Action onSkill3, Action onSkill4)
        {
            Bind(_nextTurnButton, onNextTurn);
            _onSkill1 = onSkill1;
            _onSkill2 = onSkill2;
            _onSkill3 = onSkill3;
            _onSkill4 = onSkill4;

            BindOptionalButtons(_erzaSkillButtons, _onSkill1, _onSkill2, _onSkill3, _onSkill4);
            BindOptionalButtons(_bookSkillButtons, _onSkill1, _onSkill2, _onSkill3, _onSkill4);
            BindResolvedSkillButtons(_erzaSkillsBackground, _onSkill1, _onSkill2, _onSkill3, _onSkill4);
            BindResolvedSkillButtons(_bookSkillsBackground, _onSkill1, _onSkill2, _onSkill3, _onSkill4);

            // Fallback to old direct references if backgrounds are not assigned.
            Bind(_skill1Button, onSkill1);
            Bind(_skill2Button, onSkill2);
            Bind(_skill3Button, onSkill3);
            Bind(_skill4Button, onSkill4);
        }

        public void RegisterOpenPauseMenuCallback(Action onOpenPauseMenu) =>
            Bind(_openPauseMenuButton, onOpenPauseMenu);

        private static void Bind(Button b, Action a)
        {
            if (b == null || a == null) return;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => a());
        }

        public Transform GetGameplayHudRoot() => _hudRoot != null ? _hudRoot : transform;

        public void OnBossDisplayNameChange(string displayName) => SetStringText(_bossNameText, displayName);

        public void SnapBossHealth(int hp, int maxHp)
        {
            float max = Mathf.Max(1, maxHp);
            DOTween.Kill(_bossHpFillImage, true);
            DOTween.Kill(_bossCurrentLifeText, true);
            _bossHpDisplayFloat = hp;
            SnapFill01(_bossHpFillImage, hp / max);
            SetIntText(_bossCurrentLifeText, hp);
        }

        public void OnBossHealthUpdate(int hp, int maxHp)
        {
            float max = Mathf.Max(1, maxHp);
            float start = _bossHpDisplayFloat;
            DOTween.Kill(_bossHpFillImage, true);
            DOTween.Kill(_bossCurrentLifeText, true);
            float v = start;
            DOTween.To(() => v, x =>
            {
                v = x;
                _bossHpDisplayFloat = x;
                if (_bossHpFillImage != null) _bossHpFillImage.fillAmount = Mathf.Clamp01(x / max);
                if (_bossCurrentLifeText != null) _bossCurrentLifeText.SetText(Mathf.RoundToInt(x).ToString());
            }, hp, _tweenDuration).SetEase(_tweenEase).SetTarget(_bossHpFillImage != null ? _bossHpFillImage : (UnityEngine.Object)this);
        }

        public void OnPreviewBossHealthChange(int percent0To100) => TweenFillPercent(_bossPreviewHpFillImage, percent0To100);

        public void SnapPlayerHealth(int previewHp, int actualHp, int maxHp)
        {
            _playerHpSelfDamageAimActive = false;
            float max = Mathf.Max(1, maxHp);
            DOTween.Kill(_playerHpFillImage, true);
            DOTween.Kill(_playerPreviewHpFillImage, true);
            DOTween.Kill(_playerCurrentHealthText, true);
            _playerHpDisplayFloat = actualHp;
            _playerPreviewHpDisplayFloat = previewHp;
            SnapFill01(_playerHpFillImage, actualHp / max);
            SnapFill01(_playerPreviewHpFillImage, previewHp / max);
            SetIntText(_playerCurrentHealthText, actualHp);
        }

        public void OnPlayerHealthUpdate(int hp, int maxHp)
        {
            _playerHpSelfDamageAimActive = false;
            float max = Mathf.Max(1, maxHp);
            float start = _playerHpDisplayFloat;
            DOTween.Kill(_playerHpFillImage, true);
            DOTween.Kill(_playerCurrentHealthText, true);
            float v = start;
            DOTween.To(() => v, x =>
            {
                v = x;
                _playerHpDisplayFloat = x;
                if (_playerHpFillImage != null) _playerHpFillImage.fillAmount = Mathf.Clamp01(x / max);
                if (_playerCurrentHealthText != null) _playerCurrentHealthText.SetText(Mathf.RoundToInt(x).ToString());
            }, hp, _tweenDuration).SetEase(_tweenEase).SetTarget(_playerHpFillImage != null ? _playerHpFillImage : (UnityEngine.Object)this);
        }

        public void OnPreviewPlayerHealthUpdate(int previewHp, int maxHp)
        {
            float max = Mathf.Max(1, maxHp);
            float start = _playerPreviewHpDisplayFloat;
            DOTween.Kill(_playerPreviewHpFillImage, true);
            float v = start;
            DOTween.To(() => v, x =>
            {
                v = x;
                _playerPreviewHpDisplayFloat = x;
                if (_playerPreviewHpFillImage != null) _playerPreviewHpFillImage.fillAmount = Mathf.Clamp01(x / max);
            }, previewHp, _tweenDuration).SetEase(_tweenEase).SetTarget(_playerPreviewHpFillImage != null ? _playerPreviewHpFillImage : (UnityEngine.Object)this);
        }

        public void BeginPlayerSelfDamageCastAimVisual(int actualHp, int baselineHp, int projectedHpAfterSelfHit, int maxHp)
        {
            float max = Mathf.Max(1f, maxHp);
            _playerHpSelfDamageAimActive = true;

            DOTween.Kill(_playerHpFillImage, true);
            DOTween.Kill(_playerPreviewHpFillImage, true);

            _playerPreviewHpDisplayFloat = baselineHp;
            if (_playerPreviewHpFillImage != null)
                _playerPreviewHpFillImage.fillAmount = Mathf.Clamp01(baselineHp / max);

            _playerHpDisplayFloat = actualHp;
            float startFill = Mathf.Clamp01(actualHp / max);
            float endFill = Mathf.Clamp01(projectedHpAfterSelfHit / max);
            if (_playerHpFillImage != null)
                _playerHpFillImage.fillAmount = startFill;

            float v = startFill;
            var target = _playerHpFillImage != null ? (UnityEngine.Object)_playerHpFillImage : this;
            DOTween.To(() => v, x =>
            {
                v = x;
                if (_playerHpFillImage != null) _playerHpFillImage.fillAmount = x;
            }, endFill, _tweenDuration).SetEase(_tweenEase).SetTarget(target);
        }

        public void EndPlayerSelfDamageCastAimVisual(bool cancel, int actualHp, int maxHp)
        {
            if (!_playerHpSelfDamageAimActive) return;
            _playerHpSelfDamageAimActive = false;

            float max = Mathf.Max(1f, maxHp);
            var target = _playerHpFillImage != null ? (UnityEngine.Object)_playerHpFillImage : this;
            DOTween.Kill(_playerHpFillImage, true);

            _playerHpDisplayFloat = actualHp;

            if (cancel)
            {
                float startFill = _playerHpFillImage != null ? _playerHpFillImage.fillAmount : Mathf.Clamp01(actualHp / max);
                float endFill = Mathf.Clamp01(actualHp / max);
                float v = startFill;
                DOTween.To(() => v, x =>
                {
                    v = x;
                    if (_playerHpFillImage != null) _playerHpFillImage.fillAmount = x;
                }, endFill, _tweenDuration).SetEase(_tweenEase).SetTarget(target);
            }
            else
            {
                if (_playerHpFillImage != null)
                    _playerHpFillImage.fillAmount = Mathf.Clamp01(actualHp / max);
            }
        }

        public void SnapPlayerActionPoints(int current, int max)
        {
            float maxF = Mathf.Max(1, max);
            DOTween.Kill(_playerApFillImage, true);
            DOTween.Kill(_playerPreviewApFillImage, true);
            DOTween.Kill(_playerActionPointsText, true);
            _playerApDisplayFloat = current;
            _playerApPreviewDisplayFloat = current;
            SnapFill01(_playerApFillImage, current / maxF);
            if (_playerPreviewApFillImage != null)
                _playerPreviewApFillImage.fillAmount = Mathf.Clamp01(current / maxF);
            SetIntText(_playerActionPointsText, current);
            _currentActionPoints = current;
            RefreshSkillAffordanceVisuals();
        }

        public void OnPlayerActionPointsChange(int current, int max)
        {
            _currentActionPoints = current;
            RefreshSkillAffordanceVisuals();
            float maxF = Mathf.Max(1, max);
            float start = _playerApDisplayFloat;
            DOTween.Kill(_playerApFillImage, true);
            DOTween.Kill(_playerPreviewApFillImage, true);
            DOTween.Kill(_playerActionPointsText, true);
            float v = start;
            bool syncManaPreview = _playerPreviewApFillImage != null && !_apManaAimPreviewActive;
            var target = _playerApFillImage != null ? (UnityEngine.Object)_playerApFillImage : this;
            DOTween.To(() => v, x =>
            {
                v = x;
                _playerApDisplayFloat = x;
                if (_playerApFillImage != null) _playerApFillImage.fillAmount = Mathf.Clamp01(x / maxF);
                if (syncManaPreview)
                {
                    _playerApPreviewDisplayFloat = x;
                    if (_playerPreviewApFillImage != null)
                        _playerPreviewApFillImage.fillAmount = Mathf.Clamp01(x / maxF);
                }
                if (_playerActionPointsText != null) _playerActionPointsText.SetText(Mathf.RoundToInt(x).ToString());
            }, current, _tweenDuration).SetEase(_tweenEase).SetTarget(target);
        }

        public void OnPlayerNextHitShieldChanged(bool active)
        {
            if (_playerNextHitShieldIndicator != null)
                _playerNextHitShieldIndicator.SetActive(active);
        }

        public void BeginSkillCastAimPreview(IPlayableUnit caster, SkillDataSO skill, int apCost, bool showPlayerManaPreview, int apCurrent, int apMax)
        {
            if (_castAimPreviewActive && _aimPreviewCaster != null)
                EndSkillCastAimPreviewCancel(_aimPreviewCaster);

            _castAimPreviewActive = true;
            _aimPreviewCaster = caster;
            _aimPreviewPresentationUnit = skill != null
                ? SkillCastPresentationTarget.ResolvePlayable(caster, skill) ?? caster
                : caster;
            _apManaAimPreviewActive = false;

            if (_aimPreviewPresentationUnit is INaraController naraForPreview
                && skill != null
                && _aimPreviewPresentationUnit is IEffectable effNara)
            {
                if (SkillCastSelfHealPreview.TryGetSelfHealPreviewAmount(skill, out int heal) && heal > 0)
                    effNara.PreviewHeal(heal);
                naraForPreview.BeginSelfDamageCastAimPreviewFromSkill(skill);
            }

            if (showPlayerManaPreview && apCost > 0 && apMax > 0 && caster is INaraController)
            {
                _apManaAimPreviewActive = true;
                _apAimBaseline = apCurrent;
                _apAimCost = apCost;
                _apAimMax = apMax;
                int targetAp = Mathf.Max(0, apCurrent - apCost);
                if (_playerPreviewApFillImage != null)
                {
                    EnsurePlayerManaMainFillAtCurrentWhileAiming(apCurrent, apMax);
                    TweenPlayerPreviewActionPointsFill(targetAp, apMax);
                }
                else
                    TweenPlayerActionPointsDisplay(targetAp, apMax);
            }
        }

        public void EndSkillCastAimPreviewCancel(IPlayableUnit caster)
        {
            if (!_castAimPreviewActive) return;

            if (_aimPreviewPresentationUnit is INaraController nara)
                nara.EndSelfDamageCastAimPreview(true);

            if (_aimPreviewPresentationUnit is IEffectable eff)
                eff.ResetPreview();

            if (_apManaAimPreviewActive)
            {
                _apManaAimPreviewActive = false;
                if (_playerPreviewApFillImage != null)
                    TweenPlayerPreviewActionPointsFill(_apAimBaseline, _apAimMax);
                else
                    TweenPlayerActionPointsDisplay(_apAimBaseline, _apAimMax);
            }

            _castAimPreviewActive = false;
            _aimPreviewCaster = null;
            _aimPreviewPresentationUnit = null;
        }

        public void EndSkillCastAimPreviewCommit(IPlayableUnit caster)
        {
            if (!_castAimPreviewActive) return;

            if (_aimPreviewPresentationUnit is INaraController nara)
                nara.EndSelfDamageCastAimPreview(false);

            if (_aimPreviewPresentationUnit is IEffectable eff)
                eff.ResetPreview();

            if (_apManaAimPreviewActive)
            {
                DOTween.Kill(_playerApFillImage, true);
                DOTween.Kill(_playerPreviewApFillImage, true);
                DOTween.Kill(_playerActionPointsText, true);
                int postSpendVisual = Mathf.Max(0, _apAimBaseline - _apAimCost);
                float maxF = Mathf.Max(1, _apAimMax);
                _playerApDisplayFloat = postSpendVisual;
                _playerApPreviewDisplayFloat = postSpendVisual;
                if (_playerApFillImage != null) _playerApFillImage.fillAmount = Mathf.Clamp01(postSpendVisual / maxF);
                if (_playerPreviewApFillImage != null)
                    _playerPreviewApFillImage.fillAmount = Mathf.Clamp01(postSpendVisual / maxF);
                if (_playerActionPointsText != null) _playerActionPointsText.SetText(postSpendVisual.ToString());
                _apManaAimPreviewActive = false;
            }

            _castAimPreviewActive = false;
            _aimPreviewCaster = null;
            _aimPreviewPresentationUnit = null;
        }

        private void EnsurePlayerManaMainFillAtCurrentWhileAiming(int apCurrent, int maxAp)
        {
            float maxF = Mathf.Max(1, maxAp);
            DOTween.Kill(_playerApFillImage, true);
            DOTween.Kill(_playerActionPointsText, true);
            _playerApDisplayFloat = apCurrent;
            if (_playerApFillImage != null) _playerApFillImage.fillAmount = Mathf.Clamp01(apCurrent / maxF);
            if (_playerActionPointsText != null) _playerActionPointsText.SetText(apCurrent.ToString());

            DOTween.Kill(_playerPreviewApFillImage, true);
            _playerApPreviewDisplayFloat = apCurrent;
            if (_playerPreviewApFillImage != null)
                _playerPreviewApFillImage.fillAmount = Mathf.Clamp01(apCurrent / maxF);
        }

        private void TweenPlayerPreviewActionPointsFill(int targetPreviewAp, int maxAp)
        {
            if (_playerPreviewApFillImage == null) return;
            float maxF = Mathf.Max(1, maxAp);
            float start = _playerApPreviewDisplayFloat;
            DOTween.Kill(_playerPreviewApFillImage, true);
            float v = start;
            DOTween.To(() => v, x =>
            {
                v = x;
                _playerApPreviewDisplayFloat = x;
                _playerPreviewApFillImage.fillAmount = Mathf.Clamp01(x / maxF);
            }, targetPreviewAp, _tweenDuration).SetEase(_tweenEase).SetTarget(_playerPreviewApFillImage);
        }

        private void TweenPlayerActionPointsDisplay(int targetCurrent, int maxAp)
        {
            float maxF = Mathf.Max(1, maxAp);
            float start = _playerApDisplayFloat;
            DOTween.Kill(_playerApFillImage, true);
            DOTween.Kill(_playerActionPointsText, true);
            float v = start;
            var tweenTarget = _playerApFillImage != null ? (UnityEngine.Object)_playerApFillImage : this;
            DOTween.To(() => v, x =>
            {
                v = x;
                _playerApDisplayFloat = x;
                if (_playerApFillImage != null) _playerApFillImage.fillAmount = Mathf.Clamp01(x / maxF);
                if (_playerActionPointsText != null) _playerActionPointsText.SetText(Mathf.RoundToInt(x).ToString());
            }, targetCurrent, _tweenDuration).SetEase(_tweenEase).SetTarget(tweenTarget);
        }

        public void SetAbilityManaCosts(int c1, int c2, int c3, int c4, bool showCostSlot1 = true, bool showCostSlot2 = true, bool showCostSlot3 = true, bool showCostSlot4 = true)
        {
            ApplySkillCostCell(0, c1, showCostSlot1);
            ApplySkillCostCell(1, c2, showCostSlot2);
            ApplySkillCostCell(2, c3, showCostSlot3);
            ApplySkillCostCell(3, c4, showCostSlot4);
        }

        public void OnSkill1CostChange(int cost) => ApplySkillCostCell(0, cost, true);

        public void OnSkill2CostChange(int cost) => ApplySkillCostCell(1, cost, true);

        public void OnSkill3CostChange(int cost) => ApplySkillCostCell(2, cost, true);

        public void OnSkill4CostChange(int cost) => ApplySkillCostCell(3, cost, true);

        public void OnSkill1NameChange(string name) { }

        public void OnSkill2NameChange(string name) { }

        public void ShowBookSkillsTheme(bool showBookSkillsTheme)
        {
            _showBookSkillsTheme = showBookSkillsTheme;
            if (_naraManaFlaskRoot != null)
                _naraManaFlaskRoot.SetActive(!showBookSkillsTheme);
            if (_bookManaFlaskRoot != null)
                _bookManaFlaskRoot.SetActive(showBookSkillsTheme);
            if (_erzaSkillsBackground != null) _erzaSkillsBackground.SetActive(!showBookSkillsTheme);
            if (_bookSkillsBackground != null) _bookSkillsBackground.SetActive(showBookSkillsTheme);
            RefreshSkillAffordanceVisuals();
        }

        /// <inheritdoc />
        public void SetBookCloneActionAvailable(bool available)
        {
            _bookCloneAvailable = available;
            SetIntText(_bookUniversalActionText, available ? 1 : 0);

            if (_bookUniversalActionFillImage == null) return;

            DOTween.Kill(_bookUniversalActionFillImage, true);
            float target = available ? 1f : 0f;
            float v = _bookUniversalActionFillImage.fillAmount;
            DOTween.To(() => v, x =>
            {
                v = x;
                if (_bookUniversalActionFillImage != null)
                    _bookUniversalActionFillImage.fillAmount = Mathf.Clamp01(x);
            }, target, _tweenDuration).SetEase(_tweenEase).SetTarget(_bookUniversalActionFillImage);
            RefreshSkillAffordanceVisuals();
        }

        public void SetSkillHudIcons(Sprite erza0, Sprite erza1, Sprite erza2, Sprite erza3, Sprite book0, Sprite book1, Sprite book2, Sprite book3)
        {
            ApplySkillIconsToList(_erzaSkillIconImages, erza0, erza1, erza2, erza3);
            ApplySkillIconsToList(_bookSkillIconImages, book0, book1, book2, book3);
        }

        public void SetSkillHudVisuals(
            SkillDataSO erza0, SkillDataSO erza1, SkillDataSO erza2, SkillDataSO erza3,
            SkillDataSO book0, SkillDataSO book1, SkillDataSO book2, SkillDataSO book3,
            ISkillVisualCatalog visualCatalog)
        {
            EnsureCombatSkillVisuals();
            CacheHudSkills(_erzaHudSkills, erza0, erza1, erza2, erza3);
            CacheHudSkills(_bookHudSkills, book0, book1, book2, book3);
            ApplySkillVisualToList(_erzaSkillVisuals, _erzaSkillIconImages, visualCatalog, erza0, erza1, erza2, erza3);
            ApplySkillVisualToList(_bookSkillVisuals, _bookSkillIconImages, visualCatalog, book0, book1, book2, book3);
            RefreshSkillKeybindVisibility();
            RefreshSkillAffordanceVisuals();
        }

        public void RefreshSkillCastAffordability(int currentActionPoints, bool bookCloneAvailable)
        {
            _currentActionPoints = currentActionPoints;
            _bookCloneAvailable = bookCloneAvailable;
            RefreshSkillAffordanceVisuals();
        }

        public void PlayInsufficientCastFeedback(int slotIndex, CombatSkillCastBlockReason reason)
        {
            if (slotIndex >= 0 && slotIndex < 4)
            {
                RectTransform skillMana = ResolveSkillManaShakeTarget(slotIndex, _showBookSkillsTheme);
                if (skillMana != null)
                    UiRectShake.PlayInvalidSlotShake(skillMana);
            }

            RectTransform playerMana = ResolvePlayerManaShakeTarget();
            if (playerMana != null)
                UiRectShake.PlayInvalidSlotShake(playerMana);
        }

        void RefreshSkillKeybindVisibility()
        {
            for (int i = 0; i < 4; i++)
            {
                ApplyKeybindCell(i, _erzaHudSkills[i], _erzaSkillKeybindDisplayRoots, bookTheme: false);
                ApplyKeybindCell(i, _bookHudSkills[i], _bookSkillKeybindDisplayRoots, bookTheme: true);
            }
        }

        void ApplyKeybindCell(int slotIndex, SkillDataSO skill, List<GameObject> configuredRoots, bool bookTheme)
        {
            GameObject root = ResolveKeybindDisplayRoot(slotIndex, configuredRoots, bookTheme);
            if (root == null) return;
            root.SetActive(skill != null && skill.IsCastable);
        }

        RectTransform ResolvePlayerManaShakeTarget()
        {
            if (_showBookSkillsTheme)
            {
                if (_bookPlayerManaShakeTarget != null) return _bookPlayerManaShakeTarget;
                return _bookManaFlaskRoot != null ? _bookManaFlaskRoot.transform as RectTransform : null;
            }

            if (_naraPlayerManaShakeTarget != null) return _naraPlayerManaShakeTarget;
            return _naraManaFlaskRoot != null ? _naraManaFlaskRoot.transform as RectTransform : null;
        }

        RectTransform ResolveSkillManaShakeTarget(int slotIndex, bool bookTheme)
        {
            GameObject costRoot = ResolveSkillCostDisplayRoot(slotIndex, bookTheme);
            return costRoot != null ? costRoot.transform as RectTransform : null;
        }

        GameObject ResolveKeybindDisplayRoot(int slotIndex, List<GameObject> configuredRoots, bool bookTheme)
        {
            GameObject root = At(configuredRoots, slotIndex);
            if (root != null) return root;

            Button button = bookTheme
                ? ResolveSkillButton(slotIndex, _bookSkillButtons, _bookSkillsBackground)
                : ResolveSkillButton(slotIndex, _erzaSkillButtons, _erzaSkillsBackground);
            if (button == null) return null;

            Transform keybind = FindNamedChild(button.transform, "img_Keybind")
                ?? FindNamedChild(button.transform, "icon_Keybind");
            return keybind != null ? keybind.gameObject : null;
        }

        static Transform FindNamedChild(Transform root, string childName)
        {
            if (root == null) return null;

            Transform direct = root.Find(childName);
            if (direct != null) return direct;

            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                    return child;
            }

            return null;
        }

        static void CacheHudSkills(SkillDataSO[] target, SkillDataSO s0, SkillDataSO s1, SkillDataSO s2, SkillDataSO s3)
        {
            target[0] = s0;
            target[1] = s1;
            target[2] = s2;
            target[3] = s3;
        }

        void RefreshSkillAffordanceVisuals()
        {
            EnsureCombatSkillAffordanceGroups();
            ApplySkillAffordanceToTheme(_erzaSkillCanvasGroups, _erzaHudSkills, bookTheme: false);
            ApplySkillAffordanceToTheme(_bookSkillCanvasGroups, _bookHudSkills, bookTheme: true);
        }

        void ApplySkillAffordanceToTheme(List<CanvasGroup> groups, SkillDataSO[] skills, bool bookTheme)
        {
            if (groups == null || skills == null) return;
            bool applyDimming = bookTheme ? _showBookSkillsTheme : !_showBookSkillsTheme;
            for (int i = 0; i < 4; i++)
            {
                CanvasGroup group = At(groups, i);
                if (group == null) continue;
                if (!applyDimming)
                {
                    group.alpha = 1f;
                    continue;
                }

                SkillDataSO skill = skills[i];
                group.alpha = IsSkillCastAffordable(skill, bookTheme) ? 1f : _unaffordableSkillAlpha;
            }
        }

        bool IsSkillCastAffordable(SkillDataSO skill, bool bookTheme)
        {
            if (skill == null || !skill.IsCastable) return true;

            if (bookTheme)
                return _bookCloneAvailable;

            return _currentActionPoints >= Mathf.Max(0, skill.Cost);
        }

        Button GetActiveThemeSkillButton(int slotIndex)
        {
            if (_showBookSkillsTheme)
                return ResolveSkillButton(slotIndex, _bookSkillButtons, _bookSkillsBackground)
                    ?? LegacySkillButtonAt(slotIndex);
            return ResolveSkillButton(slotIndex, _erzaSkillButtons, _erzaSkillsBackground)
                ?? LegacySkillButtonAt(slotIndex);
        }

        Button LegacySkillButtonAt(int slotIndex) => slotIndex switch
        {
            0 => _skill1Button,
            1 => _skill2Button,
            2 => _skill3Button,
            3 => _skill4Button,
            _ => null
        };

        static Button ResolveSkillButton(int slotIndex, List<Button> configured, GameObject background)
        {
            Button button = At(configured, slotIndex);
            if (button != null) return button;
            if (background == null) return null;
            Transform container = ResolveContainer(background.transform);
            if (container == null || slotIndex < 0 || slotIndex >= container.childCount) return null;
            return container.GetChild(slotIndex).GetComponent<Button>();
        }

        static void ApplySkillVisualToList(
            List<SkillSlotVisualView> visuals,
            List<Image> iconFallbacks,
            ISkillVisualCatalog visualCatalog,
            SkillDataSO s0, SkillDataSO s1, SkillDataSO s2, SkillDataSO s3)
        {
            ApplySkillVisualAt(visuals, iconFallbacks, visualCatalog, 0, s0);
            ApplySkillVisualAt(visuals, iconFallbacks, visualCatalog, 1, s1);
            ApplySkillVisualAt(visuals, iconFallbacks, visualCatalog, 2, s2);
            ApplySkillVisualAt(visuals, iconFallbacks, visualCatalog, 3, s3);
        }

        static void ApplySkillVisualAt(
            List<SkillSlotVisualView> visuals,
            List<Image> iconFallbacks,
            ISkillVisualCatalog visualCatalog,
            int index,
            SkillDataSO skill)
        {
            SkillSlotVisualView visual = At(visuals, index);
            if (visual != null)
                visual.Apply(skill, visualCatalog, SkillFrameVisualMode.FullLayers);
            SetIconAt(iconFallbacks, index, skill != null ? skill.Icon : null);
        }

        private static void ApplySkillIconsToList(List<Image> icons, Sprite s0, Sprite s1, Sprite s2, Sprite s3)
        {
            if (icons == null || icons.Count == 0) return;
            SetIconAt(icons, 0, s0);
            SetIconAt(icons, 1, s1);
            SetIconAt(icons, 2, s2);
            SetIconAt(icons, 3, s3);
        }

        private static void SetIconAt(List<Image> icons, int index, Sprite sprite)
        {
            var img = At(icons, index);
            if (img == null) return;
            img.sprite = sprite;
        }

        /// <inheritdoc />
        public void SetSkillsSlidableExpanded(bool expanded, bool instant = false) =>
            _skillsSlidablePanel?.SetExpanded(expanded, instant);

        private void WireManaFlaskButtonsToSkillsSlidable()
        {
            if (_skillsSlidablePanel == null) return;
            // Só o frasco do Livro: o da Nara já é o mesmo Button referenciado em UiSlidableAnchoredPanel (_toggleButton);
            // adicionar listener aqui causava Toggle duplo (abrir e fechar no mesmo clique).
            TryAddManaFlaskToggleListener(_bookManaFlaskRoot);
        }

        private void UnwireManaFlaskButtonsFromSkillsSlidable()
        {
            TryRemoveManaFlaskToggleListener(_bookManaFlaskRoot);
        }

        private void TryAddManaFlaskToggleListener(GameObject flaskRoot)
        {
            if (flaskRoot == null) return;
            var button = flaskRoot.GetComponent<Button>();
            if (button == null) return;
            button.onClick.AddListener(OnManaFlaskClickedToggleSkillsSlidable);
        }

        private void TryRemoveManaFlaskToggleListener(GameObject flaskRoot)
        {
            if (flaskRoot == null) return;
            var button = flaskRoot.GetComponent<Button>();
            if (button == null) return;
            button.onClick.RemoveListener(OnManaFlaskClickedToggleSkillsSlidable);
        }

        private void OnManaFlaskClickedToggleSkillsSlidable() => _skillsSlidablePanel?.Toggle();

        /// <inheritdoc />
        public void BeginFirstTurnPassTurnHint(int fightTurnNumber)
        {
            if (fightTurnNumber != 1 || _nextTurnButtonRect == null) return;
            StopFirstTurnPassTurnHintInternal();
            _firstTurnPassTurnHintRoutine = StartCoroutine(FirstTurnPassTurnHintRoutine());
        }

        /// <inheritdoc />
        public void EndFirstTurnPassTurnHint() => StopFirstTurnPassTurnHintInternal();

        private void StopFirstTurnPassTurnHintInternal()
        {
            _firstTurnPassTurnHintRunning = false;
            if (_firstTurnPassTurnHintRoutine != null)
            {
                StopCoroutine(_firstTurnPassTurnHintRoutine);
                _firstTurnPassTurnHintRoutine = null;
            }
            if (_nextTurnButtonRect != null)
            {
                DOTween.Kill(_nextTurnButtonRect, false);
                if (_hasNextTurnButtonAnchoredRestore)
                    _nextTurnButtonRect.anchoredPosition = _nextTurnButtonAnchoredRestore;
            }
            _hasNextTurnButtonAnchoredRestore = false;
        }

        private IEnumerator FirstTurnPassTurnHintRoutine()
        {
            _firstTurnPassTurnHintRunning = true;
            float threshold = Mathf.Max(0.05f, _firstTurnPassTurnHintDelaySeconds);
            float idle = 0f;
            while (idle < threshold && gameObject.activeInHierarchy && _firstTurnPassTurnHintRunning)
            {
                idle += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!gameObject.activeInHierarchy || !_firstTurnPassTurnHintRunning || _nextTurnButtonRect == null)
            {
                _firstTurnPassTurnHintRoutine = null;
                _firstTurnPassTurnHintRunning = false;
                yield break;
            }

            _nextTurnButtonAnchoredRestore = _nextTurnButtonRect.anchoredPosition;
            _hasNextTurnButtonAnchoredRestore = true;
            float y0 = _nextTurnButtonAnchoredRestore.y;
            float half = Mathf.Max(0.02f, _firstTurnPassTurnBobHalfDuration);
            float bob = _firstTurnPassTurnBobOffsetPixels;
            float interval = Mathf.Max(half * 2f, _firstTurnPassTurnBobIntervalSeconds);

            while (gameObject.activeInHierarchy && _firstTurnPassTurnHintRunning && _nextTurnButtonRect != null)
            {
                var up = _nextTurnButtonRect.DOAnchorPosY(y0 + bob, half)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true);
                yield return up.WaitForCompletion(true);

                if (!_firstTurnPassTurnHintRunning) break;

                var down = _nextTurnButtonRect.DOAnchorPosY(y0, half)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true);
                yield return down.WaitForCompletion(true);

                float waitRemain = interval - 2f * half;
                if (waitRemain > 0f)
                {
                    float w = 0f;
                    while (w < waitRemain && gameObject.activeInHierarchy && _firstTurnPassTurnHintRunning)
                    {
                        w += Time.unscaledDeltaTime;
                        yield return null;
                    }
                }
            }

            _firstTurnPassTurnHintRoutine = null;
            _firstTurnPassTurnHintRunning = false;
        }

        /// <inheritdoc />
        public void PlayPlayerTurnAnnouncement(int turnNumber)
        {
            if (_turnAnnouncementCanvasGroup == null || _turnAnnouncementText == null) return;

            KillTurnAnnouncementSequence();

            _turnAnnouncementText.SetText($"Turno {turnNumber}");
            if (_turnAnnouncementRoot != null)
                _turnAnnouncementRoot.SetActive(true);

            RectTransform scaleRt = _turnAnnouncementScaleTarget != null
                ? _turnAnnouncementScaleTarget
                : _turnAnnouncementText.rectTransform;

            float s0 = Mathf.Max(0.01f, _turnAnnouncementScaleFrom);
            _turnAnnouncementCanvasGroup.alpha = 0f;
            scaleRt.localScale = Vector3.one * s0;

            Sequence seq = DOTween.Sequence();
            _turnAnnouncementSequence = seq;

            seq.Append(_turnAnnouncementCanvasGroup.DOFade(1f, _turnAnnouncementOpenDuration));
            seq.Join(scaleRt.DOScale(1f, _turnAnnouncementOpenDuration).SetEase(_turnAnnouncementOpenEase));
            seq.AppendInterval(_turnAnnouncementHoldDuration);
            seq.Append(_turnAnnouncementCanvasGroup.DOFade(0f, _turnAnnouncementCloseDuration));
            seq.Join(scaleRt.DOScale(s0, _turnAnnouncementCloseDuration).SetEase(_turnAnnouncementCloseEase));
            seq.OnComplete(() =>
            {
                _turnAnnouncementSequence = null;
                if (_turnAnnouncementRoot != null)
                    _turnAnnouncementRoot.SetActive(false);
            });
        }

        private void ResetTurnAnnouncementHiddenImmediate()
        {
            KillTurnAnnouncementSequence();
            if (_turnAnnouncementCanvasGroup != null)
                _turnAnnouncementCanvasGroup.alpha = 0f;
            RectTransform scaleRt = _turnAnnouncementScaleTarget;
            if (scaleRt == null && _turnAnnouncementText != null)
                scaleRt = _turnAnnouncementText.rectTransform;
            if (scaleRt != null)
                scaleRt.localScale = Vector3.one * Mathf.Max(0.01f, _turnAnnouncementScaleFrom);
            if (_turnAnnouncementRoot != null)
                _turnAnnouncementRoot.SetActive(false);
        }

        private void KillTurnAnnouncementSequence()
        {
            if (_turnAnnouncementSequence != null && _turnAnnouncementSequence.IsActive())
                _turnAnnouncementSequence.Kill();
            _turnAnnouncementSequence = null;
            if (_turnAnnouncementCanvasGroup != null)
                DOTween.Kill(_turnAnnouncementCanvasGroup);
            RectTransform scaleRt = _turnAnnouncementScaleTarget;
            if (scaleRt == null && _turnAnnouncementText != null)
                scaleRt = _turnAnnouncementText.rectTransform;
            if (scaleRt != null)
                DOTween.Kill(scaleRt);
        }

        private static void BindResolvedSkillButtons(GameObject skillsBackground, Action onSkill1, Action onSkill2, Action onSkill3, Action onSkill4)
        {
            if (skillsBackground == null) return;
            Transform container = ResolveContainer(skillsBackground.transform);
            if (container == null) return;

            BindSlot(container, 0, onSkill1);
            BindSlot(container, 1, onSkill2);
            BindSlot(container, 2, onSkill3);
            BindSlot(container, 3, onSkill4);
        }

        private static void BindOptionalButtons(List<Button> buttons, Action onSkill1, Action onSkill2, Action onSkill3, Action onSkill4)
        {
            if (buttons == null || buttons.Count == 0) return;
            Bind(At(buttons, 0), onSkill1);
            Bind(At(buttons, 1), onSkill2);
            Bind(At(buttons, 2), onSkill3);
            Bind(At(buttons, 3), onSkill4);
        }

        private static void BindSlot(Transform container, int slotIndex, Action callback)
        {
            if (callback == null) return;
            if (slotIndex < 0 || slotIndex >= container.childCount) return;
            var button = container.GetChild(slotIndex).GetComponent<Button>();
            Bind(button, callback);
        }

        private GameObject GetSkillCostDisplayRoot(int slotIndex) =>
            ResolveSkillCostDisplayRoot(slotIndex, _showBookSkillsTheme);

        GameObject ResolveSkillCostDisplayRoot(int slotIndex, bool bookTheme)
        {
            List<GameObject> configuredList = bookTheme ? _bookSkillCostDisplayRoots : _erzaSkillCostDisplayRoots;
            GameObject root = At(configuredList, slotIndex);
            if (root != null) return root;

            Button button = bookTheme
                ? ResolveSkillButton(slotIndex, _bookSkillButtons, _bookSkillsBackground)
                : ResolveSkillButton(slotIndex, _erzaSkillButtons, _erzaSkillsBackground);
            Transform mana = FindNamedChild(button != null ? button.transform : null, "icon_Mana");
            return mana != null ? mana.gameObject : null;
        }

        private void ApplySkillCostCell(int slotIndex, int cost, bool showCostDisplay)
        {
            GameObject root = GetSkillCostDisplayRoot(slotIndex);
            if (root != null)
            {
                root.SetActive(showCostDisplay);
                if (showCostDisplay)
                {
                    var t = root.GetComponentInChildren<TMP_Text>(true);
                    SetIntText(t, cost);
                }

                return;
            }

            TMP_Text text = GetActiveCostText(slotIndex);
            if (text != null)
            {
                text.gameObject.SetActive(showCostDisplay);
                if (showCostDisplay)
                    SetIntText(text, cost);
            }
        }

        private TMP_Text GetActiveCostText(int slotIndex)
        {
            var configuredList = _showBookSkillsTheme ? _bookSkillCostTexts : _erzaSkillCostTexts;
            var configured = At(configuredList, slotIndex);
            if (configured != null) return configured;

            GameObject activeBg = _showBookSkillsTheme ? _bookSkillsBackground : _erzaSkillsBackground;
            if (activeBg == null) return null;

            Transform container = ResolveContainer(activeBg.transform);
            if (container == null) return null;
            if (slotIndex < 0 || slotIndex >= container.childCount) return null;

            return container.GetChild(slotIndex).GetComponentInChildren<TMP_Text>(true);
        }

        private static Transform ResolveContainer(Transform backgroundTransform)
        {
            if (backgroundTransform == null) return null;
            Transform namedContainer = backgroundTransform.Find("container");
            return namedContainer != null ? namedContainer : backgroundTransform;
        }

        private static T At<T>(List<T> list, int index) where T : class
        {
            if (list == null) return null;
            if (index < 0 || index >= list.Count) return null;
            return list[index];
        }

        private void TweenFillPercent(Image img, int percent0To100)
        {
            if (img == null) return;
            float target = Mathf.Clamp01(percent0To100 / 100f);
            DOTween.Kill(img, true);
            DOTween.To(() => img.fillAmount, a => img.fillAmount = a, target, _tweenDuration).SetEase(_tweenEase).SetTarget(img);
        }

        private static void SnapFill01(Image img, float amount01)
        {
            if (img == null) return;
            img.fillAmount = Mathf.Clamp01(amount01);
        }

        private static void SetIntText(TMP_Text t, int v)
        {
            if (t != null) t.SetText(v.ToString());
        }

        private static void SetStringText(TMP_Text t, string s)
        {
            if (t != null) t.SetText(s ?? string.Empty);
        }
    }
}
