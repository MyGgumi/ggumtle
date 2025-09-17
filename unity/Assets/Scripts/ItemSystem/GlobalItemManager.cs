using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전역 아이템 수량 관리 시스템 (서버 동기화 전용)
/// - 서버로부터 수신한 전역 아이템 수량 추적
/// - 메타데이터는 ItemDatabase에서 처리
/// - 실시간 멀티플레이어 환경에 최적화
/// </summary>
public class GlobalItemManager : MonoBehaviour
{
    public static GlobalItemManager Instance;

    [Header("전역 아이템 수량 (디버그용)")]
    [SerializeField]
    private List<GlobalItemCount> globalItemCounts = new List<GlobalItemCount>();

    // 전역 아이템 수량 딕셔너리 (런타임에서 빠른 접근용)
    private Dictionary<string, int> itemCountDict = new Dictionary<string, int>();

    // 아이템 수량 변경 이벤트
    public event Action<string, int> OnGlobalItemCountChanged;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeItemCounts();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 전역 아이템 수량 초기화 (서버 동기화 기반)
    /// </summary>
    private void InitializeItemCounts()
    {
        // 실시간 멀티플레이어에서는 서버에서 초기 데이터를 받아옴
        // 로컬 초기화는 최소한으로만 처리
        itemCountDict = new Dictionary<string, int>();

        // Inspector에서 설정된 테스트 수량이 있으면 적용 (시뮬레이션용)
        foreach (var itemCount in globalItemCounts)
        {
            if (!string.IsNullOrEmpty(itemCount.itemName))
            {
                itemCountDict[itemCount.itemName] = itemCount.count;
            }
        }

        Debug.Log($"[GlobalItemManager] 전역 아이템 수량 초기화 (서버 동기화 대기 중)");

        // 서버에서 초기 데이터 요청
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.RequestFullSync();
        }
    }

    /// <summary>
    /// 특정 아이템의 전역 수량 조회
    /// </summary>
    public int GetGlobalItemCount(string itemName)
    {
        return itemCountDict.TryGetValue(itemName, out int count) ? count : 0;
    }

    /// <summary>
    /// 특정 아이템의 전역 수량 설정
    /// </summary>
    public void SetGlobalItemCount(string itemName, int count)
    {
        // 기존 가명을 실제 ID로 변환 (호환성)
        string actualItemName = Models.ItemTypeHelper.ConvertLegacyId(itemName);

        var database = FindItemDatabase();
        if (database != null)
        {
            // 먼저 변환된 이름으로 확인
            if (!database.HasItem(actualItemName))
            {
                // 원래 이름으로도 확인
                if (!database.HasItem(itemName))
                {
                    Debug.LogWarning(
                        $"[GlobalItemManager] 존재하지 않는 아이템: {itemName} (변환: {actualItemName})"
                    );
                    Debug.LogWarning(
                        $"[GlobalItemManager] ItemDatabase에 아이템을 등록해주세요: taser, flashbang, defibrillator, light"
                    );
                    // 경고만 하고 계속 진행 (테스트를 위해)
                }
                else
                {
                    actualItemName = itemName; // 원래 이름 사용
                }
            }
        }

        int oldCount = GetGlobalItemCount(actualItemName);
        itemCountDict[actualItemName] = Mathf.Max(0, count);

        // Inspector 디버그 리스트도 업데이트
        UpdateDebugList();

        // 이벤트 발생
        OnGlobalItemCountChanged?.Invoke(actualItemName, count);

        Debug.Log($"[GlobalItemManager] {actualItemName} 전역 수량 변경: {oldCount} → {count}");
    }

    /// <summary>
    /// 특정 아이템의 전역 수량 증가
    /// </summary>
    public void AddGlobalItemCount(string itemName, int amount)
    {
        if (amount <= 0)
            return;

        int currentCount = GetGlobalItemCount(itemName);
        SetGlobalItemCount(itemName, currentCount + amount);
    }

    /// <summary>
    /// 특정 아이템의 전역 수량 감소
    /// </summary>
    public bool RemoveGlobalItemCount(string itemName, int amount)
    {
        if (amount <= 0)
            return false;

        int currentCount = GetGlobalItemCount(itemName);
        if (currentCount >= amount)
        {
            SetGlobalItemCount(itemName, currentCount - amount);
            return true;
        }

        Debug.LogWarning(
            $"[GlobalItemManager] {itemName} 수량 부족: 현재 {currentCount}, 요청 {amount}"
        );
        return false;
    }

    /// <summary>
    /// 아이템 데이터 조회 (ItemDatabase에서 처리)
    /// </summary>
    public ItemData GetItemData(string itemName)
    {
        var database = FindItemDatabase();
        return database?.GetItemData(itemName);
    }

    /// <summary>
    /// ChestItem 생성 (ItemDatabase에서 처리)
    /// </summary>
    public ChestItem CreateChestItem(string itemName, int quantity = 1)
    {
        var database = FindItemDatabase();
        return database?.CreateChestItem(itemName, quantity);
    }

    /// <summary>
    /// InventoryItem 생성 (ItemDatabase에서 처리)
    /// </summary>
    public InventoryItem CreateInventoryItem(string itemName, int quantity = 1)
    {
        var database = FindItemDatabase();
        return database?.CreateInventoryItem(itemName, quantity);
    }

    /// <summary>
    /// ItemDatabase 찾기 (런타임에서 동적 검색)
    /// </summary>
    private ItemDatabase FindItemDatabase()
    {
        // Resources 폴더에서 ItemDatabase 찾기
        var database = Resources.Load<ItemDatabase>("ItemDatabase");
        if (database == null)
        {
            // 또는 씬에서 ItemDatabase를 가진 GameObject 찾기
            var databaseObject = FindFirstObjectByType<ItemDatabase>();
            if (databaseObject != null)
                database = databaseObject;
        }

        if (database == null)
        {
            Debug.LogError(
                "[GlobalItemManager] ItemDatabase를 찾을 수 없습니다! Resources/ItemDatabase.asset이 있는지 확인하세요."
            );
        }

        return database;
    }

    /// <summary>
    /// 서버에서 전역 아이템 수량 업데이트 수신
    /// </summary>
    public void OnServerItemCountUpdate(Dictionary<string, int> serverItemCounts)
    {
        foreach (var kvp in serverItemCounts)
        {
            SetGlobalItemCount(kvp.Key, kvp.Value);
        }

        Debug.Log(
            $"[GlobalItemManager] 서버로부터 {serverItemCounts.Count}개 아이템 수량 업데이트 수신"
        );
    }

    /// <summary>
    /// 현재 전역 아이템 수량을 딕셔너리로 반환 (서버 전송용)
    /// </summary>
    public Dictionary<string, int> GetAllItemCounts()
    {
        return new Dictionary<string, int>(itemCountDict);
    }

    /// <summary>
    /// Inspector 디버그 리스트 업데이트
    /// </summary>
    private void UpdateDebugList()
    {
        globalItemCounts.Clear();
        foreach (var kvp in itemCountDict)
        {
            globalItemCounts.Add(new GlobalItemCount { itemName = kvp.Key, count = kvp.Value });
        }
    }

    /// <summary>
    /// 특정 아이템의 사용 가능 여부 확인
    /// </summary>
    public bool IsItemAvailable(string itemName, int requiredAmount = 1)
    {
        return GetGlobalItemCount(itemName) >= requiredAmount;
    }

    /// <summary>
    /// 모든 아이템 이름 리스트 반환 (ItemDatabase에서 처리)
    /// </summary>
    public List<string> GetAllItemNames()
    {
        var database = FindItemDatabase();
        return database?.GetAllItemNames() ?? new List<string>();
    }

    /// <summary>
    /// 디버그: 전역 아이템 수량 출력
    /// </summary>
    [ContextMenu("Print Global Item Counts")]
    public void PrintGlobalItemCounts()
    {
        Debug.Log("=== Global Item Counts ===");
        foreach (var kvp in itemCountDict)
        {
            Debug.Log($"{kvp.Key}: {kvp.Value}개");
        }
    }

    /// <summary>
    /// 디버그: 테스트 아이템 추가
    /// </summary>
    [ContextMenu("Add Test Items")]
    public void AddTestItems()
    {
        var allItems = GetAllItemNames();
        foreach (string itemName in allItems)
        {
            AddGlobalItemCount(itemName, UnityEngine.Random.Range(10, 100));
        }
    }
}

/// <summary>
/// Inspector에서 전역 아이템 수량을 확인하기 위한 클래스
/// </summary>
[System.Serializable]
public class GlobalItemCount
{
    public string itemName;
    public int count;
}
