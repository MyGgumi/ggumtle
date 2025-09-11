using System;
using UnityEngine;

/// <summary>
/// 먹이 전용 인벤토리 시스템
/// 일반 인벤토리와 별도로 관리되며, 먹이 아이템만 보관
/// </summary>
public class FeedingInventory : MonoBehaviour
{
    public static FeedingInventory Instance;

    [Header("먹이 인벤토리 설정")]
    [SerializeField]
    private int currentMushrooms = 0; // 현재 보유한 버섯 개수
    
    [Header("디버그")]
    [SerializeField]
    private bool showDebugLogs = true;

    // 먹이 수량 변경 이벤트
    public event Action<int> OnFeedingInventoryChanged; // 현재 버섯 개수를 매개변수로 전달
    public event Action<string> OnFeedingMessage; // 메시지 표시용

    void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (showDebugLogs)
            Debug.Log($"[FeedingInventory] 먹이 인벤토리 초기화 완료 - 현재 버섯: {currentMushrooms}개");
    }

    /// <summary>
    /// 버섯 추가 - ResourceManager에도 Light로 추가
    /// </summary>
    public int AddMushrooms(int amount)
    {
        if (amount <= 0) return 0;

        int oldAmount = currentMushrooms;
        currentMushrooms += amount;

        // ResourceManager UI 업데이트 (데이터는 여기서만 관리)
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.UpdateLightUI(currentMushrooms);
            if (showDebugLogs)
                Debug.Log($"[FeedingInventory] ResourceManager UI를 {currentMushrooms}개로 업데이트");
        }
        else if (showDebugLogs)
        {
            Debug.LogWarning("[FeedingInventory] ResourceManager.Instance가 null입니다!");
        }

        OnFeedingInventoryChanged?.Invoke(currentMushrooms);
        OnFeedingMessage?.Invoke($"버섯 {amount}개 획득! (총 {currentMushrooms}개)");

        if (showDebugLogs)
            Debug.Log($"[FeedingInventory] 버섯 추가: {oldAmount} + {amount} = {currentMushrooms}");

        return amount;
    }

    /// <summary>
    /// 버섯 제거 시도
    /// </summary>
    public bool RemoveMushrooms(int amount)
    {
        if (amount <= 0) return false;

        if (currentMushrooms >= amount)
        {
            int oldAmount = currentMushrooms;
            currentMushrooms -= amount;

            // ResourceManager UI 업데이트
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.UpdateLightUI(currentMushrooms);
                if (showDebugLogs)
                    Debug.Log($"[FeedingInventory] ResourceManager UI를 {currentMushrooms}개로 업데이트");
            }

            OnFeedingInventoryChanged?.Invoke(currentMushrooms);

            if (showDebugLogs)
                Debug.Log($"[FeedingInventory] 버섯 사용: {oldAmount} - {amount} = {currentMushrooms}");

            return true;
        }

        if (showDebugLogs)
            Debug.LogWarning($"[FeedingInventory] 버섯 부족: 현재 {currentMushrooms}개, 요청 {amount}개");

        return false;
    }

    /// <summary>
    /// 현재 버섯 개수 조회
    /// </summary>
    public int GetMushroomCount()
    {
        return currentMushrooms;
    }

    /// <summary>
    /// 버섯이 충분한지 확인
    /// </summary>
    public bool HasEnoughMushrooms(int requiredAmount)
    {
        return currentMushrooms >= requiredAmount;
    }

    /// <summary>
    /// 먹이 인벤토리 비우기
    /// </summary>
    public void ClearAll()
    {
        int oldAmount = currentMushrooms;
        currentMushrooms = 0;

        OnFeedingInventoryChanged?.Invoke(currentMushrooms);
        OnFeedingMessage?.Invoke("먹이 인벤토리를 모두 비웠습니다.");

        if (showDebugLogs)
            Debug.Log($"[FeedingInventory] 먹이 인벤토리 초기화: {oldAmount} -> 0");
    }

    /// <summary>
    /// 디버그: 먹이 인벤토리 상태 출력
    /// </summary>
    [ContextMenu("Print Feeding Inventory Status")]
    public void PrintFeedingInventoryStatus()
    {
        Debug.Log($"=== Feeding Inventory Status ===");
        Debug.Log($"버섯: {currentMushrooms}개");
    }

    /// <summary>
    /// 테스트용: 버섯 추가
    /// </summary>
    [ContextMenu("Add Test Mushrooms (10)")]
    public void AddTestMushrooms()
    {
        AddMushrooms(10);
    }
}