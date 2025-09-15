// DEPRECATED: MVVM 패턴으로 리팩토링되어 ChestViewModel/ChestService로 대체됨
// 이 코드는 레거시 코드로 주석 처리됨

/*
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 상자의 아이템 상태를 관리하는 매니저
/// - 상자 ID별 아이템 리스트 관리
/// - 플레이어 아이템 획득/반납 처리
/// - 서버와의 상자 내용물 동기화
/// </summary>
public class ChestInventoryManager : MonoBehaviour
{
    public static ChestInventoryManager Instance;

    [Header("상자 관리 설정")]
    public int maxItemsPerChest = 10;
    public bool enableDebugLogs = true;

    [Header("디버그: 상자별 아이템 현황")]
    [SerializeField]
    private List<ChestInventoryDebugData> chestInventories = new List<ChestInventoryDebugData>();

    // 상자별 아이템 딕셔너리 (빠른 접근용)
    private Dictionary<string, List<ChestItem>> chestItemDict =
        new Dictionary<string, List<ChestItem>>();

    // 이벤트
    public event Action<string, List<ChestItem>> OnChestInventoryChanged;
    public event Action<string, ChestItem, int> OnItemRemovedFromChest;
    public event Action<string, ChestItem, int> OnItemAddedToChest;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeChestInventories();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 상자 인벤토리 초기화
    /// </summary>
    private void InitializeChestInventories()
    {
        chestItemDict = new Dictionary<string, List<ChestItem>>();
        Debug.Log("[ChestInventoryManager] 상자 인벤토리 매니저 초기화 완료");
    }

    /// <summary>
    /// 상자 등록 (상자가 처음 발견될 때)
    /// </summary>
    public void RegisterChest(string chestId)
    {
        if (!chestItemDict.ContainsKey(chestId))
        {
            chestItemDict[chestId] = new List<ChestItem>();
            UpdateDebugList();
            Debug.Log($"[ChestInventoryManager] 상자 등록: {chestId}");
        }
    }

    /// <summary>
    /// 상자 제거
    /// </summary>
    public void UnregisterChest(string chestId)
    {
        if (chestItemDict.ContainsKey(chestId))
        {
            chestItemDict.Remove(chestId);
            UpdateDebugList();
            Debug.Log($"[ChestInventoryManager] 상자 제거: {chestId}");
        }
    }

    /// <summary>
    /// 특정 상자의 아이템 리스트 조회
    /// </summary>
    public List<ChestItem> GetChestItems(string chestId)
    {
        if (chestItemDict.TryGetValue(chestId, out List<ChestItem> items))
        {
            // 복사본을 반환하여 외부에서 직접 수정하는 것을 방지
            return new List<ChestItem>(items);
        }

        // 상자가 없으면 자동으로 등록
        RegisterChest(chestId);
        return new List<ChestItem>();
    }

    /// <summary>
    /// 특정 상자에 아이템 추가
    /// </summary>
    public bool AddItemToChest(string chestId, ChestItem item)
    {
        if (item == null || item.quantity <= 0)
            return false;

        if (!chestItemDict.ContainsKey(chestId))
            RegisterChest(chestId);

        var chestItems = chestItemDict[chestId];

        // 상자가 가득 찼는지 확인
        if (chestItems.Count >= maxItemsPerChest)
        {
            Debug.LogWarning(
                $"[ChestInventoryManager] 상자 {chestId}가 가득 참 (최대 {maxItemsPerChest}개)"
            );
            return false;
        }

        // 같은 아이템이 있으면 스택 시도
        foreach (var existingItem in chestItems)
        {
            if (existingItem.itemName == item.itemName)
            {
                existingItem.quantity += item.quantity;
                OnItemAddedToChest?.Invoke(chestId, item, item.quantity);
                OnChestInventoryChanged?.Invoke(chestId, GetChestItems(chestId));
                UpdateDebugList();
                Debug.Log(
                    $"[ChestInventoryManager] {chestId}에 {item.itemName} {item.quantity}개 스택 추가"
                );
                return true;
            }
        }

        // 새로운 아이템 추가
        chestItems.Add(
            new ChestItem(item.itemName, item.itemIcon, item.quantity, item.description)
        );
        OnItemAddedToChest?.Invoke(chestId, item, item.quantity);
        OnChestInventoryChanged?.Invoke(chestId, GetChestItems(chestId));
        UpdateDebugList();
        Debug.Log(
            $"[ChestInventoryManager] {chestId}에 새 아이템 {item.itemName} {item.quantity}개 추가"
        );
        return true;
    }

    /// <summary>
    /// 특정 상자에서 아이템 제거
    /// </summary>
    public bool RemoveItemFromChest(string chestId, int itemIndex, int amount = 1)
    {
        if (!chestItemDict.ContainsKey(chestId))
            return false;

        var chestItems = chestItemDict[chestId];
        if (itemIndex < 0 || itemIndex >= chestItems.Count)
            return false;

        var item = chestItems[itemIndex];
        if (item.quantity < amount)
            amount = item.quantity;

        var removedItem = new ChestItem(item.itemName, item.itemIcon, amount, item.description);

        item.quantity -= amount;
        if (item.quantity <= 0)
        {
            chestItems.RemoveAt(itemIndex);
        }

        OnItemRemovedFromChest?.Invoke(chestId, removedItem, amount);
        OnChestInventoryChanged?.Invoke(chestId, GetChestItems(chestId));
        UpdateDebugList();
        Debug.Log($"[ChestInventoryManager] {chestId}에서 {item.itemName} {amount}개 제거");
        return true;
    }

    /// <summary>
    /// 플레이어가 상자에서 아이템을 가져갈 때 처리 (서버 중심)
    /// </summary>
    public bool TakeItemFromChest(string chestId, int itemIndex, int requestedAmount = 1)
    {
        if (!chestItemDict.ContainsKey(chestId))
        {
            Debug.LogError($"[ChestInventoryManager] 존재하지 않는 상자: {chestId}");
            return false;
        }

        var chestItems = chestItemDict[chestId];
        if (itemIndex < 0 || itemIndex >= chestItems.Count)
        {
            Debug.LogError($"[ChestInventoryManager] 잘못된 아이템 인덱스: {itemIndex}");
            return false;
        }

        var item = chestItems[itemIndex];
        int actualAmount = Mathf.Min(requestedAmount, item.quantity);

        // 실시간 멀티플레이어: 서버에 즉시 요청
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.RequestTakeItem(chestId, item.itemName, actualAmount);

            if (enableDebugLogs)
                Debug.Log(
                    $"[ChestInventoryManager] 서버에 아이템 획득 요청: {item.itemName} x{actualAmount}"
                );

            return true; // 요청 성공 (실제 결과는 서버 응답에서 처리)
        }
        else
        {
            Debug.LogError("[ChestInventoryManager] ServerSyncManager가 없습니다!");
            return false;
        }
    }

    /// <summary>
    /// 서버에서 상자 내용물 업데이트 수신
    /// </summary>
    public void UpdateChestFromServer(string chestId, List<ChestItem> serverItems)
    {
        if (!chestItemDict.ContainsKey(chestId))
            RegisterChest(chestId);

        // 기존 아이템 리스트를 서버 데이터로 교체
        chestItemDict[chestId] = new List<ChestItem>(serverItems);
        OnChestInventoryChanged?.Invoke(chestId, GetChestItems(chestId));
        UpdateDebugList();

        Debug.Log(
            $"[ChestInventoryManager] 서버로부터 상자 {chestId} 업데이트: {serverItems.Count}개 아이템"
        );
    }

    /// <summary>
    /// 특정 상자가 비어있는지 확인
    /// </summary>
    public bool IsChestEmpty(string chestId)
    {
        return GetChestItems(chestId).Count == 0;
    }

    /// <summary>
    /// 특정 상자의 아이템 개수 반환
    /// </summary>
    public int GetChestItemCount(string chestId)
    {
        return GetChestItems(chestId).Count;
    }

    /// <summary>
    /// 모든 상자 ID 리스트 반환
    /// </summary>
    public List<string> GetAllChestIds()
    {
        return new List<string>(chestItemDict.Keys);
    }

    /// <summary>
    /// Inspector 디버그 리스트 업데이트
    /// </summary>
    private void UpdateDebugList()
    {
        chestInventories.Clear();
        foreach (var kvp in chestItemDict)
        {
            var chestData = new ChestInventoryDebugData
            {
                chestId = kvp.Key,
                itemCount = kvp.Value.Count,
                items = new List<ChestItem>(kvp.Value),
            };
            chestInventories.Add(chestData);
        }
    }

    /// <summary>
    /// 디버그: 모든 상자 내용 출력
    /// </summary>
    [ContextMenu("Print All Chest Contents")]
    public void PrintAllChestContents()
    {
        Debug.Log("=== All Chest Contents ===");
        foreach (var kvp in chestItemDict)
        {
            Debug.Log($"상자 {kvp.Key}: {kvp.Value.Count}개 아이템");
            foreach (var item in kvp.Value)
            {
                Debug.Log($"  - {item.itemName} x{item.quantity}");
            }
        }
    }

    /// <summary>
    /// 디버그: 테스트 아이템 분배
    /// </summary>
    [ContextMenu("Distribute Test Items")]
    public void DistributeTestItems()
    {
        if (GlobalItemManager.Instance == null)
            return;

        var allItems = GlobalItemManager.Instance.GetAllItemNames();
        var allChests = GetAllChestIds();

        if (allChests.Count == 0)
        {
            // 테스트용 상자 생성
            RegisterChest("TestChest1");
            RegisterChest("TestChest2");
            allChests = GetAllChestIds();
        }

        foreach (string itemName in allItems)
        {
            for (int i = 0; i < allChests.Count; i++)
            {
                var chestItem = GlobalItemManager.Instance.CreateChestItem(
                    itemName,
                    UnityEngine.Random.Range(1, 5)
                );
                if (chestItem != null)
                {
                    AddItemToChest(allChests[i], chestItem);
                }
            }
        }
    }
}

/// <summary>
/// Inspector에서 상자별 아이템 현황을 확인하기 위한 클래스
/// </summary>
[System.Serializable]
public class ChestInventoryDebugData
{
    public string chestId;
    public int itemCount;
    public List<ChestItem> items = new List<ChestItem>();
}
*/