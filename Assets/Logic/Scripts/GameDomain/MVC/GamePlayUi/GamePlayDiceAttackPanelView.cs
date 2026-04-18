using System.Collections.Generic;
using DG.Tweening;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui
{
    /// <summary>
    /// UI do placar de dados: soma por lado, coluna opcional com um TMP por dado (2+ dados),
    /// reordenação no Vertical Layout e punch nos números quando valores mudam por acerto.
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

        [Header("Per-die column (2+ dice)")]
        [Tooltip("Opcional. Filho do painel do jogador; se vazio, cria automaticamente à esquerda no HorizontalLayoutGroup.")]
        [SerializeField] private RectTransform _playerPerDieColumn;
        [Tooltip("Opcional. Filho do painel Laki; se vazio, cria automaticamente.")]
        [SerializeField] private RectTransform _lakiPerDieColumn;

        [Header("Punch")]
        [SerializeField] private float _valuePunchDuration = 0.22f;
        [SerializeField] private float _valuePunchStrength = 0.18f;
        [SerializeField] private int _valuePunchVibrato = 10;
        [SerializeField] private float _valuePunchElasticity = 0.4f;

        [Header("Ranking")]
        [Tooltip("Se false, a Laki fica em cima no início da ronda (0–0).")]
        [SerializeField] private bool _diceRoundStartsWithPlayerOnTop;
        [SerializeField] private float _rankSwapDuration = 0.28f;
        [SerializeField] private Ease _rankSwapEase = Ease.OutQuad;

        private VerticalLayoutGroup _rowsVerticalLayout;

        private Sequence _rankSwapSequence;

        private bool? _rankAnimTargetPlayerAbove;

        private readonly List<int> _lastPlayerRolls = new List<int>(8);
        private readonly List<int> _lastBossRolls = new List<int>(8);
        private int _lastPlayerSum;
        private int _lastBossSum;
        private bool _haveSeededUi;

        private readonly List<TMP_Text> _playerDieCells = new List<TMP_Text>(8);
        private readonly List<TMP_Text> _lakiDieCells = new List<TMP_Text>(8);

        private void Awake() => EnsureLayoutInitialized();

        private void OnDisable() => KillRankSwapTween();

        public void PrepareRoundStart()
        {
            EnsureLayoutInitialized();
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
            ApplyRowOrder(_diceRoundStartsWithPlayerOnTop);
            RebuildRowsLayout();
        }

        public void ApplyProgress(DiceUiProgressPayload payload)
        {
            if (payload == null) return;
            EnsureLayoutInitialized();

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

            SyncPerDieColumn(true, pRolls, payload.PlayerSlotPunch);
            SyncPerDieColumn(false, bRolls, payload.BossSlotPunch);

            RefreshRankingAfterScores(payload.PlayerSum, payload.BossSum);

            CopySnapshot(pRolls, bRolls, payload.PlayerSum, payload.BossSum);
        }

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
                tmp.enableWordWrapping = false;
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

        private void ApplyRowOrder(bool playerRankedAbove)
        {
            if (_lakiPanelRoot == null || _playerPanelRoot == null) return;
            if (_lakiPanelRoot.parent != _playerPanelRoot.parent) return;

            bool reverse = _rowsVerticalLayout != null && _rowsVerticalLayout.reverseArrangement;
            if (!reverse)
            {
                if (playerRankedAbove)
                {
                    _playerPanelRoot.SetSiblingIndex(0);
                    _lakiPanelRoot.SetSiblingIndex(1);
                }
                else
                {
                    _lakiPanelRoot.SetSiblingIndex(0);
                    _playerPanelRoot.SetSiblingIndex(1);
                }
            }
            else
            {
                if (playerRankedAbove)
                {
                    _lakiPanelRoot.SetSiblingIndex(0);
                    _playerPanelRoot.SetSiblingIndex(1);
                }
                else
                {
                    _playerPanelRoot.SetSiblingIndex(0);
                    _lakiPanelRoot.SetSiblingIndex(1);
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

            seq.OnComplete(() =>
            {
                ApplyRowOrder(playerShouldEndOnTop);
                if (vlg != null) vlg.enabled = true;
                RebuildRowsLayout();
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
        }
    }
}
