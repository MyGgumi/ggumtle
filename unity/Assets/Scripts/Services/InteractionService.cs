using System;
using UnityEngine;
using Models;

namespace Services
{
    public class InteractionService
    {
        private InteractionModel _interactionModel;

        public event Action<InteractionType, string, float> OnInteractionStarted;
        public event Action<float> OnInteractionProgress;
        public event Action<InteractionType> OnInteractionCompleted;
        public event Action OnInteractionCancelled;
        public event Action<InteractionType, string, GameObject> OnNearbyInteractionAdded;
        public event Action<InteractionType, GameObject> OnNearbyInteractionRemoved;

        public InteractionService(InteractionModel interactionModel)
        {
            _interactionModel = interactionModel;
        }

        #region Current Interaction Operations

        public void StartInteraction(InteractionType type, string text, float duration = 0f, bool requiresHold = false, GameObject targetObject = null, Vector3 targetPosition = default)
        {
            _interactionModel.SetCurrentInteraction(type, text, duration, requiresHold);

            if (targetObject != null)
                _interactionModel.currentInteraction.targetObject = targetObject;
            if (targetPosition != default)
                _interactionModel.currentInteraction.targetPosition = targetPosition;

            _interactionModel.StartCurrentInteraction();
            OnInteractionStarted?.Invoke(type, text, duration);
        }

        public void UpdateInteraction(float deltaTime)
        {
            if (_interactionModel.IsInteractionInProgress)
            {
                _interactionModel.UpdateCurrentInteraction(deltaTime);
                OnInteractionProgress?.Invoke(_interactionModel.CurrentProgress);

                if (_interactionModel.currentInteraction.IsCompleted)
                {
                    var completedType = _interactionModel.currentInteraction.type;
                    _interactionModel.CompleteCurrentInteraction();
                    OnInteractionCompleted?.Invoke(completedType);
                }
            }
        }

        public void CompleteInteraction()
        {
            if (_interactionModel.HasActiveInteraction)
            {
                var completedType = _interactionModel.currentInteraction.type;
                _interactionModel.CompleteCurrentInteraction();
                OnInteractionCompleted?.Invoke(completedType);
            }
        }

        public void CancelInteraction()
        {
            if (_interactionModel.HasActiveInteraction)
            {
                _interactionModel.CancelCurrentInteraction();
                OnInteractionCancelled?.Invoke();
            }
        }

        public void ClearInteraction()
        {
            _interactionModel.ClearCurrentInteraction();
        }

        public bool HasActiveInteraction => _interactionModel.HasActiveInteraction;
        public bool IsInteractionInProgress => _interactionModel.IsInteractionInProgress;
        public float CurrentProgress => _interactionModel.CurrentProgress;
        public InteractionType CurrentInteractionType => _interactionModel.currentInteraction.type;
        public string CurrentInteractionText => _interactionModel.currentInteraction.displayText;

        #endregion

        #region Nearby Interactions Operations

        public void AddNearbyInteraction(InteractionType type, string text, GameObject target = null)
        {
            _interactionModel.AddNearbyInteraction(type, text, target);
            OnNearbyInteractionAdded?.Invoke(type, text, target);
        }

        public void RemoveNearbyInteraction(InteractionType type, GameObject target = null)
        {
            _interactionModel.RemoveNearbyInteraction(type, target);
            OnNearbyInteractionRemoved?.Invoke(type, target);
        }

        public void ClearNearbyInteractions()
        {
            _interactionModel.ClearNearbyInteractions();
        }

        public InteractionData GetBestNearbyInteraction()
        {
            return _interactionModel.GetBestNearbyInteraction();
        }

        public bool HasNearbyInteractions => _interactionModel.HasNearbyInteractions;

        #endregion

        #region Settings

        public void SetPlayerMovement(bool canMove)
        {
            _interactionModel.SetPlayerMovement(canMove);
        }

        public void SetUIVisibility(bool visible)
        {
            _interactionModel.SetUIVisibility(visible);
        }

        public bool PlayerCanMove => _interactionModel.playerCanMove;
        public bool ShowUI => _interactionModel.showUI;
        public float DetectionRange => _interactionModel.detectionRange;

        #endregion

        #region Utility Methods

        public void Reset()
        {
            _interactionModel.Reset();
        }

        #endregion
    }
}