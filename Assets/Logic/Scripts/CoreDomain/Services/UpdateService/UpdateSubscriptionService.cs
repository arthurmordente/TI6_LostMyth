using Logic.Scripts.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace Logic.Scripts.Services.UpdateService {
    public class UpdateSubscriptionService : MonoBehaviour, IUpdateSubscriptionService {
        private static readonly List<IUpdatable> _updateObservers = new List<IUpdatable>();

        private static readonly List<IFixedUpdatable> _fixedUpdateObservers = new List<IFixedUpdatable>();
        private static readonly List<IFixedUpdatable> _pendingAddFixedUpdateObservers = new List<IFixedUpdatable>();
        private static readonly List<IFixedUpdatable> _pendingRemoveFixedUpdateObservers = new List<IFixedUpdatable>();

        private static readonly List<ILateUpdatable> _lateUpdateObservers = new List<ILateUpdatable>();
        private static readonly List<ILateUpdatable> _pendingAddLateUpdateObservers = new List<ILateUpdatable>();
        private static readonly List<ILateUpdatable> _pendingRemoveLateUpdateObservers = new List<ILateUpdatable>();
        private static int _currentUpdateIndex;
        private void Update() {
            // Verbose frame logs disabled to reduce console noise during gameplay
            // Debug.Log("--------------Inicio Update-----------");
            for (_currentUpdateIndex = _updateObservers.Count - 1; _currentUpdateIndex >= 0; _currentUpdateIndex--) {
                var observer = _updateObservers[_currentUpdateIndex];
                // Debug.Log("Observer name: " + observer.ToString());
                observer.ManagedUpdate();
            }
            // Debug.Log("--------------Fim Update-----------");
        }

        private void LateUpdate() {
            // Debug.Log("--------------Inicio LateUpdate-----------");
            _lateUpdateObservers.AddRange(_pendingAddLateUpdateObservers);
            _pendingAddLateUpdateObservers.Clear();
            _lateUpdateObservers.RemoveElements(_pendingRemoveLateUpdateObservers);
            _pendingRemoveLateUpdateObservers.Clear();

            foreach (var observer in _lateUpdateObservers) {
                // Debug.Log("Observer name: " + observer.ToString());
                observer.ManagedLateUpdate();
            }
            // Debug.Log("--------------Fim LateUpdate-----------");
        }

        private void FixedUpdate() {
            // Debug.Log("--------------Inicio FixedUpdate-----------");
            _fixedUpdateObservers.AddRange(_pendingAddFixedUpdateObservers);
            _pendingAddFixedUpdateObservers.Clear();
            _fixedUpdateObservers.RemoveElements(_pendingRemoveFixedUpdateObservers);
            _pendingRemoveFixedUpdateObservers.Clear();

            foreach (var observer in _fixedUpdateObservers) {
                // Debug.Log("Observer name: " + observer.ToString());
                observer.ManagedFixedUpdate();
            }
            // Debug.Log("--------------Fim FixedUpdate-----------");
        }

        public void RegisterUpdatable(IUpdatable observer) {
            var isCurrentlyIterating = _currentUpdateIndex > 0;
            if (isCurrentlyIterating) {
                _updateObservers.Insert(0, observer);
                _currentUpdateIndex++;
            }
            else {
                _updateObservers.Add(observer);
            }
            Debug.LogWarning("Observer Updatable Register: " + observer.ToString());
        }

        public void UnregisterUpdatable(IUpdatable observer) {
            var isCurrentlyIterating = _currentUpdateIndex > 0;
            if (isCurrentlyIterating) {
                var indexOfObserver = _updateObservers.IndexOf(observer);
                _updateObservers.Remove(observer);

                var wasObserverAlreadyIteratedThisFrame = indexOfObserver >= _currentUpdateIndex;
                if (!wasObserverAlreadyIteratedThisFrame) {
                    _currentUpdateIndex--;
                }
            }
            else {
                _updateObservers.Remove(observer);
            }
            Debug.LogWarning("Observer Updatable Unregister: " + observer.ToString());
        }

        public void RegisterLateUpdatable(ILateUpdatable observer) {
            _pendingAddLateUpdateObservers.Add(observer);
            Debug.LogWarning("Observer LateUpdatable Register: " + observer.ToString());
        }

        public void UnregisterLateUpdatable(ILateUpdatable observer) {
            _pendingRemoveLateUpdateObservers.Add(observer);
            Debug.LogWarning("Observer LateUpdatable Unregister: " + observer.ToString());
        }

        public void RegisterFixedUpdatable(IFixedUpdatable updatable) {
            _pendingAddFixedUpdateObservers.Add(updatable);
            Debug.LogWarning("Observer FixedUpdatable Register: " + updatable.ToString());
        }

        public void UnregisterFixedUpdatable(IFixedUpdatable updatable) {
            _pendingRemoveFixedUpdateObservers.Add(updatable);
            Debug.LogWarning("Observer FixedUpdatable Unregister: " + updatable.ToString());
        }
    }
}