using System;
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

        [Header("Dice score")]
        [Tooltip("Painel de pontuação do minigame de dados (ex.: DiceScore_Menu). Ativo só durante DiceAttack.")]
        [SerializeField] private GameObject _diceScoreAreaRoot;
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

        [Header("Skills — mana cost TMP per slot (4 abilities)")]
        [SerializeField] private TMP_Text _skill1CostText;
        [SerializeField] private TMP_Text _skill2CostText;
        [SerializeField] private TMP_Text _skill3CostText;
        [SerializeField] private TMP_Text _skill4CostText;

        [Header("Buttons")]
        [SerializeField] private Button _nextTurnButton;
        [SerializeField] private Button _skill1Button;
        [SerializeField] private Button _skill2Button;
        [SerializeField] private Button _skill3Button;
        [SerializeField] private Button _skill4Button;

        [SerializeField] private float _tweenDuration = 0.35f;
        [SerializeField] private Ease _tweenEase = Ease.OutQuad;

        private float _playerHpDisplayFloat;
        private float _playerPreviewHpDisplayFloat;
        private float _bossHpDisplayFloat;
        private float _playerApDisplayFloat;

        private GamePlayDiceAttackPanelView _dicePanelResolved;

        private void Awake()
        {
            SetDiceScoreAreaActive(false);
        }

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

        private void OnDiceUiProgress(System.Collections.Generic.List<int> pRolls, int pSum,
            System.Collections.Generic.List<int> bRolls, int bSum)
        {
            if (!DiceAttackRuntimeService.IsActive && !MinigameRuntimeService.IsActive) return;
            SetDiceScoreAreaActive(true);
            ResolveDicePanel()?.ApplyProgress(pSum, bSum);
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
            if (_diceScoreAreaRoot != null) _diceScoreAreaRoot.SetActive(active);
        }

        public void InitStartPoint()
        {
            if (_hudRoot == null) _hudRoot = GetComponent<RectTransform>();
        }

        public void RegisterCallbacks(Action onNextTurn, Action onSkill1, Action onSkill2, Action onSkill3, Action onSkill4)
        {
            Bind(_nextTurnButton, onNextTurn);
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

        public void OnSkill1CostChange(int cost) => SetIntText(_skill1CostText, cost);

        public void OnSkill2CostChange(int cost) => SetIntText(_skill2CostText, cost);

        public void OnSkill3CostChange(int cost) => SetIntText(_skill3CostText, cost);

        public void OnSkill4CostChange(int cost) => SetIntText(_skill4CostText, cost);

        public void OnSkill1NameChange(string name) { }

        public void OnSkill2NameChange(string name) { }

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
