using Logic.Scripts.Core.CoreInitiator.Base;
using Logic.Scripts.Services.AudioService;
using Logic.Scripts.Services.InitiatorInvokerService;
using Logic.Scripts.Services.Logger.Base;
using Logic.Scripts.Services.SceneServices;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class SceneLoaderService : ISceneLoaderService {
    private readonly ISceneInitiatorsService _sceneInitiatorsService;
    private readonly IAudioService _audioService;
    private readonly HashSet<string> _loadedScenes = new();
    private readonly HashSet<string> _loadingScenes = new();

    [Inject]
    public SceneLoaderService(ISceneInitiatorsService sceneInitiatorsService, IAudioService audioService) {
        _sceneInitiatorsService = sceneInitiatorsService;
        _audioService = audioService;
    }

    public void InitEntryPoint() {
        AddOpenedScenesToLoadedHashset();
    }

    public async Awaitable<bool> TryLoadScene(string sceneName, CancellationTokenSource cancellationTokenSource) {
        bool isSceneAlreadyLoaded = _loadedScenes.Contains(sceneName);

        if (isSceneAlreadyLoaded) {
            LogService.LogError($"scene:{sceneName} is already Loaded");
            return false;
        }

        bool isSceneAlreadyLoading = _loadingScenes.Contains(sceneName);

        if (isSceneAlreadyLoading) {
            LogService.LogError($"scene:{sceneName} is already Loading");
            return false;
        }

        await LoadScene(sceneName, cancellationTokenSource);
        return true;
    }

    public async Awaitable<bool> TryLoadScene<TEnterData>(SceneType sceneType, TEnterData enterData, CancellationTokenSource cancellationTokenSource) where TEnterData : class, IInitiatorEnterData {
        if (!await TryLoadScene(sceneType.ToString(), cancellationTokenSource)) {
            return false;
        }
        await _sceneInitiatorsService.InvokeInitiatorLoadEntryPoint(sceneType, enterData, cancellationTokenSource);
        return true;
    }

    public async Awaitable StartScene<TEnterData>(SceneType sceneType, TEnterData enterData, CancellationTokenSource cancellationTokenSource) where TEnterData : class, IInitiatorEnterData {
        await _sceneInitiatorsService.InvokeInitiatorStartEntryPoint(sceneType, enterData, cancellationTokenSource);
    }

    public async Awaitable<bool> TryUnloadScene(SceneType sceneType, CancellationTokenSource cancellationTokenSource) {
        string sceneName = sceneType.ToString();
        bool isSceneAlreadyLoaded = _loadedScenes.Contains(sceneName);

        if (!isSceneAlreadyLoaded) {
            LogService.LogError($"scene:{sceneName} cant be unloaded as it is not Loaded");
            return false;
        }

        bool isSceneAlreadyLoading = _loadingScenes.Contains(sceneName);

        if (isSceneAlreadyLoading) {
            LogService.LogError($"scene:{sceneName} cant be unloaded as it during Loading");
            return false;
        }

        await UnloadScene(sceneType, cancellationTokenSource);
        return true;
    }

    private void AddOpenedScenesToLoadedHashset() {
        int countLoaded = SceneManager.sceneCount;

        for (int i = 0; i < countLoaded; i++) {
            string sceneName = SceneManager.GetSceneAt(i).name;

            if (!_loadedScenes.Contains(sceneName)) {
                _loadedScenes.Add(sceneName);
            }
        }
    }

    private async Awaitable LoadScene(string sceneName, CancellationTokenSource cancellationTokenSource) {
        _loadingScenes.Add(sceneName);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
        _audioService?.StopAllAudio();
        await SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        cancellationTokenSource.Token.ThrowIfCancellationRequested();
        _loadingScenes.Remove(sceneName);
        _loadedScenes.Add(sceneName);
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
    }

    private async Awaitable UnloadScene(SceneType sceneType, CancellationTokenSource cancellationTokenSource) {
        _audioService?.StopAllAudio();
        await _sceneInitiatorsService.InvokeInitiatorExitPoint(sceneType, cancellationTokenSource);
        string sceneName = sceneType.ToString();
        await SceneManager.UnloadSceneAsync(sceneName);
        _loadedScenes.Remove(sceneName);
    }
}
