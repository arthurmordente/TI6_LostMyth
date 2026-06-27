using Logic.Scripts.GameDomain.GameInitiator;
using Logic.Scripts.GameDomain.MVC.Abilitys;
using Logic.Scripts.GameDomain.Services.Cheats;
using Logic.Scripts.GameDomain.Services.Skills;
using Logic.Scripts.GameDomain.States;
using System.Collections.Generic;
using Zenject;
using Logic.Scripts.Services.AudioService;
using UnityEngine;
using UnityEngine.Serialization;

namespace Logic.Scripts.GameDomain.ZenjectInstallers {
    public class GameInstaller : MonoInstaller {
        public List<AbilityData> Abilities;
        public AbilityPointData PointData;

        [SerializeField] private MusicClipsScriptableObject _gameplayMusicClips;
        [SerializeField] private SfxClipsScriptableObject _gameplaySfxClips;
        [SerializeField] private UniversalUiSceneViews _universalUiSceneViews;

        [Header("New Skill System — catálogo global")]
        [Tooltip("Uma única lista de SkillDataSO para todo o jogo. Exploração e Luta usam este serviço via Zenject (parent GameScene → CoreScene).")]
        [FormerlySerializedAs("_paschoalSkillCatalog")]
        [SerializeField] private SkillDataSO[] _newSkillSystemSkillCatalog;

        [Header("Skill visuals — divindade × tipo")]
        [SerializeField] private SkillVisualCatalogSO _skillVisualCatalog;

        [Header("Loadout cheats")]
        [SerializeField] private CheatDataSO[] _loadoutCheatCatalog;

        public override void InstallBindings() {
            Container.Bind<IGameInitiator>().To<GameInitiator.GameInitiator>().AsSingle().NonLazy();
            Container.BindInterfacesTo<CheatController>().AsSingle().NonLazy();
            Container.BindInterfacesTo<AbilityPointService>().AsSingle().WithArguments(Abilities, PointData).NonLazy();
            Container.BindFactory<GamePlayInitatorEnterData, GamePlayState, GamePlayState.Factory>();
            Container.BindFactory<ExplorationInitiatorEnterData, ExplorationState, ExplorationState.Factory>();
            Container.BindInterfacesTo<LevelsDataService>().AsSingle().NonLazy();
            Container.BindFactory<LobbyInitiatorEnterData, LobbyState, LobbyState.Factory>().AsSingle().NonLazy();

            BindUniversalUi();

            Container.Bind<INewSkillSystemSkillLoadoutService>().To<NewSkillSystemSkillLoadoutService>().AsSingle()
                .WithArguments(_newSkillSystemSkillCatalog, 4).NonLazy();

            Container.Bind<ILoadoutCheatService>().To<LoadoutCheatService>().AsSingle()
                .WithArguments(_loadoutCheatCatalog ?? System.Array.Empty<CheatDataSO>()).NonLazy();
            Container.Bind<LoadoutCheatGameplayService>().AsSingle().NonLazy();

            if (_skillVisualCatalog != null)
                Container.Bind<ISkillVisualCatalog>().To<SkillVisualCatalogService>().AsSingle()
                    .WithArguments(_skillVisualCatalog).NonLazy();
            else
                Container.Bind<ISkillVisualCatalog>().FromInstance(NullSkillVisualCatalog.Instance).AsSingle();

            Container.Bind<IAudioService>()
                .To<AudioService>()
                .FromComponentInHierarchy()
                .AsSingle()
                .IfNotBound();

            if (_gameplayMusicClips != null)
                Container.BindInstance(_gameplayMusicClips).WhenInjectedInto<GameInitiator.GameInitiator>();
            if (_gameplaySfxClips != null)
                Container.BindInstance(_gameplaySfxClips).WhenInjectedInto<GameInitiator.GameInitiator>();
        }

        private void BindUniversalUi() {
            if (_universalUiSceneViews == null)
                _universalUiSceneViews = GetComponent<UniversalUiSceneViews>();

            if (_universalUiSceneViews == null) {
                var go = new GameObject(nameof(UniversalUiSceneViews));
                go.transform.SetParent(transform);
                _universalUiSceneViews = go.AddComponent<UniversalUiSceneViews>();
            }

            _universalUiSceneViews.EnsureViews();
            Container.Bind<UniversalUiSceneViews>().FromInstance(_universalUiSceneViews).AsSingle();

            if (_universalUiSceneViews.Options != null)
                Container.Bind<IOptionsView>().FromInstance(_universalUiSceneViews.Options).AsSingle();
            if (_universalUiSceneViews.Credits != null)
                Container.Bind<ICreditsView>().FromInstance(_universalUiSceneViews.Credits).AsSingle();
            if (_universalUiSceneViews.Load != null)
                Container.Bind<ILoadScreenView>().FromInstance(_universalUiSceneViews.Load).AsSingle();
            if (_universalUiSceneViews.Guide != null)
                Container.Bind<IGuideScreenView>().FromInstance(_universalUiSceneViews.Guide).AsSingle();
            if (_universalUiSceneViews.Cheats != null)
                Container.Bind<ICheatsScreenView>().FromInstance(_universalUiSceneViews.Cheats).AsSingle();

            Container.BindInterfacesTo<UniversalUIController>().AsSingle().NonLazy();
        }
    }
}
