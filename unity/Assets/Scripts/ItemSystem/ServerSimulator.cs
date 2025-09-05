using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실제 서버가 없을 때 서버 역할을 시뮬레이션하는 클래스
/// 실시간 멀티플레이어 로직을 로컬에서 테스트하기 위한 목적
/// </summary>
public class ServerSimulator : MonoBehaviour
{
    public static ServerSimulator Instance;

    [Header("시뮬레이션 설정")]
    public float networkDelay = 0.1f; // 네트워크 지연 시뮬레이션 (초)
    public float failureRate = 0.05f; // 5% 실패율 시뮬레이션
    public bool enableDebugLogs = true;

    // 서버 상태 데이터
    private Dictionary<string, int> serverGlobalItems = new Dictionary<string, int>();
    private Dictionary<string, List<ChestItemData>> serverChestItems =
        new Dictionary<string, List<ChestItemData>>();
    private InventorySlotData[] serverPlayerInventory = new InventorySlotData[3]; // 3슬롯 고정

    // 이벤트
    public event Action<ItemActionResponse> OnActionResponse;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeServerState();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 서버 상태 초기화
    /// </summary>
    private void InitializeServerState()
    {
        // 플레이어 인벤토리 초기화 (빈 슬롯)
        for (int i = 0; i < serverPlayerInventory.Length; i++)
        {
            serverPlayerInventory[i] = new InventorySlotData
            {
                itemName = "",
                quantity = 0,
                isEmpty = true,
            };
        }

        // 전역 아이템 수량 초기화 (테스트용)
        serverGlobalItems["Apple"] = 100;
        serverGlobalItems["Stone"] = 100;

        // 상자들 미리 초기화 (5개 기본 상자)
        InitializePredefinedChests();

        if (enableDebugLogs)
            Debug.Log("[ServerSimulator] 서버 상태 초기화 완료");
    }

