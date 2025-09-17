using System;
using System.Collections;
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
    public float failureRate = 0f; // 5% 실패율 시뮬레이션
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
        serverGlobalItems["taser"] = 20; // 테이저건 (기존 Apple)
        serverGlobalItems["flashbang"] = 20; // 섬광탄 (기존 Stone)
        serverGlobalItems["defibrillator"] = 20; // 자가제세동기
        serverGlobalItems["light"] = 500; // 빛젤리 (기존 Mushroom) - 꿈틀이 먹이용으로 많이 제공

        // 상자들 미리 초기화 (5개 기본 상자)
        InitializePredefinedChests();

        if (enableDebugLogs)
            Debug.Log("[ServerSimulator] 서버 상태 초기화 완료");
    }

    /// <summary>
    /// 미리 정의된 상자들 초기화 (테스트용 5개 상자)
    /// 각 아이템이 개별 슬롯을 차지하도록 구성
    /// </summary>
    private void InitializePredefinedChests()
    {
        var predefinedChests = new Dictionary<string, List<ChestItemData>>
        {
            ["Chest_0"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "taser",
                    quantity = 1,
                    description = "테이저건",
                },
                new ChestItemData
                {
                    itemName = "taser",
                    quantity = 1,
                    description = "테이저건",
                },
                new ChestItemData
                {
                    itemName = "flashbang",
                    quantity = 1,
                    description = "섬광탄",
                },
            },
            ["Chest_1"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "taser",
                    quantity = 1,
                    description = "테이저건",
                },
                new ChestItemData
                {
                    itemName = "flashbang",
                    quantity = 1,
                    description = "섬광탄",
                },
            },
            ["Chest_2"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "defibrillator",
                    quantity = 1,
                    description = "자가제세동기",
                },
            },
            ["Chest_3"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "flashbang",
                    quantity = 1,
                    description = "섬광탄",
                },
            },
            ["Chest_4"] = new List<ChestItemData>
            {
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "light",
                    quantity = 1,
                    description = "빛젤리",
                },
                new ChestItemData
                {
                    itemName = "taser",
                    quantity = 1,
                    description = "테이저건",
                },
                new ChestItemData
                {
                    itemName = "flashbang",
                    quantity = 1,
                    description = "섬광탄",
                },
                new ChestItemData
                {
                    itemName = "defibrillator",
                    quantity = 1,
                    description = "자가제세동기",
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

            case ItemAction.Put:
                response = ProcessPutItem(request);
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

        // Mushroom인 경우 FeedingInventory로, 다른 아이템은 PlayerInventory로
        int addedAmount = 0;
        if (request.itemName == "Mushroom")
        {
            // Mushroom은 먹이 인벤토리로 (제한 없음)
            addedAmount = request.quantity;

            // 실제로 InventoryViewModel에 추가
            if (ViewModels.UI.InventoryViewModel.Instance != null)
            {
                ViewModels.UI.InventoryViewModel.Instance.AddFeeding(addedAmount);
                Debug.Log($"[ServerSimulator] Mushroom {addedAmount}개 - 먹이 인벤토리로 추가됨");
            }
            else
            {
                Debug.LogError("[ServerSimulator] InventoryViewModel.Instance가 null입니다!");
            }
        }
        else
        {
            // 다른 아이템은 플레이어 인벤토리로
            addedAmount = TryAddToPlayerInventory(request.itemName, request.quantity);
            if (addedAmount == 0)
            {
                return CreateFailureResponse(request, "인벤토리가 가득 찼습니다");
            }
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
            CreateChestOnlyResponseData(request.chestId)
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
    /// 아이템 넣기 처리 (인벤토리 → 상자)
    /// </summary>
    private ItemActionResponse ProcessPutItem(ItemActionRequest request)
    {
        // 플레이어 인벤토리에서 아이템 찾기 및 제거
        bool itemFound = false;
        for (int i = 0; i < serverPlayerInventory.Length; i++)
        {
            var slot = serverPlayerInventory[i];
            if (
                !slot.isEmpty
                && slot.itemName == request.itemName
                && slot.quantity >= request.quantity
            )
            {
                // 아이템 제거
                slot.quantity -= request.quantity;
                if (slot.quantity <= 0)
                {
                    slot.isEmpty = true;
                    slot.itemName = "";
                    slot.quantity = 0;
                }
                itemFound = true;
                break;
            }
        }

        if (!itemFound)
        {
            return CreateFailureResponse(request, "인벤토리에 해당 아이템이 없습니다");
        }

        // 상자에 아이템 추가
        if (!serverChestItems.ContainsKey(request.chestId))
        {
            serverChestItems[request.chestId] = new List<ChestItemData>();
        }

        var chestItems = serverChestItems[request.chestId];
        chestItems.Add(
            new ChestItemData
            {
                itemName = request.itemName,
                quantity = request.quantity,
                description = "",
            }
        );

        // 전역 아이템 수량은 변경하지 않음 (인벤토리 → 상자 이동이므로)

        // 인벤토리 정리
        CompactServerInventory();

        if (enableDebugLogs)
            Debug.Log(
                $"[ServerSimulator] 상자에 아이템 넣기 완료: {request.itemName} x{request.quantity} → {request.chestId}"
            );

        return CreateSuccessResponse(
            request,
            $"{request.itemName} {request.quantity}개를 상자에 넣었습니다",
            CreateResponseData(request.chestId)
        );
    }

    /// <summary>
    /// 플레이어 인벤토리에 아이템 추가 시도 (각 슬롯별 다른 아이템, 슬롯당 최대 3개)
    /// </summary>
    private int TryAddToPlayerInventory(string itemName, int quantity)
    {
        int remainingAmount = quantity;

        // 1단계: 같은 아이템이 있는 슬롯에 스택 (한 슬롯에만)
        for (int i = 0; i < serverPlayerInventory.Length; i++)
        {
            var slot = serverPlayerInventory[i];
            if (!slot.isEmpty && slot.itemName == itemName)
            {
                int maxStack = 3; // 플레이어 인벤토리는 최대 3개까지 스택
                int canAdd = Mathf.Min(remainingAmount, maxStack - slot.quantity);
                slot.quantity += canAdd;
                remainingAmount -= canAdd;

                // 해당 아이템이 있는 슬롯에서만 스택하고 종료 (다른 슬롯에는 같은 아이템 추가 안함)
                break;
            }
        }

        // 2단계: 같은 아이템이 없고 남은 수량이 있으면 빈 슬롯에 새로 추가
        if (remainingAmount > 0)
        {
            for (int i = 0; i < serverPlayerInventory.Length; i++)
            {
                var slot = serverPlayerInventory[i];
                if (slot.isEmpty)
                {
                    int maxStack = 3; // 플레이어 인벤토리는 최대 3개까지 스택
                    slot.itemName = itemName;
                    slot.quantity = Mathf.Min(remainingAmount, maxStack);
                    slot.isEmpty = false;
                    remainingAmount -= slot.quantity;

                    // 한 슬롯에만 추가하고 종료
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

        var availableItems = new[] { "Mushroom", "Apple", "Stone" };
        int itemCount = UnityEngine.Random.Range(6, 10); // 6-9개 아이템 (9슬롯 최대)

        for (int i = 0; i < itemCount; i++)
        {
            // Mushroom이 나올 확률을 높임 (70%)
            string randomItem;
            float itemRoll = UnityEngine.Random.value;
            if (itemRoll < 0.7f)
            {
                randomItem = "Mushroom";
            }
            else if (itemRoll < 0.85f)
            {
                randomItem = "Apple";
            }
            else
            {
                randomItem = "Stone";
            }

            // 모든 아이템은 1개씩만 (겹치지 않음)
            int randomQuantity = 1;

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
    /// 응답 데이터 생성 (플레이어 인벤토리 포함)
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
    /// OpenChest 전용 응답 데이터 생성 (상자 데이터만 포함, 플레이어 인벤토리 제외)
    /// </summary>
    private ItemActionData CreateChestOnlyResponseData(string chestId)
    {
        var data = new ItemActionData
        {
            playerInventory = null, // OpenChest는 플레이어 인벤토리 업데이트 불필요
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
    /// 아이템 사용 요청 처리
    /// </summary>
    public void RequestUseItem(string itemName, int quantity = 1)
    {
        var request = new ItemActionRequest
        {
            action = ItemAction.Use,
            itemName = itemName,
            quantity = quantity,
            playerId = "Player1",
            timestamp = Time.time,
        };

        if (enableDebugLogs)
            Debug.Log($"[ServerSimulator] 아이템 사용 요청: {itemName} x{quantity}");

        StartCoroutine(ProcessUseItemRequest(request));
    }

    /// <summary>
    /// 아이템 사용 요청 비동기 처리
    /// </summary>
    private System.Collections.IEnumerator ProcessUseItemRequest(ItemActionRequest request)
    {
        yield return new WaitForSeconds(networkDelay);

        // 실패율 시뮬레이션
        if (UnityEngine.Random.value < failureRate)
        {
            SendFailureResponse(request, "네트워크 오류로 요청이 실패했습니다");
            yield break;
        }

        // 플레이어 인벤토리에서 해당 아이템 찾기
        int slotIndex = -1;
        for (int i = 0; i < serverPlayerInventory.Length; i++)
        {
            if (
                !serverPlayerInventory[i].isEmpty
                && serverPlayerInventory[i].itemName == request.itemName
                && serverPlayerInventory[i].quantity >= request.quantity
            )
            {
                slotIndex = i;
                break;
            }
        }

        if (slotIndex == -1)
        {
            SendFailureResponse(request, "인벤토리에 해당 아이템이 없습니다");
            yield break;
        }

        // 아이템 사용 처리
        serverPlayerInventory[slotIndex].quantity -= request.quantity;
        if (serverPlayerInventory[slotIndex].quantity <= 0)
        {
            // 아이템이 다 소모되면 슬롯 비우기
            serverPlayerInventory[slotIndex] = new InventorySlotData
            {
                itemName = "",
                quantity = 0,
                isEmpty = true,
            };
        }

        // 인벤토리 정리 (빈 슬롯을 뒤로 이동)
        CompactServerInventory();

        // 성공 응답 전송
        var response = new ItemActionResponse
        {
            action = request.action,
            success = true,
            message = $"{request.itemName} {request.quantity}개를 사용했습니다",
            serverTimestamp = Time.time,
            data = CreateResponseData(),
        };

        OnActionResponse?.Invoke(response);

        if (enableDebugLogs)
            Debug.Log(
                $"[ServerSimulator] 아이템 사용 완료: {request.itemName} x{request.quantity}"
            );
    }

    /// <summary>
    /// 서버 인벤토리 정리 (빈 슬롯을 뒤로 이동)
    /// </summary>
    private void CompactServerInventory()
    {
        var compactedSlots = new InventorySlotData[serverPlayerInventory.Length];

        // 먼저 모든 슬롯을 빈 슬롯으로 초기화
        for (int i = 0; i < compactedSlots.Length; i++)
        {
            compactedSlots[i] = new InventorySlotData
            {
                itemName = "",
                quantity = 0,
                isEmpty = true,
            };
        }

        // 빈 슬롯이 아닌 아이템들만 앞쪽으로 이동
        int writeIndex = 0;
        for (int i = 0; i < serverPlayerInventory.Length; i++)
        {
            if (!serverPlayerInventory[i].isEmpty)
            {
                compactedSlots[writeIndex] = serverPlayerInventory[i];
                writeIndex++;
            }
        }

        serverPlayerInventory = compactedSlots;

        if (enableDebugLogs)
            Debug.Log("[ServerSimulator] 서버 인벤토리 정리 완료");
    }

    /// <summary>
    /// 인벤토리에서 상자로 아이템 넣기 요청 처리
    /// </summary>
    public void RequestPutItemToChest(string chestId, string itemName, int quantity = 1)
    {
        var request = new ItemActionRequest
        {
            action = ItemAction.Put, // 인벤토리 → 상자로 아이템 넣기
            chestId = chestId,
            itemName = itemName,
            quantity = quantity,
            playerId = "Player1",
            timestamp = Time.time,
        };

        if (enableDebugLogs)
            Debug.Log(
                $"[ServerSimulator] 상자에 아이템 넣기 요청: {itemName} x{quantity} → {chestId}"
            );

        ProcessActionRequest(request);
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
