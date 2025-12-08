using DG.Tweening;
using System.Threading;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;
using Logic.Scripts.Turns;
using Logic.Scripts.Services.CommandFactory;
using Logic.Scripts.GameDomain.Commands;
using Unity.VisualScripting;

namespace Logic.Scripts.GameDomain.MVC.Ui {
    public class GamePlayUiView : MonoBehaviour {

        [SerializeField] private GamePlayUiBindSO _gamePlayUiBindSO;
        [SerializeField] private float tweenDuration = 0.5f;
        private VisualElement _mainContainer;

        private VisualElement _cooldownSlot1Container;
        private VisualElement _cooldownSlot2Container;
        private VisualElement _cooldownSlot3Container;
        private VisualElement _cooldownSlot4Container;
        private VisualElement _cooldownSlot5Container;
        private VisualElement _cooldownClone1Container;
        private VisualElement _cooldownClone2Container;

        private Button _setSkillSet1Btn;
        private Button _setSkillSet2Btn;
        private Button _setSkillSet3Btn;
        private Button _useSkill1Btn;
        private Button _useSkill2Btn;
        private Button _useSkill3Btn;

        private Button _nextTurnBtn;

        private ITurnQuery _turnQuery;
        private ICommandFactory _commandFactory;
        private int _cachedAp;

        #region AuxMethods
        void TweenLength(System.Func<Length> getter, System.Action<Length> setter, int newValue) {
            DOTween.To(() => getter().value,
                       x => setter(new Length(x, getter().unit)),
                       newValue,
                       tweenDuration)
                   .SetOptions(false)
                   .SetTarget(_gamePlayUiBindSO);
        }

        void TweenInt(System.Func<int> getter, System.Action<int> setter, int newValue) {
            DOTween.To(() => (float)getter(),
                       x => setter(Mathf.RoundToInt(x)),
                       newValue,
                       tweenDuration)
                   .SetTarget(_gamePlayUiBindSO);
        }

        void TweenString(System.Func<string> getter, System.Action<string> setter, string newValue) {
            DOTween.To(() => getter(),
                       x => setter(x),
                       newValue,
                       tweenDuration)
                   .SetTarget(_gamePlayUiBindSO);
        }
        #endregion

        #region UpdateUiMethods
        public void OnActualBossHealthChange(int newValue) =>
            TweenLength(() => _gamePlayUiBindSO.ActualBosshealthPercent, v => _gamePlayUiBindSO.ActualBosshealthPercent = v, newValue);

        public void OnPreviewBossHealthChange(int newValue) =>
            TweenLength(() => _gamePlayUiBindSO.PreviewBossHealthPercent, v => _gamePlayUiBindSO.PreviewBossHealthPercent = v, newValue);

        public void OnActualBossLifeChange(int newValue) =>
            TweenInt(() => _gamePlayUiBindSO.ActualBossLife, v => _gamePlayUiBindSO.ActualBossLife = v, newValue);


        public void OnActualPlayerLifePercentChange(int newValue) =>
            TweenLength(() => _gamePlayUiBindSO.ActualPlayerLifePercent, v => _gamePlayUiBindSO.ActualPlayerLifePercent = v, newValue);

        public void OnPreviewPlayerLifePercentChange(int newValue) =>
            TweenLength(() => _gamePlayUiBindSO.PreviewPlayerLifePercent, v => _gamePlayUiBindSO.PreviewPlayerLifePercent = v, newValue);

        public void OnActualPlayerHealthChange(int newValue) =>
            TweenInt(() => _gamePlayUiBindSO.ActualPlayerHealth, v => _gamePlayUiBindSO.ActualPlayerHealth = v, newValue);

        public void OnPlayerActionPointsChange(int newValue) =>
            TweenInt(() => _gamePlayUiBindSO.PlayerActionPoints, v => _gamePlayUiBindSO.PlayerActionPoints = v, newValue);


        public void OnSkill1CostChange(int newValue) =>
            TweenInt(() => _gamePlayUiBindSO.Skill1Cost, v => _gamePlayUiBindSO.Skill1Cost = v, newValue);

        public void OnSkill2CostChange(int newValue) =>
            TweenInt(() => _gamePlayUiBindSO.Skill2Cost, v => _gamePlayUiBindSO.Skill2Cost = v, newValue);

        public void OnSkill3CostChange(int newValue) =>
            TweenInt(() => _gamePlayUiBindSO.Skill3Cost, v => _gamePlayUiBindSO.Skill3Cost = v, newValue);


