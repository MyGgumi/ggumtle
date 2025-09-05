using System.Collections.Generic;

/// <summary>
/// 아이템 액션 타입
/// </summary>
public enum ItemAction
{
    Take, // 아이템 획득
    Use, // 아이템 사용
    OpenChest, // 상자 열기
    RequestSync, // 동기화 요청
}

/// <summary>
/// 서버로 보내는 아이템 액션 요청
/// </summary>
[System.Serializable]
public class ItemActionRequest
{
    public ItemAction action;
    public string itemName;
    public string chestId;
    public int quantity;
    public float timestamp;
    public string playerId; // 멀티플레이어용
}

/// <summary>
/// 서버로부터 받는 아이템 액션 응답
/// </summary>
[System.Serializable]
public class ItemActionResponse
{
    public ItemAction action;
    public bool success;
    public string message;
    public ItemActionData data;
    public float serverTimestamp;
}

/// <summary>
/// 아이템 액션 응답 데이터
/// </summary>
[System.Serializable]
public class ItemActionData
{
    // 플레이어 인벤토리 업데이트
    public PlayerInventoryData playerInventory;

    // 상자 아이템 업데이트
    public ChestInventoryData chestInventory;

    // 전역 아이템 수량 업데이트
    public Dictionary<string, int> globalItemCounts;
}

/// <summary>
/// 플레이어 인벤토리 데이터 (서버 → 클라이언트)
/// </summary>
[System.Serializable]
public class PlayerInventoryData
{
    public InventorySlotData[] slots;
}

/// <summary>
/// 인벤토리 슬롯 데이터
/// </summary>
[System.Serializable]
public class InventorySlotData
{
    public string itemName;
    public int quantity;
    public bool isEmpty;
}

/// <summary>
/// 상자 인벤토리 데이터 (서버 → 클라이언트)
/// </summary>
[System.Serializable]
public class ChestInventoryData
{
    public string chestId;
    public ChestItemData[] items;
}

/// <summary>
/// 상자 아이템 데이터
/// </summary>
[System.Serializable]
public class ChestItemData
{
    public string itemName;
    public int quantity;
    public string description; // 서버에서 오버라이드할 수도 있음
}
