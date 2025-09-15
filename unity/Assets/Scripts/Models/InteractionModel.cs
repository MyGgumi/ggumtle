using System;
using UnityEngine;

namespace Models
{
    public enum InteractionType
    {
        None,
        Dig,
        Revive,
        Feeding,
        Faint,
        Chest,
        Custom
    }

    public enum InteractionState
    {
        None,
        Available,
        InProgress,
        Completed,
        Failed
    }

    [System.Serializable]
    public class InteractionData
    {
        public InteractionType type;
        public InteractionState state;
        public string displayText;
        public float progress;
        public float duration;
        public bool isCountdown;
        public bool requiresHold;
        public GameObject targetObject;
        public Vector3 targetPosition;

        public InteractionData()
        {
            type = InteractionType.None;
            state = InteractionState.None;
            displayText = "";
            progress = 0f;
            duration = 0f;
            isCountdown = false;
            requiresHold = false;
            targetObject = null;
            targetPosition = Vector3.zero;
        }

        public InteractionData(InteractionType type, string text, float duration = 0f, bool requiresHold = false)
        {
            this.type = type;
            this.state = InteractionState.Available;
            this.displayText = text;
            this.progress = 0f;
            this.duration = duration;
            this.isCountdown = false;
            this.requiresHold = requiresHold;
            this.targetObject = null;
            this.targetPosition = Vector3.zero;
        }

        public bool IsActive => state == InteractionState.Available || state == InteractionState.InProgress;
        public bool IsInProgress => state == InteractionState.InProgress;
        public bool IsCompleted => state == InteractionState.Completed;
        public bool HasDuration => duration > 0f;
        public float ProgressPercentage => HasDuration ? Mathf.Clamp01(progress / duration) : 0f;

        public void Reset()
        {
            type = InteractionType.None;
            state = InteractionState.None;
            displayText = "";
            progress = 0f;
            duration = 0f;
            isCountdown = false;
            requiresHold = false;
            targetObject = null;
            targetPosition = Vector3.zero;
        }

        public void Start()
        {
            if (state == InteractionState.Available)
            {
                state = InteractionState.InProgress;
                progress = 0f;
            }
        }

        public void UpdateProgress(float deltaTime)
        {
            if (state != InteractionState.InProgress || !HasDuration) return;

            progress += deltaTime;
            if (progress >= duration)
            {
                progress = duration;
                state = InteractionState.Completed;
            }
        }

        public void Complete()
        {
            state = InteractionState.Completed;
            progress = HasDuration ? duration : 1f;
        }

        public void Cancel()
        {
            if (state == InteractionState.InProgress)
            {
                state = InteractionState.Available;
                progress = 0f;
            }
        }

        public void Fail()
        {
            state = InteractionState.Failed;
        }
    }

    [System.Serializable]
    public class InteractionModel
    {
        [Header("Current Interaction")]
        public InteractionData currentInteraction = new InteractionData();

        [Header("Nearby Interactions")]
        public InteractionData[] nearbyInteractions = new InteractionData[5];

        [Header("Settings")]
        public float detectionRange = 3f;
        public bool playerCanMove = true;
        public bool showUI = false;

        public InteractionModel()
        {
            for (int i = 0; i < nearbyInteractions.Length; i++)
            {
                nearbyInteractions[i] = new InteractionData();
            }
        }

        #region Current Interaction Methods

        public void SetCurrentInteraction(InteractionType type, string text, float duration = 0f, bool requiresHold = false)
        {
            currentInteraction = new InteractionData(type, text, duration, requiresHold);
        }

        public void StartCurrentInteraction()
        {
            currentInteraction.Start();
        }

        public void UpdateCurrentInteraction(float deltaTime)
        {
            currentInteraction.UpdateProgress(deltaTime);
        }

        public void CompleteCurrentInteraction()
        {
            currentInteraction.Complete();
        }

        public void CancelCurrentInteraction()
        {
            currentInteraction.Cancel();
        }

        public void ClearCurrentInteraction()
        {
            currentInteraction.Reset();
        }

        public bool HasActiveInteraction => currentInteraction.IsActive;
        public bool IsInteractionInProgress => currentInteraction.IsInProgress;
        public float CurrentProgress => currentInteraction.ProgressPercentage;

        #endregion

        #region Nearby Interactions Methods

        public void AddNearbyInteraction(InteractionType type, string text, GameObject target = null)
        {
            for (int i = 0; i < nearbyInteractions.Length; i++)
            {
                if (nearbyInteractions[i].type == InteractionType.None)
                {
                    nearbyInteractions[i] = new InteractionData(type, text);
                    nearbyInteractions[i].targetObject = target;
                    break;
                }
            }
        }

        public void RemoveNearbyInteraction(InteractionType type, GameObject target = null)
        {
            for (int i = 0; i < nearbyInteractions.Length; i++)
            {
                if (nearbyInteractions[i].type == type &&
                    (target == null || nearbyInteractions[i].targetObject == target))
                {
                    nearbyInteractions[i].Reset();
                    break;
                }
            }
        }

        public void ClearNearbyInteractions()
        {
            for (int i = 0; i < nearbyInteractions.Length; i++)
            {
                nearbyInteractions[i].Reset();
            }
        }

        public InteractionData GetBestNearbyInteraction()
        {
            foreach (var interaction in nearbyInteractions)
            {
                if (interaction.IsActive)
                {
                    return interaction;
                }
            }
            return null;
        }

        public bool HasNearbyInteractions
        {
            get
            {
                foreach (var interaction in nearbyInteractions)
                {
                    if (interaction.IsActive) return true;
                }
                return false;
            }
        }

        #endregion

        #region Utility Methods

        public void Reset()
        {
            currentInteraction.Reset();
            ClearNearbyInteractions();
            detectionRange = 3f;
            playerCanMove = true;
            showUI = false;
        }

        public void SetPlayerMovement(bool canMove)
        {
            playerCanMove = canMove;
        }

        public void SetUIVisibility(bool visible)
        {
            showUI = visible;
        }

        #endregion
    }
}