        public void OnSkill1NameChange(string newValue) =>
            TweenString(() => _gamePlayUiBindSO.Skill1Name, v => _gamePlayUiBindSO.Skill1Name = v, newValue);

        public void OnSkill2NameChange(string newValue) =>
            TweenString(() => _gamePlayUiBindSO.Skill2Name, v => _gamePlayUiBindSO.Skill2Name = v, newValue);

        public void OnSkill3NameChange(string newValue) =>
            TweenString(() => _gamePlayUiBindSO.Skill3Name, v => _gamePlayUiBindSO.Skill3Name = v, newValue);
        #endregion


        public void InitStartPoint() {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _mainContainer = root.Q<VisualElement>("main-container");

            _cooldownSlot1Container = root.Q<VisualElement>("Ability-Slot1-cooldown-container");
            _cooldownSlot2Container = root.Q<VisualElement>("Ability-Slot2-cooldown-container");
            _cooldownSlot3Container = root.Q<VisualElement>("Ability-Slot3-cooldown-container");
            _cooldownSlot4Container = root.Q<VisualElement>("Ability-Slot4-cooldown-container");
            _cooldownSlot5Container = root.Q<VisualElement>("Ability-Slot5-cooldown-container");
            _cooldownClone1Container = root.Q<VisualElement>("Clone-Slot1-Cooldown-label");
            _cooldownClone2Container = root.Q<VisualElement>("Clone-Slot2-Cooldown-label");
            for (int i = 1; i <= 7; i++) {
                SetCooldownContainer(i, true);
            }

            _setSkillSet1Btn = root.Q<Button>("Skill-Set1-btn");
            _setSkillSet2Btn = root.Q<Button>("Skill-Set2-btn");
            _setSkillSet3Btn = root.Q<Button>("Skill-Set3-btn");
            _useSkill1Btn = root.Q<Button>("Ability-Slot1-btn");
            _useSkill2Btn = root.Q<Button>("Ability-Slot2-btn");
            _useSkill3Btn = root.Q<Button>("Ability-Slot3-btn");

            _nextTurnBtn = root.Q<Button>("Next-Turn-btn");

            //if (_nextTurnBtn != null) {
            //    _nextTurnBtn.clicked += () => _commandFactory.CreateCommandVoid<CompletePlayerActionCommand>().Execute();
            //}
        }

        public VisualElement GetMainContainer() {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            _mainContainer = root.Q<VisualElement>("main-container");
            return _mainContainer;
        }

        public void SetCooldownContainer(int slotIndex, bool isHidden) {
            switch (slotIndex) {
                case 1:
                    if (isHidden) _cooldownSlot1Container.style.visibility = Visibility.Hidden;
                    else _cooldownSlot1Container.style.visibility = Visibility.Visible;
                    break;
                case 2:
                    if (isHidden) _cooldownSlot2Container.style.visibility = Visibility.Hidden;
                    else _cooldownSlot2Container.style.visibility = Visibility.Visible;
                    break;
                case 3:
                    if (isHidden) _cooldownSlot3Container.style.visibility = Visibility.Hidden;
                    else _cooldownSlot3Container.style.visibility = Visibility.Visible;
                    break;
                case 4:
                    if (isHidden) _cooldownSlot4Container.style.visibility = Visibility.Hidden;
                    else _cooldownSlot4Container.style.visibility = Visibility.Visible;
                    break;
                case 5:
                    if (isHidden) _cooldownSlot5Container.style.visibility = Visibility.Hidden;
                    else _cooldownSlot5Container.style.visibility = Visibility.Visible;
                    break;
                case 6:
                    if (isHidden) _cooldownClone1Container.style.visibility = Visibility.Hidden;
                    else _cooldownClone1Container.style.visibility = Visibility.Visible;
                    break;
                case 7:
                    if (isHidden) _cooldownClone2Container.style.visibility = Visibility.Hidden;
                    else _cooldownClone2Container.style.visibility = Visibility.Visible;
                    break;
            }
        }

        public void SetStartingValues(CancellationTokenSource cancellationTokenSource) {

        }

        public void InitExitPoint() {

        }

        [Inject]
        public void Construct(ITurnQuery turnQuery, ICommandFactory commandFactory) {
            _turnQuery = turnQuery;
            _commandFactory = commandFactory;
            _cachedAp = -1;
        }

        private void Update() {
            if (_turnQuery == null) return;
            int ap = _turnQuery.ActionPointsCurrent;
            if (ap != _cachedAp) {
                _cachedAp = ap;
                OnPlayerActionPointsChange(ap);
            }
        }
    }

}