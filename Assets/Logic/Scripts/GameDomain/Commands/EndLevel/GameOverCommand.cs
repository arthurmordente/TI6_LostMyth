using Logic.Scripts.GameDomain.GameInputActions;
using Logic.Scripts.GameDomain.MVC.Ui;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.Utils;
using UnityEngine;

public class GameOverCommand : BaseCommand, ICommandVoid {
    public const float GameOverFadeSeconds = 1f;

    static bool _sequenceActive;

    private IGamePlayUiController _gamePlayUiController;
    private IGameInputActionsController _gameInputActionsController;
    private IAudioService _audioService;

    private GameOverCommandData _data;

    public static void ResetSequenceGuard() => _sequenceActive = false;

    public GameOverCommand SetData(GameOverCommandData data) {
        _data = data;
        return this;
    }

    public override void ResolveDependencies() {
        _gamePlayUiController = _diContainer.Resolve<IGamePlayUiController>();
        _gameInputActionsController = _diContainer.Resolve<IGameInputActionsController>();
        try { _audioService = _diContainer.Resolve<IAudioService>(); } catch { _audioService = null; }
    }

    public void Execute() {
        if (_sequenceActive) return;
        _sequenceActive = true;
        _gameInputActionsController.UnregisterGameplayInputListeners();
        RunEndSequence();
    }

    async Awaitable RunEndSequence() {
        try {
            if (_data == null) return;

            if (_data.DeathAnimator != null)
                await DeathAnimationAwaiter.WaitUntilComplete(_data.DeathAnimator);

            GeneralSfxFeedback.PlayGameOverStinger(_audioService, _data.IsWin);
            await _gamePlayUiController.ShowGameOverWithFadeAsync(_data.IsWin, GameOverFadeSeconds);
            Time.timeScale = 0f;
        }
        catch {
            Time.timeScale = 0f;
            if (_data != null)
                _gamePlayUiController.ShowGameOver(_data.IsWin);
        }
    }
}
