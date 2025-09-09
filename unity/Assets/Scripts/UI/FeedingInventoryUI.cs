using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 먹이 인벤토리 전용 UI 관리
/// Mushroom 개수만 표시하는 간단한 UI
/// </summary>
public class FeedingInventoryUI : MonoBehaviour
{
    [Header("먹이 인벤토리 UI")]
    public Text mushroomCountText;

    private int displayedCount = 0;

    void Start()
    {
        InitializeUI();
        SubscribeToFeedingInventory();
    }

    void OnDestroy()
    {
        UnsubscribeFromFeedingInventory();
    }


    /// <summary>
    /// UI 초기화
    /// </summary>
    private void InitializeUI()
    {

        // 초기 개수 표시
        UpdateMushroomCount(0);

        Debug.Log("[FeedingInventoryUI] 먹이 인벤토리 UI 초기화 완료");
    }

    /// <summary>
    /// FeedingInventory 이벤트 구독
    /// </summary>
    private void SubscribeToFeedingInventory()
    {
        if (FeedingInventory.Instance != null)
        {
            FeedingInventory.Instance.OnFeedingInventoryChanged += OnMushroomCountChanged;

            // 초기 개수 동기화
            UpdateMushroomCount(FeedingInventory.Instance.GetMushroomCount());
        }
        else
        {
            Debug.LogWarning("[FeedingInventoryUI] FeedingInventory.Instance가 null입니다!");
        }
    }

    /// <summary>
    /// FeedingInventory 이벤트 구독 해제
    /// </summary>
    private void UnsubscribeFromFeedingInventory()
    {
        if (FeedingInventory.Instance != null)
        {
            FeedingInventory.Instance.OnFeedingInventoryChanged -= OnMushroomCountChanged;
        }
    }

    /// <summary>
    /// Mushroom 개수 변경 시 UI 업데이트
    /// </summary>
    private void OnMushroomCountChanged(int newCount)
    {
        UpdateMushroomCount(newCount);
    }

    /// <summary>
    /// Mushroom 개수 UI 업데이트
    /// </summary>
    private void UpdateMushroomCount(int count)
    {
        displayedCount = count;

        if (mushroomCountText != null)
        {
            mushroomCountText.text = $"🍄 {count}";
        }

        // 고정 색상
        if (mushroomCountText != null)
        {
            mushroomCountText.color = Color.black;
        }

        Debug.Log($"[FeedingInventoryUI] Mushroom 개수 UI 업데이트: {count}개");
    }

    /// <summary>
    /// 현재 표시된 Mushroom 개수 반환
    /// </summary>
    public int GetDisplayedMushroomCount()
    {
        return displayedCount;
    }

    /// <summary>
    /// 디버그: 현재 FeedingInventory 상태 출력
    /// </summary>
    [ContextMenu("Print Feeding Inventory Status")]
    public void PrintFeedingInventoryStatus()
    {
        if (FeedingInventory.Instance != null)
        {
            FeedingInventory.Instance.PrintFeedingInventoryStatus();
        }
        Debug.Log($"[FeedingInventoryUI] 표시된 개수: {displayedCount}개");
    }

    /// <summary>
    /// 테스트: Mushroom 추가
    /// </summary>
    [ContextMenu("Test Add Mushroom")]
    public void TestAddMushroom()
    {
        if (FeedingInventory.Instance != null)
        {
            FeedingInventory.Instance.AddMushrooms(5);
        }
    }

    /// <summary>
    /// 테스트: Mushroom 제거
    /// </summary>
    [ContextMenu("Test Remove Mushroom")]
    public void TestRemoveMushroom()
    {
        if (FeedingInventory.Instance != null)
        {
            FeedingInventory.Instance.RemoveMushrooms(3);
        }
    }
}