using System;
using UnityEngine;

/// <summary>
/// 모든 HUD 관련 이벤트들을 중앙집중식으로 관리하는 클래스
/// </summary>
public static class HUDEvents
{
    // ==== 게임 타임 이벤트 ====
    public static event Action<int> OnGgumtleProgressChanged;
    public static event Action<int> OnTimeWarning;
    public static event Action OnTimeUp;

    // ==== 리소스 이벤트 ====
    public static event Action<int> OnLightCountChanged;
    public static event Action<int, int> OnInventorySlotUsed;
    public static event Action<int, int> OnInventorySlotUpdated;

    // ==== 인벤토리 아이템 이벤트 ====
    public static event Action<string, int> OnItemObtained;
    public static event Action<string, int> OnItemUsed;
    public static event Action<int, int> OnSlotSwapped;

    // ==== 체력/상태 이벤트 ====
    public static event Action<int, int> OnHealthChanged;
    public static event Action<bool> OnFaintStateChanged;
    public static event Action OnPlayerDeath;
    public static event Action OnPlayerRevived;

    // ==== 상호작용 이벤트 ====
    public static event Action<InteractionType, float, bool> OnInteractionStarted;
    public static event Action<InteractionType, float> OnInteractionProgress;
    public static event Action<InteractionType, bool> OnInteractionCompleted;
    public static event Action OnInteractionCancelled;

    // ==== 플레이어 이벤트 ====
    public static event Action<int, string, string> OnPlayerStatusChanged;
    public static event Action<int, bool> OnPlayerConnectionChanged;

    // ==== 이벤트 트리거 메서드들 ====
    public static void TriggerGgumtleProgress(int level) => OnGgumtleProgressChanged?.Invoke(level);

    public static void TriggerTimeWarning(int minutes) => OnTimeWarning?.Invoke(minutes);

    public static void TriggerTimeUp() => OnTimeUp?.Invoke();

    public static void TriggerLightGain(int count) => OnLightCountChanged?.Invoke(count);

    public static void TriggerInventoryUse(int slot, int count) =>
        OnInventorySlotUsed?.Invoke(slot, count);

    public static void TriggerInventoryUpdate(int slot, int count) =>
        OnInventorySlotUpdated?.Invoke(slot, count);

    public static void TriggerItemObtained(string itemName, int quantity) =>
        OnItemObtained?.Invoke(itemName, quantity);

    public static void TriggerItemUsed(string itemName, int quantity) =>
        OnItemUsed?.Invoke(itemName, quantity);

    public static void TriggerSlotSwapped(int slot1, int slot2) =>
        OnSlotSwapped?.Invoke(slot1, slot2);

    public static void TriggerHealthChange(int current, int max) =>
        OnHealthChanged?.Invoke(current, max);

    public static void TriggerFaintState(bool isFainted) => OnFaintStateChanged?.Invoke(isFainted);

    public static void TriggerPlayerDeath() => OnPlayerDeath?.Invoke();

    public static void TriggerPlayerRevive() => OnPlayerRevived?.Invoke();

    // 직접 상호작용 트리거 (dig, revive, feeding 등)
    public static void TriggerInteractionFromInput(
        string interactionName,
        float duration,
        bool isCountdown
    )
    {
        // 문자열을 InteractionType으로 변환
        InteractionType type = interactionName.ToLower() switch
        {
            "dig" => InteractionType.Dig,
            "revive" => InteractionType.Revive,
            "feeding" => InteractionType.Feeding,
            "faint" => InteractionType.Faint,
            _ => InteractionType.Dig,
        };

        // 기존 이벤트 호출
        OnInteractionStarted?.Invoke(type, duration, isCountdown);

        Debug.Log(
            $"[HUDEvents] 상호작용 트리거: {interactionName} ({duration}초, 카운트다운: {isCountdown})"
        );
    }

    // 기절 이벤트용 (OnFaintStateChanged에서 호출됨)
    public static void TriggerFaintInteraction(float duration)
    {
        OnInteractionStarted?.Invoke(InteractionType.Faint, duration, true);
        Debug.Log($"[HUDEvents] 기절 상호작용 트리거: {duration}초");
    }

    // 기존 메서드들 (하위 호환성)
    public static void TriggerInteractionStart(
        InteractionType type,
        float duration,
        bool isCountdown = false
    ) => OnInteractionStarted?.Invoke(type, duration, isCountdown);

    public static void TriggerInteractionProgress(InteractionType type, float progress) =>
        OnInteractionProgress?.Invoke(type, progress);

    public static void TriggerInteractionComplete(InteractionType type, bool success) =>
        OnInteractionCompleted?.Invoke(type, success);

    public static void TriggerInteractionCancel() => OnInteractionCancelled?.Invoke();

    public static void TriggerPlayerStatusChange(int playerId, string status, string name) =>
        OnPlayerStatusChanged?.Invoke(playerId, status, name);

    public static void TriggerPlayerConnectionChange(int playerId, bool isOnline) =>
        OnPlayerConnectionChanged?.Invoke(playerId, isOnline);
}
