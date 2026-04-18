using System;
using System.Collections.Generic;
using DG.Tweening;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui
{
    /// <summary>
    /// uGUI fight HUD. HP/AP fills use 0–1 on the Image; HP/AP numbers tween to match.
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
        [SerializeField] private TMP_Text _playerActionPointsText;

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

        [Header("Buttons")]
        [SerializeField] private Button _nextTurnButton;
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

        private GamePlayDiceAttackPanelView _dicePanelResolved;
        private Action _onSkill1;
        private Action _onSkill2;
        private Action _onSkill3;
        private Action _onSkill4;
        private bool _showBookSkillsTheme;
        private Sequence _turnAnnouncementSequence;

        private void Awake()
        {
            if (_diceSlidablePanel != null && _diceScoreAreaRoot != null)
                _diceScoreAreaRoot.SetActive(true);
            if (_diceSlidablePanel != null)
                _diceSlidablePanel.SetExpanded(false, true);
            else
                SetDiceScoreAreaActive(false);

            ResetTurnAnnouncementHiddenImmediate();
        }

        private void OnDestroy() => KillTurnAnnouncementSequence();

        private void OnEnable()
        {
            DiceAttackRuntimeService.OnDiceAttackBegan += OnDiceAttackBegan;
            DiceAttackRuntimeService.OnDiceAttackEnded += OnDiceAttackEnded;
            DiceUiRuntime.OnProgress += OnDiceUiProgress;
        }

        private void OnDisable()
        {
            DiceAttackRuntimeService.OnDiceAttackBegan -= OnDiceAttackBegan;
            DiceAttackRuntimeService.OnDiceAttackEnded -= OnDiceAttackEnded;
            DiceUiRuntime.OnProgress -= OnDiceUiProgress;
        }

        private void OnDiceAttackBegan()
        {
            SetDiceScoreAreaActive(true);
            ResolveDicePanel()?.PrepareRoundStart();
        }

        private void OnDiceAttackEnded() => SetDiceScoreAreaActive(false);

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

        public void SnapPlayerActionPoints(int current, int max)
        {
            float maxF = Mathf.Max(1, max);
            DOTween.Kill(_playerApFillImage, true);
            DOTween.Kill(_playerActionPointsText, true);
            _playerApDisplayFloat = current;
            SnapFill01(_playerApFillImage, current / maxF);
            SetIntText(_playerActionPointsText, current);
        }

        public void OnPlayerActionPointsChange(int current, int max)
        {
            float maxF = Mathf.Max(1, max);
            float start = _playerApDisplayFloat;
            DOTween.Kill(_playerApFillImage, true);
            DOTween.Kill(_playerActionPointsText, true);
            float v = start;
            var target = _playerApFillImage != null ? (UnityEngine.Object)_playerApFillImage : this;
            DOTween.To(() => v, x =>
            {
                v = x;
                _playerApDisplayFloat = x;
                if (_playerApFillImage != null) _playerApFillImage.fillAmount = Mathf.Clamp01(x / maxF);
                if (_playerActionPointsText != null) _playerActionPointsText.SetText(Mathf.RoundToInt(x).ToString());
            }, current, _tweenDuration).SetEase(_tweenEase).SetTarget(target);
        }

        public void OnSkill1CostChange(int cost) => SetIntText(GetActiveCostText(0), cost);

        public void OnSkill2CostChange(int cost) => SetIntText(GetActiveCostText(1), cost);

        public void OnSkill3CostChange(int cost) => SetIntText(GetActiveCostText(2), cost);

        public void OnSkill4CostChange(int cost) => SetIntText(GetActiveCostText(3), cost);

        public void OnSkill1NameChange(string name) { }

        public void OnSkill2NameChange(string name) { }

        public void ShowBookSkillsTheme(bool showBookSkillsTheme)
        {
            _showBookSkillsTheme = showBookSkillsTheme;
            if (_erzaSkillsBackground != null) _erzaSkillsBackground.SetActive(!showBookSkillsTheme);
            if (_bookSkillsBackground != null) _bookSkillsBackground.SetActive(showBookSkillsTheme);
        }

        /// <inheritdoc />
        public void SetSkillsSlidableExpanded(bool expanded, bool instant = false) =>
            _skillsSlidablePanel?.SetExpanded(expanded, instant);

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
