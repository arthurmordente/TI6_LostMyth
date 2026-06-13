using System.Collections.Generic;
using DG.Tweening;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui
{
    /// <summary>
    /// UI do placar de dados no <c>DiceScore_Menu</c>: somas nos painéis Laki/Erza, reordenação no VLG,
    /// opcional <c>DiceSum_Menu</c> com <see cref="UiSlidableAnchoredPanel"/> multi-estado e TMP explícitos Dado 1–3 por lado
    /// (slot null = não existe na UI). Filas Laki/jogador opcionais para animar troca de ordem com o painel principal.
    /// </summary>
    public sealed class GamePlayDiceAttackPanelView : MonoBehaviour
    {
        [Header("Rows — RectTransforms dos painéis Laki / jogador (irmãos sob o mesmo Vertical Layout Group)")]
        [FormerlySerializedAs("_lakiScoreRow")]
        [SerializeField] private RectTransform _lakiPanelRoot;
        [FormerlySerializedAs("_playerScoreRow")]
        [SerializeField] private RectTransform _playerPanelRoot;
        [Tooltip("Pai dos dois painéis (Vertical Layout Group). Recomendado atribuir explicitamente.")]
        [SerializeField] private RectTransform _diceScoreRowsParent;

        [Header("Sums")]
        [FormerlySerializedAs("_lakiDiceSumText")]
        [SerializeField] private TMP_Text _lakiSumText;
        [FormerlySerializedAs("_playerDiceSumText")]
        [SerializeField] private TMP_Text _playerSumText;

        [Header("Dice sum side panel (DiceSum_Menu)")]
        [Tooltip("Opcional. Índices com 3 estados (offsets no slidable): [0] se algum lado tiver 2 dados; [1] se ambos tiverem só 1; último índice se algum tiver 3+. Fonte: LakiDiceAttackState durante DiceAttack.")]
        [SerializeField] private UiSlidableAnchoredPanel _diceSumSlidable;
        [Tooltip("Opcional. Rect da fila Laki no VLG do DiceSum (ex. LakiPoints). Se vazio, usa o parent do 1.º TMP Laki atribuído.")]
        [SerializeField] private RectTransform _diceSumLakiRow;
        [Tooltip("Opcional. Rect da fila jogador (ex. ErzaPoints). Se vazio, usa o parent do 1.º TMP jogador atribuído.")]
        [SerializeField] private RectTransform _diceSumPlayerRow;

        [Header("DiceSum_Menu — Laki (Dado 1 = índice 0 na lista do boss)")]
        [SerializeField] private TMP_Text _diceSumLakiDie1;
        [SerializeField] private TMP_Text _diceSumLakiDie2;
        [SerializeField] private TMP_Text _diceSumLakiDie3;

        [Header("DiceSum_Menu — Jogador (Dado 1 = índice 0 na lista do jogador)")]
        [SerializeField] private TMP_Text _diceSumPlayerDie1;
        [SerializeField] private TMP_Text _diceSumPlayerDie2;
        [SerializeField] private TMP_Text _diceSumPlayerDie3;

        [Header("Per-die column (fallback sem DiceSum_Menu)")]
        [Tooltip("Só usado se DiceSum_Menu não estiver configurado. Filho do painel do jogador.")]
        [SerializeField] private RectTransform _playerPerDieColumn;
        [SerializeField] private RectTransform _lakiPerDieColumn;

        [Header("Punch")]
        [SerializeField] private float _valuePunchDuration = 0.22f;
        [SerializeField] private float _valuePunchStrength = 0.18f;
        [SerializeField] private int _valuePunchVibrato = 10;
        [SerializeField] private float _valuePunchElasticity = 0.4f;

        [Header("Resolution celebration")]
        [SerializeField] private float _celebrationBoardPunchStrength = 0.12f;
        [SerializeField] private float _celebrationBoardPunchDuration = 0.45f;
        [SerializeField] private int _celebrationBoardPunchVibrato = 8;

        [Header("Ranking")]
        [Tooltip("Se false, a Laki fica em cima no início da ronda (0–0).")]
        [SerializeField] private bool _diceRoundStartsWithPlayerOnTop;
        [SerializeField] private float _rankSwapDuration = 0.28f;
        [SerializeField] private Ease _rankSwapEase = Ease.OutQuad;

        private VerticalLayoutGroup _rowsVerticalLayout;
        private VerticalLayoutGroup _diceSumRowsVerticalLayout;

        private Sequence _rankSwapSequence;

        private bool? _rankAnimTargetPlayerAbove;

        private readonly List<int> _lastPlayerRolls = new List<int>(8);
        private readonly List<int> _lastBossRolls = new List<int>(8);
        private int _lastPlayerSum;
        private int _lastBossSum;
        private bool _haveSeededUi;

        private readonly List<TMP_Text> _playerDieCells = new List<TMP_Text>(8);
        private readonly List<TMP_Text> _lakiDieCells = new List<TMP_Text>(8);

        private void Awake()
        {
            EnsureLayoutInitialized();
            EnsureDiceSumRowsInitialized();
        }

        private void OnDisable() => KillRankSwapTween();

        public void PrepareRoundStart()
        {
            EnsureLayoutInitialized();
            EnsureDiceSumRowsInitialized();
            KillRankSwapTween();
            KillAllValuePunches();
            _haveSeededUi = false;
            _lastPlayerRolls.Clear();
            _lastBossRolls.Clear();
            _lastPlayerSum = 0;
            _lastBossSum = 0;
            SetSums(0, 0, punchPlayer: false, punchBoss: false);
            SetPerDieColumnActive(_playerPerDieColumn, false);
            SetPerDieColumnActive(_lakiPerDieColumn, false);
            ClearDiceSumTexts();
            UpdateDiceSumSlidableState(
                LakiDiceAttackState.PlayerDiceCount,
                LakiDiceAttackState.BossDiceCount,
                instant: true);
            ApplyRowOrder(_diceRoundStartsWithPlayerOnTop);
            ApplyDiceSumRowOrder(_diceRoundStartsWithPlayerOnTop);
            RebuildRowsLayout();
            RebuildDiceSumRowsLayout();
        }

        public void PlayResolutionCelebration(bool playerWon, bool isTie)
        {
            EnsureLayoutInitialized();
            EnsureDiceSumRowsInitialized();

            if (_diceSumSlidable != null && HasAnyDiceSumTextAssigned())
            {
                int last = Mathf.Max(0, _diceSumSlidable.StateCount - 1);
                _diceSumSlidable.SetStateIndex(last, instant: false);
            }

            var shakeRoot = _diceScoreRowsParent != null ? _diceScoreRowsParent : transform as RectTransform;
            if (shakeRoot != null)
            {
                DOTween.Kill(shakeRoot, false);
                shakeRoot.localScale = Vector3.one;
                shakeRoot.DOPunchScale(
                    Vector3.one * _celebrationBoardPunchStrength,
                    _celebrationBoardPunchDuration,
                    _celebrationBoardPunchVibrato,
                    _valuePunchElasticity).SetEase(Ease.OutQuad);
            }

            if (!isTie)
            {
                if (playerWon && _playerSumText != null)
                    PunchValue(_playerSumText.rectTransform);
                else if (!playerWon && _lakiSumText != null)
                    PunchValue(_lakiSumText.rectTransform);
            }
        }

        public void ApplyProgress(DiceUiProgressPayload payload)
        {
            if (payload == null) return;
            EnsureLayoutInitialized();
            EnsureDiceSumRowsInitialized();

            var pRolls = payload.PlayerRolls ?? new List<int>();
            var bRolls = payload.BossRolls ?? new List<int>();

            bool playerExplicit = payload.PlayerSlotPunch != null;
            bool bossExplicit = payload.BossSlotPunch != null;

            bool punchPlayerSum = payload.PunchPlayerSum;
            if (!punchPlayerSum && !playerExplicit)
                punchPlayerSum = ShouldLegacySumPunch(pRolls, payload.PlayerSum, _lastPlayerRolls, _lastPlayerSum);

            bool punchBossSum = payload.PunchBossSum;
            if (!punchBossSum && !bossExplicit)
                punchBossSum = ShouldLegacySumPunch(bRolls, payload.BossSum, _lastBossRolls, _lastBossSum);

            SetSums(payload.PlayerSum, payload.BossSum, punchPlayerSum, punchBossSum);

            if (UseExternalDiceSumPanel())
            {
                SetPerDieColumnActive(_playerPerDieColumn, false);
                SetPerDieColumnActive(_lakiPerDieColumn, false);
                UpdateDiceSumSlidableState(pRolls.Count, bRolls.Count, instant: !_haveSeededUi);
                SyncDiceSumTexts(bRolls, pRolls, payload.BossSlotPunch, payload.PlayerSlotPunch);
            }
            else
            {
                SyncPerDieColumn(true, pRolls, payload.PlayerSlotPunch);
                SyncPerDieColumn(false, bRolls, payload.BossSlotPunch);
            }

            RefreshRankingAfterScores(payload.PlayerSum, payload.BossSum);

            CopySnapshot(pRolls, bRolls, payload.PlayerSum, payload.BossSum);
        }

        private bool UseExternalDiceSumPanel() =>
            _diceSumSlidable != null && HasAnyDiceSumTextAssigned();

        private bool HasAnyDiceSumTextAssigned() =>
            _diceSumLakiDie1 != null || _diceSumLakiDie2 != null || _diceSumLakiDie3 != null
            || _diceSumPlayerDie1 != null || _diceSumPlayerDie2 != null || _diceSumPlayerDie3 != null;

        private RectTransform ResolveDiceSumLakiRow()
        {
            if (_diceSumLakiRow != null) return _diceSumLakiRow;
            var t = _diceSumLakiDie1 ?? _diceSumLakiDie2 ?? _diceSumLakiDie3;
            return t != null ? t.transform.parent as RectTransform : null;
        }

        private RectTransform ResolveDiceSumPlayerRow()
        {
            if (_diceSumPlayerRow != null) return _diceSumPlayerRow;
            var t = _diceSumPlayerDie1 ?? _diceSumPlayerDie2 ?? _diceSumPlayerDie3;
            return t != null ? t.transform.parent as RectTransform : null;
        }

        /// <summary>
        /// Escolhe o índice do <see cref="UiSlidableAnchoredPanel"/> conforme quantidade de dados por lado.
        /// Durante <see cref="DiceAttackRuntimeService.IsActive"/>, usa <see cref="LakiDiceAttackState"/> (valores do <c>BossAttack</c> / sessão).
        /// Caso contrário (ex. minigame de dados), usa os fallbacks (tamanho das listas no payload).
        /// Regra: algum com 3+ → último estado (aberto); algum com 2 → estado 0; ambos com 1 → estado 1.
        /// </summary>
        private void UpdateDiceSumSlidableState(int fallbackPlayerDice, int fallbackBossDice, bool instant)
        {
            if (_diceSumSlidable == null || !HasAnyDiceSumTextAssigned()) return;

            int p = DiceAttackRuntimeService.IsActive
                ? LakiDiceAttackState.PlayerDiceCount
                : Mathf.Max(1, fallbackPlayerDice);
            int b = DiceAttackRuntimeService.IsActive
                ? LakiDiceAttackState.BossDiceCount
                : Mathf.Max(1, fallbackBossDice);

            int n = _diceSumSlidable.StateCount;
            int last = Mathf.Max(0, n - 1);
            int max = Mathf.Max(p, b);
            int idx;
            if (max >= 3)
                idx = last;
            else if (max >= 2)
                idx = 0;
            else
                idx = Mathf.Min(1, last);

            _diceSumSlidable.SetStateIndex(idx, instant);
        }

        private void EnsureDiceSumRowsInitialized()
        {
            if (!HasAnyDiceSumTextAssigned()) return;
            var lr = ResolveDiceSumLakiRow();
            var pr = ResolveDiceSumPlayerRow();
            if (lr != null && pr != null && lr.parent == pr.parent)
                _diceSumRowsVerticalLayout = lr.parent.GetComponent<VerticalLayoutGroup>();
        }

        private void ClearDiceSumTexts()
        {
            if (!HasAnyDiceSumTextAssigned()) return;
            ClearDieSlot(_diceSumLakiDie1);
            ClearDieSlot(_diceSumLakiDie2);
            ClearDieSlot(_diceSumLakiDie3);
            ClearDieSlot(_diceSumPlayerDie1);
            ClearDieSlot(_diceSumPlayerDie2);
            ClearDieSlot(_diceSumPlayerDie3);
        }

        private static void ClearDieSlot(TMP_Text tmp)
        {
            if (tmp != null) tmp.SetText(string.Empty);
        }

        private void SyncDiceSumTexts(List<int> bossRolls, List<int> playerRolls, bool[] bossPunch, bool[] playerPunch)
        {
            FillThreeDiceSlots(_diceSumLakiDie1, _diceSumLakiDie2, _diceSumLakiDie3, bossRolls, bossPunch, _lastBossRolls);
            FillThreeDiceSlots(_diceSumPlayerDie1, _diceSumPlayerDie2, _diceSumPlayerDie3, playerRolls, playerPunch, _lastPlayerRolls);
        }

        /// <summary>Dado k → TMP do mesmo índice (k-1). TMP null = slot não usado na UI.</summary>
        private void FillThreeDiceSlots(TMP_Text d1, TMP_Text d2, TMP_Text d3, List<int> rolls, bool[] explicitPunch, List<int> lastRolls)
        {
            rolls ??= new List<int>();
            FillOneDieSlot(0, d1, rolls, explicitPunch, lastRolls);
            FillOneDieSlot(1, d2, rolls, explicitPunch, lastRolls);
            FillOneDieSlot(2, d3, rolls, explicitPunch, lastRolls);
        }

        private void FillOneDieSlot(int dieIndex, TMP_Text tmp, List<int> rolls, bool[] explicitPunch, List<int> lastRolls)
        {
            if (tmp == null) return;
            if (dieIndex < rolls.Count)
            {
                tmp.SetText(rolls[dieIndex].ToString());
                if (ShouldPunchSlot(dieIndex, rolls, explicitPunch, lastRolls))
                    PunchValue(tmp.rectTransform);
            }
            else
                tmp.SetText(string.Empty);
        }

        private void RebuildDiceSumRowsLayout()
        {
            var lr = ResolveDiceSumLakiRow();
            var pr = ResolveDiceSumPlayerRow();
            if (lr == null || pr == null || lr.parent == null) return;
            if (lr.parent != pr.parent) return;
            var p = lr.parent as RectTransform;
            if (p == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(p);
        }

        private void ApplyDiceSumRowOrder(bool playerRankedAbove) =>
            ApplyPairRowOrder(ResolveDiceSumLakiRow(), ResolveDiceSumPlayerRow(), _diceSumRowsVerticalLayout, playerRankedAbove);

        private void CopySnapshot(List<int> pRolls, List<int> bRolls, int pSum, int bSum)
        {
            _lastPlayerRolls.Clear();
            _lastPlayerRolls.AddRange(pRolls);
            _lastBossRolls.Clear();
            _lastBossRolls.AddRange(bRolls);
            _lastPlayerSum = pSum;
            _lastBossSum = bSum;
            _haveSeededUi = true;
        }

        private bool ShouldLegacySumPunch(List<int> rolls, int newSum, List<int> lastRolls, int lastSum)
        {
            if (!_haveSeededUi) return false;
            if (rolls == null || lastRolls == null) return false;
            if (rolls.Count != lastRolls.Count) return false;
            return newSum != lastSum;
        }

        private void SyncPerDieColumn(bool isPlayer, List<int> rolls, bool[] explicitPunch)
        {
            if (UseExternalDiceSumPanel()) return;

            var panel = isPlayer ? _playerPanelRoot : _lakiPanelRoot;
            var style = isPlayer ? _playerSumText : _lakiSumText;
            var cells = isPlayer ? _playerDieCells : _lakiDieCells;
            var last = isPlayer ? _lastPlayerRolls : _lastBossRolls;

            if (panel == null || rolls == null)
            {
                SetPerDieColumnActive(isPlayer ? _playerPerDieColumn : _lakiPerDieColumn, false);
                return;
            }

            bool show = rolls.Count >= 2;
            if (!show)
            {
                SetPerDieColumnActive(isPlayer ? _playerPerDieColumn : _lakiPerDieColumn, false);
                return;
            }

            if (isPlayer)
                EnsurePerDieColumn(ref _playerPerDieColumn, panel, true);
            else
                EnsurePerDieColumn(ref _lakiPerDieColumn, panel, false);

            RectTransform col = isPlayer ? _playerPerDieColumn : _lakiPerDieColumn;
            SetPerDieColumnActive(col, true);
            EnsureDieCellCount(col, cells, style, rolls.Count);

            for (int i = 0; i < rolls.Count; i++)
            {
                var tmp = cells[i];
                if (tmp == null) continue;
                tmp.SetText(rolls[i].ToString());
                bool punchSlot = ShouldPunchSlot(i, rolls, explicitPunch, last);
                if (punchSlot)
                    PunchValue(tmp.rectTransform);
            }
        }

        private bool ShouldPunchSlot(int i, List<int> rolls, bool[] explicitPunch, List<int> lastRolls)
        {
            if (explicitPunch != null)
                return i < explicitPunch.Length && explicitPunch[i];
            if (!_haveSeededUi) return false;
            if (rolls == null || lastRolls == null) return false;
            if (rolls.Count != lastRolls.Count) return false;
            if (i >= lastRolls.Count) return false;
            return rolls[i] != lastRolls[i];
        }

        private void SetPerDieColumnActive(RectTransform col, bool active)
        {
            if (col != null)
                col.gameObject.SetActive(active);
        }

        private void EnsurePerDieColumn(ref RectTransform col, RectTransform panelRoot, bool isPlayer)
        {
            if (col != null) return;
            var hlg = panelRoot.GetComponent<HorizontalLayoutGroup>();
            var go = new GameObject(isPlayer ? "PlayerPerDieColumn" : "LakiPerDieColumn");
            var rt = go.AddComponent<RectTransform>();
            rt.SetParent(panelRoot, false);
            rt.SetAsFirstSibling();
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 40f;
            le.minWidth = 32f;
            le.flexibleWidth = 0f;
            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(0, 8, 0, 0);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            col = rt;
            if (hlg != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot);
        }

        private static void EnsureDieCellCount(RectTransform column, List<TMP_Text> cells, TMP_Text styleSource, int count)
        {
            while (cells.Count < count)
            {
                int idx = cells.Count;
                var cellGo = new GameObject($"DieValue_{idx}");
                var cellRt = cellGo.AddComponent<RectTransform>();
                cellRt.SetParent(column, false);
                var tmp = cellGo.AddComponent<TextMeshProUGUI>();
                ApplyTmpStyle(styleSource, tmp);
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.text = "0";
                var cellLe = cellGo.AddComponent<LayoutElement>();
                cellLe.preferredHeight = 24f;
                cellLe.minHeight = 20f;
                cells.Add(tmp);
            }

            for (int i = cells.Count - 1; i >= count; i--)
            {
                if (cells[i] != null && cells[i].gameObject != null)
                    Destroy(cells[i].gameObject);
                cells.RemoveAt(i);
            }
        }

        private static void ApplyTmpStyle(TMP_Text from, TMP_Text to)
        {
            if (to == null) return;
            if (from != null)
            {
                to.font = from.font;
                to.fontSharedMaterial = from.fontSharedMaterial;
                to.fontSize = Mathf.Max(8f, from.fontSize * 0.72f);
                to.fontStyle = from.fontStyle;
                to.color = from.color;
                to.outlineWidth = from.outlineWidth;
                to.outlineColor = from.outlineColor;
            }
        }

        private void PunchValue(RectTransform rt)
        {
            if (rt == null) return;
            DOTween.Kill(rt, false);
            rt.localScale = Vector3.one;
            rt.DOPunchScale(Vector3.one * _valuePunchStrength, _valuePunchDuration, _valuePunchVibrato, _valuePunchElasticity)
                .SetEase(Ease.OutQuad);
        }

        private void KillAllValuePunches()
        {
            KillTmpPunch(_playerSumText);
            KillTmpPunch(_lakiSumText);
            KillCellsPunch(_playerDieCells);
            KillCellsPunch(_lakiDieCells);
            KillTmpPunch(_diceSumLakiDie1);
            KillTmpPunch(_diceSumLakiDie2);
            KillTmpPunch(_diceSumLakiDie3);
            KillTmpPunch(_diceSumPlayerDie1);
            KillTmpPunch(_diceSumPlayerDie2);
            KillTmpPunch(_diceSumPlayerDie3);
        }

        private void KillTmpPunch(TMP_Text t)
        {
            if (t == null) return;
            DOTween.Kill(t.rectTransform, false);
            t.rectTransform.localScale = Vector3.one;
        }

        private void KillCellsPunch(List<TMP_Text> cells)
        {
            if (cells == null) return;
            for (int i = 0; i < cells.Count; i++)
                KillTmpPunch(cells[i]);
        }

        private void EnsureLayoutInitialized()
        {
            if (_rowsVerticalLayout != null && _diceScoreRowsParent != null) return;

            var parent = _diceScoreRowsParent;
            if (parent == null && _lakiPanelRoot != null && _playerPanelRoot != null
                && _lakiPanelRoot.parent == _playerPanelRoot.parent)
                parent = _lakiPanelRoot.parent as RectTransform;

            _diceScoreRowsParent = parent;
            _rowsVerticalLayout = _diceScoreRowsParent != null
                ? _diceScoreRowsParent.GetComponent<VerticalLayoutGroup>()
                : null;
        }

        private void SetSums(int playerSum, int lakiSum, bool punchPlayer, bool punchBoss)
        {
            if (_playerSumText != null)
            {
                _playerSumText.SetText(playerSum.ToString());
                if (punchPlayer) PunchValue(_playerSumText.rectTransform);
            }
            if (_lakiSumText != null)
            {
                _lakiSumText.SetText(lakiSum.ToString());
                if (punchBoss) PunchValue(_lakiSumText.rectTransform);
            }
        }

        private bool PlayerRowIsAboveLakiBySibling()
        {
            if (_playerPanelRoot == null || _lakiPanelRoot == null) return false;
            if (_playerPanelRoot.parent != _lakiPanelRoot.parent) return false;
            bool reverse = _rowsVerticalLayout != null && _rowsVerticalLayout.reverseArrangement;
            int pi = _playerPanelRoot.GetSiblingIndex();
            int li = _lakiPanelRoot.GetSiblingIndex();
            return reverse ? pi > li : pi < li;
        }

        private bool EffectiveRankPlayerAboveLaki()
        {
            if (_rankAnimTargetPlayerAbove.HasValue)
                return _rankAnimTargetPlayerAbove.Value;
            return PlayerRowIsAboveLakiBySibling();
        }

        private void ApplyRowOrder(bool playerRankedAbove) =>
            ApplyPairRowOrder(_lakiPanelRoot, _playerPanelRoot, _rowsVerticalLayout, playerRankedAbove);

        private static void ApplyPairRowOrder(RectTransform lakiRow, RectTransform playerRow, VerticalLayoutGroup vlg, bool playerRankedAbove)
        {
            if (lakiRow == null || playerRow == null) return;
            if (lakiRow.parent != playerRow.parent) return;

            bool reverse = vlg != null && vlg.reverseArrangement;
            if (!reverse)
            {
                if (playerRankedAbove)
                {
                    playerRow.SetSiblingIndex(0);
                    lakiRow.SetSiblingIndex(1);
                }
                else
                {
                    lakiRow.SetSiblingIndex(0);
                    playerRow.SetSiblingIndex(1);
                }
            }
            else
            {
                if (playerRankedAbove)
                {
                    lakiRow.SetSiblingIndex(0);
                    playerRow.SetSiblingIndex(1);
                }
                else
                {
                    playerRow.SetSiblingIndex(0);
                    lakiRow.SetSiblingIndex(1);
                }
            }
        }

        private void RebuildRowsLayout()
        {
            if (_diceScoreRowsParent == null) return;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_diceScoreRowsParent);
        }

        private void RefreshRankingAfterScores(int playerSum, int lakiSum)
        {
            if (_lakiPanelRoot == null || _playerPanelRoot == null) return;
            if (_lakiPanelRoot.parent != _playerPanelRoot.parent) return;

            if (playerSum == lakiSum)
                return;

            bool wantPlayerAbove = playerSum > lakiSum;
            if (wantPlayerAbove == EffectiveRankPlayerAboveLaki())
                return;

            AnimateRankSwap(wantPlayerAbove);
        }

        private void AnimateRankSwap(bool playerShouldEndOnTop)
        {
            KillRankSwapTween();
            _rankAnimTargetPlayerAbove = playerShouldEndOnTop;

            var vlg = _rowsVerticalLayout;
            if (vlg == null && _diceScoreRowsParent != null)
                vlg = _diceScoreRowsParent.GetComponent<VerticalLayoutGroup>();

            Canvas.ForceUpdateCanvases();
            if (_diceScoreRowsParent != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(_diceScoreRowsParent);
            if (vlg != null) vlg.enabled = false;

            VerticalLayoutGroup vlgSum = null;
            var sumLr = ResolveDiceSumLakiRow();
            var sumPr = ResolveDiceSumPlayerRow();
            bool swapDiceSum = HasAnyDiceSumTextAssigned()
                && sumLr != null && sumPr != null && sumLr.parent == sumPr.parent;
            if (swapDiceSum)
            {
                vlgSum = _diceSumRowsVerticalLayout;
                if (vlgSum == null && sumLr.parent != null)
                    vlgSum = sumLr.parent.GetComponent<VerticalLayoutGroup>();
                Canvas.ForceUpdateCanvases();
                var sumParent = sumLr.parent as RectTransform;
                if (sumParent != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(sumParent);
                if (vlgSum != null) vlgSum.enabled = false;
            }

            float dur = _rankSwapDuration;

            Vector3 pWorld = _playerPanelRoot.position;
            Vector3 lWorld = _lakiPanelRoot.position;
            bool useWorld = (pWorld - lWorld).sqrMagnitude > 1e-8f;

            var seq = DOTween.Sequence();
            if (useWorld)
            {
                seq.Join(_playerPanelRoot.DOMove(lWorld, dur).SetEase(_rankSwapEase));
                seq.Join(_lakiPanelRoot.DOMove(pWorld, dur).SetEase(_rankSwapEase));
            }
            else
            {
                Vector2 pPos = _playerPanelRoot.anchoredPosition;
                Vector2 lPos = _lakiPanelRoot.anchoredPosition;
                seq.Join(_playerPanelRoot.DOAnchorPos(lPos, dur).SetEase(_rankSwapEase));
                seq.Join(_lakiPanelRoot.DOAnchorPos(pPos, dur).SetEase(_rankSwapEase));
            }

            if (swapDiceSum)
            {
                Vector3 dp = sumPr.position;
                Vector3 dl = sumLr.position;
                bool useWorldSum = (dp - dl).sqrMagnitude > 1e-8f;
                if (useWorldSum)
                {
                    seq.Join(sumPr.DOMove(dl, dur).SetEase(_rankSwapEase));
                    seq.Join(sumLr.DOMove(dp, dur).SetEase(_rankSwapEase));
                }
                else
                {
                    Vector2 p2 = sumPr.anchoredPosition;
                    Vector2 l2 = sumLr.anchoredPosition;
                    seq.Join(sumPr.DOAnchorPos(l2, dur).SetEase(_rankSwapEase));
                    seq.Join(sumLr.DOAnchorPos(p2, dur).SetEase(_rankSwapEase));
                }
            }

            seq.OnComplete(() =>
            {
                ApplyRowOrder(playerShouldEndOnTop);
                ApplyDiceSumRowOrder(playerShouldEndOnTop);
                if (vlg != null) vlg.enabled = true;
                if (vlgSum != null) vlgSum.enabled = true;
                RebuildRowsLayout();
                RebuildDiceSumRowsLayout();
                _rankAnimTargetPlayerAbove = null;
            });
            _rankSwapSequence = seq;
        }

        private void KillRankSwapTween()
        {
            if (_rankSwapSequence != null && _rankSwapSequence.IsActive())
                _rankSwapSequence.Kill();
            _rankSwapSequence = null;
            _rankAnimTargetPlayerAbove = null;
            if (_rowsVerticalLayout != null) _rowsVerticalLayout.enabled = true;
            if (_diceSumRowsVerticalLayout != null) _diceSumRowsVerticalLayout.enabled = true;
            if (_playerPanelRoot != null) DOTween.Kill(_playerPanelRoot, false);
            if (_lakiPanelRoot != null) DOTween.Kill(_lakiPanelRoot, false);
            var kr = ResolveDiceSumPlayerRow();
            var lr = ResolveDiceSumLakiRow();
            if (kr != null) DOTween.Kill(kr, false);
            if (lr != null) DOTween.Kill(lr, false);
        }
    }
}
