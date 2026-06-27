using Logic.Scripts.GameDomain.MVC.Abilitys;

using Logic.Scripts.GameDomain.MVC.Nara;

using Logic.Scripts.GameDomain.MVC.Ui;

using Logic.Scripts.GameDomain.Services.ActiveUnit;

using Logic.Scripts.GameDomain.Services.Cheats;

using Logic.Scripts.GameDomain.Services.Skills;

using Logic.Scripts.Services.AudioService;

using Logic.Scripts.Services.CommandFactory;

using Logic.Scripts.Services.UpdateService;

using UnityEngine;

using Zenject;



namespace Logic.Scripts.GameDomain.MVC.Book.Divide

{

    public class DivideAbilityHandler : IDivideAbilityHandler

    {

        private readonly IBookController _bookController;

        private readonly INaraController _naraController;

        private readonly IActiveUnitService _activeUnitService;

        private readonly AbilityData _divideTargetingData;

        private readonly DivideAbilityVfxConfigSO _divideAbilityVfxConfig;

        private readonly IUpdateSubscriptionService _updateSubscriptionService;

        private readonly ICommandFactory _commandFactory;

        private readonly IAudioService _audioService;

        private readonly IGamePlayUiController _gamePlayUiController;

        private readonly LoadoutCheatGameplayService _loadoutCheatGameplayService;



        private bool _divideCommandUsedThisTurn;

        private bool _isAiming;

        private bool _targetingSetUp;



        public bool IsBookDeployed => _bookController.IsDeployed;

        public bool IsAiming => _isAiming;

        public bool CanUseDivideCommandThisTurn => !_divideCommandUsedThisTurn;



        public DivideAbilityHandler(

            IBookController bookController,

            INaraController naraController,

            IActiveUnitService activeUnitService,

            IUpdateSubscriptionService updateSubscriptionService,

            ICommandFactory commandFactory,

            AbilityData divideTargetingData,

            IAudioService audioService,

            [InjectOptional] DivideAbilityVfxConfigSO divideAbilityVfxConfig = null,

            [InjectOptional] IGamePlayUiController gamePlayUiController = null,

            [InjectOptional] LoadoutCheatGameplayService loadoutCheatGameplayService = null)

        {

            _bookController = bookController;

            _naraController = naraController;

            _activeUnitService = activeUnitService;

            _updateSubscriptionService = updateSubscriptionService;

            _commandFactory = commandFactory;

            _divideTargetingData = divideTargetingData;

            _divideAbilityVfxConfig = divideAbilityVfxConfig;

            _audioService = audioService;

            _gamePlayUiController = gamePlayUiController;

            _loadoutCheatGameplayService = loadoutCheatGameplayService;

        }



        public void Activate()

        {

            if (_divideCommandUsedThisTurn)

                return;



            if (IsBookDeployed) {

                RecallBook();

            } else if (_isAiming) {

                return;

            } else {

                StartAiming();

            }

        }



        public void ConfirmPlacement()

        {

            if (!_isAiming) return;

            if (_divideTargetingData == null) return;

            if (_divideCommandUsedThisTurn) return;



            IEffectable[] targets;

            Vector3 spawnPos = _divideTargetingData.TargetingStrategy.LockAim(out targets);



            _isAiming = false;

            SpawnTransientVfx(_divideAbilityVfxConfig?.ConfirmSpawnVfx, spawnPos);

            DeployBook(spawnPos);

        }



        public void CancelAim()

        {

            if (!_isAiming) return;



            var strategy = _divideTargetingData?.TargetingStrategy;

            Vector3 previewPosition = default;

            var hadPreviewPosition = strategy != null && strategy.TryGetAimPreviewPosition(out previewPosition);



            _divideTargetingData?.Cancel();

            _isAiming = false;



            if (hadPreviewPosition)

                SpawnTransientVfx(_divideAbilityVfxConfig?.CancelAimVfx, previewPosition);

        }



        public void OnPlayerTurnStart()

        {

            _divideCommandUsedThisTurn = false;



            _activeUnitService?.SetNaraAsActiveUnit();



            if (IsBookDeployed)

            {

                _bookController.GainTurnActionPoints();

                _loadoutCheatGameplayService?.ApplyBookTurnStart(_bookController);

                _bookController.ResetMovementArea();

            }



            SyncDivideKeybindHud();

        }



        public void OnPlayerTurnEnd()

        {

            _activeUnitService?.SetNaraAsActiveUnit();



            if (_isAiming) CancelAim();

        }



        private void StartAiming()

        {

            if (_divideTargetingData == null)

            {

                Debug.LogWarning("[DivideAbility] No targeting data assigned. Assign a PointTargetingAbilityData in the GamePlayInstaller.");

                return;

            }



            if (!_targetingSetUp)

            {

                _divideTargetingData.SetUp(_updateSubscriptionService, _commandFactory);

                _targetingSetUp = true;

            }



            _isAiming = true;

            _divideTargetingData.Aim(_naraController);

        }



        private void DeployBook(Vector3 position)

        {

            _bookController.CreateBook(position);

            _naraController.SetBookCloneDeployed(true);

            _audioService?.PlaySfx(SfxIds.Ezra_Clone, AudioChannelType.SfxCombat);

            _bookController.GainTurnActionPoints();

            _activeUnitService.RegisterBook(_bookController);

            MarkDivideCommandUsed();

        }



        private void RecallBook()

        {

            _activeUnitService.SetNaraAsActiveUnit();

            _activeUnitService.UnregisterBook();

            _bookController.DestroyBook();

            _naraController.SetBookCloneDeployed(false);

            MarkDivideCommandUsed();

        }



        void MarkDivideCommandUsed()

        {

            _divideCommandUsedThisTurn = true;

            SyncDivideKeybindHud();

        }



        void SyncDivideKeybindHud() =>
            _gamePlayUiController?.SyncDivideKeybindHud(IsBookDeployed, CanUseDivideCommandThisTurn);



        static void SpawnTransientVfx(GameObject prefab, Vector3 position)

        {

            if (prefab == null)

                return;



            SkillCastVfxUtility.TrySpawnTransiient(prefab, position, Quaternion.identity);

        }

    }

}


