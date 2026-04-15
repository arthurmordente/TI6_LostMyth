using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.Scripts.GameDomain.MVC.Ui
{
    /// <summary>
    /// UI do placar de dados: textos das somas e reordenação animada no Vertical Layout.
    /// Eventos vêm do <see cref="GamePlayUiCanvasView"/> (canvas sempre ativo); este script pode estar num filho desativado.
    /// </summary>
    public sealed class GamePlayDiceAttackPanelView : MonoBehaviour
    {
        [Header("Rows — RectTransforms dos painéis Laki / jogador (irmãos sob o mesmo Vertical Layout Group)")]
        [SerializeField] private RectTransform _lakiScoreRow;
        [SerializeField] private RectTransform _playerScoreRow;
        [Tooltip("Pai dos dois painéis (Vertical Layout Group). Recomendado atribuir explicitamente.")]
        [SerializeField] private RectTransform _diceScoreRowsParent;

        [Header("Sums")]
        [SerializeField] private TMP_Text _lakiDiceSumText;
        [SerializeField] private TMP_Text _playerDiceSumText;

        [Header("Ranking")]
        [Tooltip("Se false, a Laki fica em cima no início da ronda (0–0).")]
        [SerializeField] private bool _diceRoundStartsWithPlayerOnTop;
        [SerializeField] private float _rankSwapDuration = 0.28f;
        [SerializeField] private Ease _rankSwapEase = Ease.OutQuad;

        private VerticalLayoutGroup _rowsVerticalLayout;

        private Sequence _rankSwapSequence;

        private void Awake() => EnsureLayoutInitialized();

        private void OnDisable() => KillRankSwapTween();

        /// <summary>Chamado pelo canvas ao iniciar DiceAttack (depois de ativar o painel).</summary>
        public void PrepareRoundStart()
        {
            EnsureLayoutInitialized();
            KillRankSwapTween();
            SetSums(0, 0);
            ApplyRowOrder(_diceRoundStartsWithPlayerOnTop);
            RebuildRowsLayout();
        }

        /// <summary>Chamado pelo canvas a cada <see cref="Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice.DiceUiRuntime.OnProgress"/>.</summary>
        public void ApplyProgress(int playerSum, int lakiSum)
        {
            EnsureLayoutInitialized();
            SetSums(playerSum, lakiSum);
            RefreshRankingAfterScores(playerSum, lakiSum);
        }

        private void EnsureLayoutInitialized()
        {
            if (_rowsVerticalLayout != null && _diceScoreRowsParent != null) return;

            var parent = _diceScoreRowsParent;
            if (parent == null && _lakiScoreRow != null && _playerScoreRow != null
                && _lakiScoreRow.parent == _playerScoreRow.parent)
                parent = _lakiScoreRow.parent as RectTransform;

            _diceScoreRowsParent = parent;
            _rowsVerticalLayout = _diceScoreRowsParent != null
                ? _diceScoreRowsParent.GetComponent<VerticalLayoutGroup>()
                : null;
        }

        private void SetSums(int playerSum, int lakiSum)
        {
            if (_playerDiceSumText != null) _playerDiceSumText.SetText(playerSum.ToString());
            if (_lakiDiceSumText != null) _lakiDiceSumText.SetText(lakiSum.ToString());
        }

        private bool PlayerRowIsAboveLaki()
        {
            if (_playerScoreRow == null || _lakiScoreRow == null) return false;
            if (_playerScoreRow.parent != _lakiScoreRow.parent) return false;
            return _playerScoreRow.GetSiblingIndex() < _lakiScoreRow.GetSiblingIndex();
        }

        private void ApplyRowOrder(bool playerRankedAbove)
        {
            if (_lakiScoreRow == null || _playerScoreRow == null) return;
            if (_lakiScoreRow.parent != _playerScoreRow.parent) return;

            if (playerRankedAbove)
            {
                _playerScoreRow.SetSiblingIndex(0);
                _lakiScoreRow.SetSiblingIndex(1);
            }
            else
            {
                _lakiScoreRow.SetSiblingIndex(0);
                _playerScoreRow.SetSiblingIndex(1);
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
            if (_lakiScoreRow == null || _playerScoreRow == null) return;
            if (_lakiScoreRow.parent != _playerScoreRow.parent) return;

            if (playerSum == lakiSum)
                return;

            bool wantPlayerAbove = playerSum > lakiSum;
            if (wantPlayerAbove == PlayerRowIsAboveLaki())
                return;

            AnimateRankSwap(wantPlayerAbove);
        }

        private void AnimateRankSwap(bool playerShouldEndOnTop)
        {
            KillRankSwapTween();

            var vlg = _rowsVerticalLayout;
            if (vlg == null && _diceScoreRowsParent != null)
                vlg = _diceScoreRowsParent.GetComponent<VerticalLayoutGroup>();

            Canvas.ForceUpdateCanvases();
            if (vlg != null) vlg.enabled = false;

            Vector2 pPos = _playerScoreRow.anchoredPosition;
            Vector2 lPos = _lakiScoreRow.anchoredPosition;
            float dur = _rankSwapDuration;

            var seq = DOTween.Sequence();
            seq.Join(_playerScoreRow.DOAnchorPos(lPos, dur).SetEase(_rankSwapEase));
            seq.Join(_lakiScoreRow.DOAnchorPos(pPos, dur).SetEase(_rankSwapEase));
            seq.OnComplete(() =>
            {
                ApplyRowOrder(playerShouldEndOnTop);
                if (vlg != null) vlg.enabled = true;
                RebuildRowsLayout();
            });
            _rankSwapSequence = seq;
        }

        private void KillRankSwapTween()
        {
            if (_rankSwapSequence != null && _rankSwapSequence.IsActive())
                _rankSwapSequence.Kill();
            _rankSwapSequence = null;
            if (_rowsVerticalLayout != null) _rowsVerticalLayout.enabled = true;
        }
    }
}
