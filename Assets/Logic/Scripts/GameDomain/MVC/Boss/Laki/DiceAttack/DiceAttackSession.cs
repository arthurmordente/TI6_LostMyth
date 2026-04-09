using System.Collections.Generic;
using System.Threading.Tasks;
using Logic.Scripts.GameDomain.MVC.Boss.Laki.Minigames.Dice;
using Logic.Scripts.GameDomain.MVC.Nara;
using Logic.Scripts.Turns;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace Logic.Scripts.GameDomain.MVC.Boss.Laki.DiceAttack
{
    /// <summary>
    /// Stateless entry: starts a dice attack without any minigame-round prefab.
    /// Lives until <see cref="DestroyDiceAttackRoot"/> (survives destruction of the BossAttack instance).
    /// </summary>
    public static class DiceAttackSession
    {
        public static void Start(in DiceAttackSettings settings, DiContainer sceneContainer)
        {
            var session = new ActiveSession(settings, sceneContainer);
            session.Begin();
        }

        private sealed class ActiveSession : DiceAttackRuntimeService.IResolver, DiceAttackRuntimeService.IPlayerTurnGate, DiceAttackRuntimeService.IStatusProvider, IDiceCallbacks
        {
            private enum Outcome { Pending, PlayerWin, BossWin, Tie }

            private readonly DiceAttackSettings _settings;
            private readonly Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView _arenaView;
            private readonly INaraController _player;
            private readonly Logic.Scripts.GameDomain.MVC.Boss.IBossController _boss;
            private readonly IEnvironmentActorsRegistry _envReg;

            private bool _resolved;
            private bool _playerGateConsumed;
            private DiceAttackResult _final;
            private Outcome _outcome = Outcome.Pending;

            private int _expectedBossDice;
            private int _expectedPlayerDice;

            private readonly List<int> _playerRolls = new List<int>(8);
            private readonly List<int> _bossRolls = new List<int>(8);
            private readonly List<GameObject> _spawnedDice = new List<GameObject>(8);

            private GameObject _promptInstance;

            public ActiveSession(in DiceAttackSettings settings, DiContainer sceneContainer)
            {
                _settings = settings;
                _arenaView = Object.FindFirstObjectByType<Logic.Scripts.GameDomain.MVC.Environment.Laki.LakiRouletteArenaView>();
                _player = sceneContainer != null ? sceneContainer.Resolve<INaraController>() : null;
                _boss = sceneContainer != null ? sceneContainer.Resolve<Logic.Scripts.GameDomain.MVC.Boss.IBossController>() : null;
                try { _envReg = sceneContainer != null ? sceneContainer.Resolve<IEnvironmentActorsRegistry>() : null; }
                catch { _envReg = null; }
            }

            public void Begin()
            {
                _expectedBossDice = Mathf.Max(1, LakiDiceAttackState.BossDiceCount);
                _expectedPlayerDice = Mathf.Max(1, LakiDiceAttackState.PlayerDiceCount);

                DiceAttackRuntimeService.StatusProvider = this;
                DiceAttackRuntimeService.SetActiveName(string.IsNullOrEmpty(_settings.DisplayName) ? "DiceAttack" : _settings.DisplayName);
                DiceAttackRuntimeService.Begin();
                DiceAttackRuntimeService.RegisterResolver(this);
                DiceAttackRuntimeService.RegisterPlayerTurnGate(this);
                DiceUiRuntime.Reset();

                RunBossRollPhase();
            }

            public bool TryResolveAtBossTurn(out DiceAttackResult result)
            {
                if (!_resolved && AllRollsReported()) ResolveNow();
                if (_resolved)
                {
                    result = _final;
                    return true;
                }
                result = default;
                return false;
            }

            public void DestroyDiceAttackRoot()
            {
                HidePlayerPrompt();
                DestroyAllSpawnedDice();
                DiceAttackRuntimeService.UnregisterPlayerTurnGate(this);
                DiceAttackRuntimeService.UnregisterResolver(this);
                DiceAttackRuntimeService.EndAndScheduleBossResolutionSkip();
                try { DiceUiRuntime.Reset(); } catch { }
            }

            public async Task OnPlayerTurnStartAsync()
            {
                if (_resolved || _playerGateConsumed) return;
                _playerGateConsumed = true;
                ShowPlayerPrompt();
                try
                {
                    await WaitAnyInputAsync();
                }
                finally
                {
                    HidePlayerPrompt();
                }
                await Task.Delay(Mathf.RoundToInt(_settings.PlayerRollInputConsumeDelay * 1000f));
                RunPlayerRollPhase();
            }

            public string GetStatus()
            {
                return $"DiceAttack P={Sum(_playerRolls)}/{_expectedPlayerDice} B={Sum(_bossRolls)}/{_expectedBossDice} resolved={_resolved}";
            }

            public void OnDiceRolled(bool isBoss, int rollSlotIndex, int value)
            {
                if (isBoss) SetRollAt(_bossRolls, rollSlotIndex, value);
                else SetRollAt(_playerRolls, rollSlotIndex, value);
                ReportUiProgress();
            }

            public void OnDieValueChanged(bool isBoss, int rollSlotIndex, int value)
            {
                var list = isBoss ? _bossRolls : _playerRolls;
                if (rollSlotIndex < 0 || rollSlotIndex >= list.Count) return;
                list[rollSlotIndex] = value;
                ReportUiProgress();
            }

            public void OnDieAnimationComplete(bool isBoss, int rollSlotIndex, int value)
            {
                var list = isBoss ? _bossRolls : _playerRolls;
                if (rollSlotIndex >= 0 && rollSlotIndex < list.Count)
                    list[rollSlotIndex] = value;
                ReportUiProgress();
            }

            private static void SetRollAt(List<int> list, int slotIndex, int value)
            {
                while (list.Count <= slotIndex) list.Add(0);
                list[slotIndex] = value;
            }

            private void RunBossRollPhase()
            {
                RollForSide(true, LakiDiceAttackState.BossDiceCount, LakiDiceAttackState.BossFaceMin, LakiDiceAttackState.BossFaceMax);
            }

            private void RunPlayerRollPhase()
            {
                RollForSide(false, LakiDiceAttackState.PlayerDiceCount, LakiDiceAttackState.PlayerFaceMin, LakiDiceAttackState.PlayerFaceMax);
            }

            private void RollForSide(bool isBoss, int count, int minFace, int maxFace)
            {
                count = count < 1 ? 1 : count;
                minFace = minFace < 1 ? 1 : minFace;
                maxFace = maxFace < minFace ? minFace : maxFace;
                for (int i = 0; i < count; i++)
                {
                    int value = Random.Range(minFace, maxFace + 1);
                    SpawnDieVisual(isBoss, maxFace, value, i);
                }
            }

            private void SpawnDieVisual(bool isBoss, int maxFace, int value, int rollSlotIndex)
            {
                if (_arenaView == null) return;
                Vector3 spawn = isBoss ? GetBossPosition() : GetPlayerPosition();
                int tile = Random.Range(0, Mathf.Max(1, _arenaView.TileCount));
                var prefab = isBoss ? _settings.BossDiePrefab : _settings.PlayerDiePrefab;
                var go = prefab != null ? Object.Instantiate(prefab, spawn, Quaternion.identity) : new GameObject(isBoss ? "BossDie" : "PlayerDie");
                if (prefab == null) go.transform.position = spawn;
                var actor = go.GetComponent<DiceActor>();
                if (actor == null) actor = go.AddComponent<DiceActor>();
                int hp = _settings.DieHp > 0 ? _settings.DieHp : 99;
                actor.Init(this, isBoss, maxFace, hp, value, _arenaView, tile, spawn, rollSlotIndex, reportRollOnEnvironmentExecute: false);
                _spawnedDice.Add(go);
                _envReg?.Add(actor);
            }

            private bool AllRollsReported()
            {
                return _bossRolls.Count >= _expectedBossDice && _playerRolls.Count >= _expectedPlayerDice;
            }

            private void ResolveNow()
            {
                int playerSum = Sum(_playerRolls);
                int bossSum = Sum(_bossRolls);
                bool playerWon = playerSum > bossSum;
                _outcome = playerSum == bossSum ? Outcome.Tie : (playerWon ? Outcome.PlayerWin : Outcome.BossWin);
                _final = new DiceAttackResult { Completed = true, PlayerWon = playerWon };
                _resolved = true;
                DiceUiRuntime.ReportFinal(playerSum, bossSum);
                string winner = _outcome == Outcome.Tie ? "Tie" : (playerWon ? "Player" : "Boss");
                Debug.Log($"[Laki][DiceAttack] TURN_RESULT Winner={winner} P={playerSum} B={bossSum} outcome={_outcome}");
                DestroyAllSpawnedDice();
            }

            private void ReportUiProgress()
            {
                DiceUiRuntime.ReportProgress(new List<int>(_playerRolls), Sum(_playerRolls), new List<int>(_bossRolls), Sum(_bossRolls));
            }

            private void ShowPlayerPrompt()
            {
                DiceAttackUIRuntime.NotifyPlayerRollPromptShow();
                if (_settings.PlayerRollPromptPrefab == null)
                {
                    Debug.LogWarning("[Laki][DiceAttack] Assign Player Roll Prompt Prefab on BossAttack (DiceAttack) to show on-screen instructions, or listen to DiceAttackUIRuntime.OnPlayerRollPromptShow.");
                    return;
                }
                _promptInstance = Object.Instantiate(_settings.PlayerRollPromptPrefab);
                ApplyDicePromptPresentation(_promptInstance);
                var prompt = _promptInstance.GetComponent<DiceAttackPlayerRollPrompt>();
                if (prompt != null) prompt.Show();
                else _promptInstance.SetActive(true);
            }

            private static void ApplyDicePromptPresentation(GameObject instance)
            {
                if (instance == null) return;
                var rt = instance.GetComponent<RectTransform>();
                if (rt != null) rt.localScale = Vector3.one;
                var rootCanvas = instance.GetComponent<Canvas>();
                if (rootCanvas != null)
                {
                    rootCanvas.overrideSorting = true;
                    rootCanvas.sortingOrder = Mathf.Max(rootCanvas.sortingOrder, 1000);
                }
            }

            private void HidePlayerPrompt()
            {
                DiceAttackUIRuntime.NotifyPlayerRollPromptHide();
                if (_promptInstance != null)
                {
                    Object.Destroy(_promptInstance);
                    _promptInstance = null;
                }
            }

            private void DestroyAllSpawnedDice()
            {
                for (int i = 0; i < _spawnedDice.Count; i++)
                {
                    var go = _spawnedDice[i];
                    if (go == null) continue;
                    var actor = go.GetComponent<DiceActor>();
                    if (actor != null) _envReg?.Remove(actor);
                    Object.Destroy(go);
                }
                _spawnedDice.Clear();
            }

            private async Task WaitAnyInputAsync()
            {
                while (!_resolved)
                {
                    if ((Keyboard.current?.anyKey.wasPressedThisFrame == true) ||
                        (Mouse.current?.leftButton.wasPressedThisFrame == true) ||
                        (Mouse.current?.rightButton.wasPressedThisFrame == true) ||
                        (Touchscreen.current?.primaryTouch.press.wasPressedThisFrame == true))
                    {
                        return;
                    }
                    await Task.Yield();
                }
            }

            private Vector3 GetBossPosition()
            {
                var fallback = _arenaView != null ? _arenaView.transform.position : Vector3.zero;
                var controller = _boss as Logic.Scripts.GameDomain.MVC.Boss.BossController;
                if (controller != null)
                {
                    try { var t = controller.GetReferenceTransform(); if (t != null) return t.position; } catch { }
                }
                return fallback;
            }

            private Vector3 GetPlayerPosition()
            {
                var fallback = _arenaView != null ? _arenaView.transform.position : Vector3.zero;
                if (_player != null)
                {
                    try
                    {
                        var go = _player.NaraViewGO;
                        if (go != null) { var p = go.transform.position; p.y += 2f; return p; }
                    }
                    catch { }
                }
                fallback.y += 2f;
                return fallback;
            }

            private static int Sum(List<int> values)
            {
                int sum = 0;
                for (int i = 0; i < values.Count; i++) sum += values[i];
                return sum;
            }
        }
    }
}