    /// <summary>
    /// 미리 정의된 상자들 초기화 (테스트용 5개 상자)
    /// </summary>
    private void InitializePredefinedChests()
    {
        var predefinedChests = new Dictionary<string, List<ChestItemData>>
        {
            ["Chest_0"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "Apple",
                    quantity = 5,
                    description = "Apple 설명",
                },
            },
            ["Chest_1"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "Stone",
                    quantity = 4,
                    description = "Stone 설명",
                },
            },
            ["Chest_2"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "Apple",
                    quantity = 6,
                    description = "Apple 설명",
                },
                new ChestItemData
                {
                    itemName = "Stone",
                    quantity = 3,
                    description = "Stone 설명",
                },
                new ChestItemData
                {
                    itemName = "Apple",
                    quantity = 1,
                    description = "Apple 설명",
                },
            },
            ["Chest_3"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "Stone",
                    quantity = 5,
                    description = "Stone 설명",
                },
                new ChestItemData
                {
                    itemName = "Apple",
                    quantity = 4,
                    description = "Apple 설명",
                },
                new ChestItemData
                {
                    itemName = "Stone",
                    quantity = 2,
                    description = "Stone 설명",
                },
                new ChestItemData
                {
                    itemName = "Apple",
                    quantity = 1,
                    description = "Apple 설명",
                },
            },
            ["Chest_4"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "Apple",
                    quantity = 3,
                    description = "Apple 설명",
                },
                new ChestItemData
                {
                    itemName = "Stone",
                    quantity = 6,
                    description = "Stone 설명",
                },
                new ChestItemData
                {
                    itemName = "Apple",
                    quantity = 2,
                    description = "Apple 설명",
                },
                new ChestItemData
                {
                    itemName = "Apple",
                    quantity = 2,
                    description = "Apple 설명",
                },
            },
        };

        // 서버 상자 딕셔너리에 추가
        foreach (var kvp in predefinedChests)
        {
            serverChestItems[kvp.Key] = kvp.Value;
            if (enableDebugLogs)
                Debug.Log(
                    $"[ServerSimulator] 상자 {kvp.Key} 초기화 완료 ({kvp.Value.Count}개 아이템)"
                );
        }
    }

    /// <summary>
    /// 클라이언트에서 액션 요청 처리
    /// </summary>
    public void ProcessActionRequest(ItemActionRequest request)
    {
        // 네트워크 지연 시뮬레이션
        StartCoroutine(ProcessActionWithDelay(request));
    }

    private System.Collections.IEnumerator ProcessActionWithDelay(ItemActionRequest request)
    {
        yield return new UnityEngine.WaitForSeconds(networkDelay);

        // 실패율 시뮬레이션
        if (UnityEngine.Random.value < failureRate)
        {
            SendFailureResponse(request, "Network error simulation");
            yield break;
        }

        // 액션 타입별 처리
        ItemActionResponse response = null;
        switch (request.action)
        {
            case ItemAction.Take:
                response = ProcessTakeItem(request);
                break;

            case ItemAction.Use:
                response = ProcessUseItem(request);
                break;

            case ItemAction.OpenChest:
                response = ProcessOpenChest(request);
                break;

            case ItemAction.RequestSync:
                response = ProcessSyncRequest(request);
                break;
        }

        // 응답 전송
        if (response != null)
        {
            response.serverTimestamp = Time.time;
            OnActionResponse?.Invoke(response);
        }
    }

    /// <summary>
    /// 아이템 획득 처리
    /// </summary>
    private ItemActionResponse ProcessTakeItem(ItemActionRequest request)
    {
        // 상자에서 아이템 확인
        if (!serverChestItems.ContainsKey(request.chestId))
        {
            return CreateFailureResponse(request, "상자를 찾을 수 없습니다");
        }

        var chestItems = serverChestItems[request.chestId];
        ChestItemData targetItem = null;
        int itemIndex = -1;

        for (int i = 0; i < chestItems.Count; i++)
        {
            if (chestItems[i].itemName == request.itemName)
            {
                targetItem = chestItems[i];
                itemIndex = i;
                break;
            }
        }

        if (targetItem == null || targetItem.quantity < request.quantity)
        {
            return CreateFailureResponse(request, "아이템이 충분하지 않습니다");
        }

        // 플레이어 인벤토리에 공간 확인 및 추가
        int addedAmount = TryAddToPlayerInventory(request.itemName, request.quantity);
        if (addedAmount == 0)
        {
            return CreateFailureResponse(request, "인벤토리가 가득 찼습니다");
        }

        // 상자에서 아이템 차감
        targetItem.quantity -= addedAmount;
        if (targetItem.quantity <= 0)
        {
            chestItems.RemoveAt(itemIndex);
        }

        // 전역 아이템 수량 업데이트 (선택적)
        if (serverGlobalItems.ContainsKey(request.itemName))
        {
            serverGlobalItems[request.itemName] = Mathf.Max(
                0,
                serverGlobalItems[request.itemName] - addedAmount
            );
        }

        return CreateSuccessResponse(
            request,
            "아이템 획득 성공",
            CreateResponseData(request.chestId)
        );
    }

    /// <summary>
    /// 아이템 사용 처리
    /// </summary>
    private ItemActionResponse ProcessUseItem(ItemActionRequest request)
    {
        // 플레이어 인벤토리에서 아이템 찾기
        InventorySlotData targetSlot = null;
        for (int i = 0; i < serverPlayerInventory.Length; i++)
        {
            var slot = serverPlayerInventory[i];
            if (
                !slot.isEmpty
                && slot.itemName == request.itemName
                && slot.quantity >= request.quantity
            )
            {
                targetSlot = slot;
                break;
            }
        }

        if (targetSlot == null)
        {
            return CreateFailureResponse(request, "사용할 아이템이 없습니다");
        }

        // 아이템 사용 처리
        targetSlot.quantity -= request.quantity;
        if (targetSlot.quantity <= 0)
        {
            targetSlot.itemName = "";
            targetSlot.quantity = 0;
            targetSlot.isEmpty = true;
        }

        return CreateSuccessResponse(request, "아이템 사용 완료", CreateResponseData());
    }

    /// <summary>
    /// 상자 열기 처리
    /// </summary>
    private ItemActionResponse ProcessOpenChest(ItemActionRequest request)
    {
        // 상자가 이미 존재하는지 확인
        if (!serverChestItems.ContainsKey(request.chestId))
        {
            // 미리 정의된 상자 ID 패턴 확인
            if (IsPredefinedChestId(request.chestId))
            {
                Debug.LogWarning(
                    $"[ServerSimulator] 미리 정의된 상자 {request.chestId}가 없음! 다시 생성"
                );
                GeneratePredefinedChest(request.chestId);
            }
            else
            {
                // 동적으로 생성된 상자 ID - 랜덤 아이템 생성
                GenerateRandomChestItems(request.chestId);
            }
        }

        if (enableDebugLogs)
            Debug.Log(
                $"[ServerSimulator] 상자 {request.chestId} 열기 - {serverChestItems[request.chestId].Count}개 아이템"
            );

        return CreateSuccessResponse(
            request,
            "상자 열기 성공",
            CreateResponseData(request.chestId)
        );
    }

    /// <summary>
    /// 미리 정의된 상자 ID인지 확인
    /// </summary>
    private bool IsPredefinedChestId(string chestId)
    {
        return chestId.StartsWith("Chest_") && chestId.Length <= 7; // Chest_0 ~ Chest_4 형태
    }

    /// <summary>
    /// 미리 정의된 상자 생성 (실패시 대체)
    /// </summary>
    private void GeneratePredefinedChest(string chestId)
    {
        // 기본 패턴으로 생성
        var items = new List<ChestItemData>
        {
            new ChestItemData
            {
                itemName = "Apple",
                quantity = 3,
                description = "Apple 설명",
            },
            new ChestItemData
            {
                itemName = "Stone",
                quantity = 2,
                description = "Stone 설명",
            },
            new ChestItemData
            {
                itemName = "Apple",
                quantity = 1,
                description = "Apple 설명",
            },
        };

        serverChestItems[chestId] = items;
        Debug.Log($"[ServerSimulator] 대체 상자 {chestId} 생성 완료");
    }

    /// <summary>
    /// 동기화 요청 처리
    /// </summary>
    private ItemActionResponse ProcessSyncRequest(ItemActionRequest request)
    {
        return CreateSuccessResponse(request, "동기화 완료", CreateResponseData());
    }

    /// <summary>
    /// 플레이어 인벤토리에 아이템 추가 시도
    /// </summary>
    private int TryAddToPlayerInventory(string itemName, int quantity)
    {
        int remainingAmount = quantity;

        // 1단계: 같은 아이템 스택에 추가
        for (int i = 0; i < serverPlayerInventory.Length; i++)
        {
            var slot = serverPlayerInventory[i];
            if (!slot.isEmpty && slot.itemName == itemName)
            {
                int maxStack = 99; // ItemDatabase에서 가져와야 하지만 시뮬레이션에서는 고정값
                int canAdd = Mathf.Min(remainingAmount, maxStack - slot.quantity);
                slot.quantity += canAdd;
                remainingAmount -= canAdd;

                if (remainingAmount <= 0)
                    break;
            }
        }

        // 2단계: 빈 슬롯에 추가
        if (remainingAmount > 0)
        {
            for (int i = 0; i < serverPlayerInventory.Length; i++)
            {
                var slot = serverPlayerInventory[i];
                if (slot.isEmpty)
                {
                    slot.itemName = itemName;
                    slot.quantity = remainingAmount;
                    slot.isEmpty = false;
                    remainingAmount = 0;
                    break;
                }
            }
        }

        return quantity - remainingAmount;
    }

    /// <summary>
    /// 랜덤 상자 아이템 생성 (테스트용)
    /// </summary>
    private void GenerateRandomChestItems(string chestId)
    {
        var chestItems = new List<ChestItemData>();

        // 상자 ID를 기반으로 시드 생성 (같은 상자는 항상 같은 아이템)
        int seed = chestId.GetHashCode();
        UnityEngine.Random.InitState(seed);

        var availableItems = new[] { "Apple", "Stone" };
        int itemCount = UnityEngine.Random.Range(3, 5); // 3-4개 아이템

        for (int i = 0; i < itemCount; i++)
        {
            string randomItem = availableItems[UnityEngine.Random.Range(0, availableItems.Length)];
            int randomQuantity = UnityEngine.Random.Range(1, 6); // 1-5개 수량

            chestItems.Add(
                new ChestItemData
                {
                    itemName = randomItem,
                    quantity = randomQuantity,
                    description = $"{randomItem} 설명",
                }
            );
        }

        // 랜덤 시드 복원
        UnityEngine.Random.InitState(System.Environment.TickCount);

        serverChestItems[chestId] = chestItems;

        if (enableDebugLogs)
            Debug.Log(
                $"[ServerSimulator] 상자 {chestId}에 {itemCount}개 아이템 생성 (시드: {seed})"
            );
    }

    /// <summary>
    /// 성공 응답 생성
    /// </summary>
    private ItemActionResponse CreateSuccessResponse(
        ItemActionRequest request,
        string message,
        ItemActionData data = null
    )
    {
        return new ItemActionResponse
        {
            action = request.action,
            success = true,
            message = message,
            data = data,
        };
    }

    /// <summary>
    /// 실패 응답 생성
    /// </summary>
    private ItemActionResponse CreateFailureResponse(ItemActionRequest request, string message)
    {
        return new ItemActionResponse
        {
            action = request.action,
            success = false,
            message = message,
            data = null,
        };
    }

    /// <summary>
    /// 실패 응답 전송
    /// </summary>
    private void SendFailureResponse(ItemActionRequest request, string message)
    {
        var response = CreateFailureResponse(request, message);
        response.serverTimestamp = Time.time;
        OnActionResponse?.Invoke(response);
    }

    /// <summary>
    /// 응답 데이터 생성
    /// </summary>
    private ItemActionData CreateResponseData(string chestId = null)
    {
        var data = new ItemActionData
        {
            playerInventory = new PlayerInventoryData { slots = serverPlayerInventory },
            globalItemCounts = new Dictionary<string, int>(serverGlobalItems),
        };

        if (!string.IsNullOrEmpty(chestId) && serverChestItems.ContainsKey(chestId))
        {
            data.chestInventory = new ChestInventoryData
            {
                chestId = chestId,
                items = serverChestItems[chestId].ToArray(),
            };
        }

        return data;
    }

    /// <summary>
    /// 디버그: 서버 상태 출력
    /// </summary>
    [ContextMenu("Print Server State")]
    public void PrintServerState()
    {
        Debug.Log("=== Server State ===");

        Debug.Log("플레이어 인벤토리:");
        for (int i = 0; i < serverPlayerInventory.Length; i++)
        {
            var slot = serverPlayerInventory[i];
            if (slot.isEmpty)
                Debug.Log($"  슬롯 {i}: [빈 슬롯]");
            else
                Debug.Log($"  슬롯 {i}: {slot.itemName} x{slot.quantity}");
        }

        Debug.Log("전역 아이템:");
        foreach (var kvp in serverGlobalItems)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value}개");
        }

        Debug.Log($"상자 개수: {serverChestItems.Count}개");
    }
}
