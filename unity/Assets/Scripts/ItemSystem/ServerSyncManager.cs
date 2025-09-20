using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 실시간 멀티플레이어를 위한 서버 동기화 매니저
/// - 액션 기반 실시간 처리
/// - 서버 시뮬레이션 지원
/// </summary>
public class ServerSyncManager : MonoBehaviour
{
    public static ServerSyncManager Instance;

    [Header("실시간 처리 설정")]
    public bool useServerSimulator = false; // 실제 서버 연결로 변경 (시뮬레이터 비활성화)
    public float requestTimeout = 10.0f; // 요청 타임아웃 (초)
    public bool enableDebugLogs = true;

    [Header("플레이어 정보")]
    public string playerId = "Player_001"; // 실제로는 로그인 시 할당

    // 서버 통신 이벤트
    public event Action OnConnectionEstablished;
    public event Action<ItemActionResponse> OnActionResponse;

    // 응답 대기 중인 요청들
    private Dictionary<float, ItemActionRequest> pendingRequests =
        new Dictionary<float, ItemActionRequest>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeConnection();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 서버 시뮬레이터 연결
        if (useServerSimulator && ServerSimulator.Instance != null)
        {
            ServerSimulator.Instance.OnActionResponse += HandleServerResponse;
            OnConnectionEstablished?.Invoke();

            if (enableDebugLogs)
                Debug.Log("[ServerSyncManager] 서버 시뮬레이터 연결 완료");
        }
        else if (!useServerSimulator)
        {
            // 실제 서버 연결 로직 (TODO: 실제 서버 URL과 연결 구현 필요)
            if (enableDebugLogs)
                Debug.Log("[ServerSyncManager] 실제 서버 연결 모드로 설정됨 - 서버 연결 구현 필요");

            // 임시로 연결된 것으로 처리 (실제 서버 연결 시 제거)
            OnConnectionEstablished?.Invoke();
        }
        else
        {
            Debug.LogWarning("[ServerSyncManager] 서버 시뮬레이터가 활성화되었지만 ServerSimulator.Instance를 찾을 수 없음");
        }
    }

    void Update()
    {
        // 타임아웃된 요청 처리
        CheckRequestTimeouts();
    }

    /// <summary>
    /// 서버 연결 초기화
    /// </summary>
    private void InitializeConnection()
    {
        if (enableDebugLogs)
            Debug.Log("[ServerSyncManager] 실시간 서버 동기화 매니저 초기화");
    }

    /// <summary>
    /// 플레이어가 아이템을 획득할 때 서버에 즉시 요청
    /// </summary>
    public void RequestTakeItem(string chestId, string itemName, int quantity)
    {
        var request = new ItemActionRequest
        {
            action = ItemAction.Take,
            itemName = itemName,
            chestId = chestId,
            quantity = quantity,
            timestamp = Time.time,
            playerId = playerId,
        };

        SendActionRequest(request);

        if (enableDebugLogs)
            Debug.Log(
                $"[ServerSyncManager] 아이템 획득 요청: {itemName} x{quantity} from {chestId}"
            );
    }

    /// <summary>
    /// 플레이어가 아이템을 사용할 때 서버에 즉시 요청
    /// </summary>
    public void RequestUseItem(string itemName, int quantity)
    {
        var request = new ItemActionRequest
        {
            action = ItemAction.Use,
            itemName = itemName,
            quantity = quantity,
            timestamp = Time.time,
            playerId = playerId,
        };

        SendActionRequest(request);

        if (enableDebugLogs)
            Debug.Log($"[ServerSyncManager] 아이템 사용 요청: {itemName} x{quantity}");
    }

    /// <summary>
    /// 상자 열기 요청 (실시간으로 상자 내용물 받기)
    /// </summary>
    public void RequestOpenChest(string chestId)
    {
        var request = new ItemActionRequest
        {
            action = ItemAction.OpenChest,
            chestId = chestId,
            timestamp = Time.time,
            playerId = playerId,
        };

        SendActionRequest(request);

        if (enableDebugLogs)
            Debug.Log($"[ServerSyncManager] 상자 열기 요청: {chestId}");
    }

    /// <summary>
    /// 전체 동기화 요청
    /// </summary>
    public void RequestFullSync()
    {
        var request = new ItemActionRequest
        {
            action = ItemAction.RequestSync,
            timestamp = Time.time,
            playerId = playerId,
        };

        SendActionRequest(request);

        if (enableDebugLogs)
            Debug.Log("[ServerSyncManager] 전체 동기화 요청");
    }

    /// <summary>
    /// 서버에 액션 요청 전송
    /// </summary>
    private void SendActionRequest(ItemActionRequest request)
    {
        // 요청 추적을 위해 저장
        pendingRequests[request.timestamp] = request;

        if (useServerSimulator && ServerSimulator.Instance != null)
        {
            // 서버 시뮬레이터로 전송
            ServerSimulator.Instance.ProcessActionRequest(request);
        }
        else
        {
            // 실제 서버로 전송 (구현 필요)
            SendToRealServer(request);
        }
    }

    /// <summary>
    /// 실제 서버로 요청 전송 (미구현)
    /// </summary>
    private void SendToRealServer(ItemActionRequest request)
    {
        // TODO: 실제 서버 통신 구현
        // - HTTP REST API
        // - WebSocket
        // - Unity Netcode 등

        Debug.LogWarning("[ServerSyncManager] 실제 서버 통신이 구현되지 않았습니다!");

        // 임시로 실패 응답 생성
        var failureResponse = new ItemActionResponse
        {
            action = request.action,
            success = false,
            message = "Real server not implemented",
            serverTimestamp = Time.time,
        };

        HandleServerResponse(failureResponse);
    }

    /// <summary>
    /// 서버 응답 처리
    /// </summary>
    private void HandleServerResponse(ItemActionResponse response)
    {
        // 대기 중인 요청에서 제거
        var requestToRemove = -1f;
        foreach (var kvp in pendingRequests)
        {
            if (
                Mathf.Abs(
                    kvp.Value.timestamp
                        - (response.serverTimestamp - (useServerSimulator ? 0.1f : 0))
                ) < 0.1f
            )
            {
                requestToRemove = kvp.Key;
                break;
            }
        }

        if (requestToRemove != -1f)
        {
            pendingRequests.Remove(requestToRemove);
        }

        // 응답 처리
        if (response.success)
        {
            HandleSuccessResponse(response);
        }
        else
        {
            HandleFailureResponse(response);
        }

        // 외부 이벤트 발생
        OnActionResponse?.Invoke(response);
    }

    /// <summary>
    /// 성공 응답 처리
    /// </summary>
    private void HandleSuccessResponse(ItemActionResponse response)
    {
        if (response.data == null)
            return;

        // 플레이어 인벤토리 업데이트
        if (response.data.playerInventory != null && PlayerInventory.Instance != null)
        {
            UpdatePlayerInventoryFromServer(response.data.playerInventory);
        }

        // 상자 인벤토리 업데이트 (Service → ViewModel → View 패턴)
        if (response.data.chestInventory != null)
        {
            var chestViewModel = ViewModels.UI.ChestViewModel.Instance;
            if (chestViewModel != null)
            {
                // ChestService를 통해 서버 데이터 동기화
                UpdateChestServiceFromServer(response.data.chestInventory, chestViewModel);
            }
            else
            {
                Debug.LogWarning("[ServerSyncManager] ChestViewModel.Instance가 null입니다!");
            }
        }

        // 전역 아이템 수량 업데이트
        if (response.data.globalItemCounts != null && GlobalItemManager.Instance != null)
        {
            GlobalItemManager.Instance.OnServerItemCountUpdate(response.data.globalItemCounts);
        }

        if (enableDebugLogs)
            Debug.Log(
                $"[ServerSyncManager] 서버 응답 처리 성공: {response.action} - {response.message}"
            );
    }

    /// <summary>
    /// 실패 응답 처리
    /// </summary>
    private void HandleFailureResponse(ItemActionResponse response)
    {
        Debug.LogWarning(
            $"[ServerSyncManager] 서버 요청 실패: {response.action} - {response.message}"
        );

        // UI에 에러 메시지 표시 등의 처리 가능
        // UIManager.Instance?.ShowErrorMessage(response.message);
    }

    /// <summary>
    /// 서버 데이터로부터 플레이어 인벤토리 업데이트
    /// </summary>
    private void UpdatePlayerInventoryFromServer(PlayerInventoryData serverData)
    {
        if (PlayerInventory.Instance == null || serverData.slots == null)
            return;

        // 서버의 정확한 데이터로 인벤토리 상태 동기화
        for (int i = 0; i < serverData.slots.Length && i < 3; i++) // 3슬롯 제한
        {
            var serverSlot = serverData.slots[i];

            if (serverSlot.isEmpty)
            {
                PlayerInventory.Instance.ClearSlot(i);
            }
            else
            {
                // 현재 슬롯 상태와 서버 상태 비교 후 업데이트
                var currentItem = PlayerInventory.Instance.GetItem(i);
                if (
                    currentItem == null
                    || currentItem.IsEmpty()
                    || currentItem.itemName != serverSlot.itemName
                    || currentItem.quantity != serverSlot.quantity
                )
                {
                    // PlayerInventory.OnServerActionResponse()에서 처리하므로 여기서는 제거
                    // 직접 조작하면 슬롯 순서가 파괴됨
                    Debug.Log(
                        $"[ServerSyncManager] 인벤토리 동기화는 PlayerInventory에서 처리됨 - 슬롯{i} 스킵"
                    );
                }
            }
        }
    }

    /// <summary>
    /// 서버 데이터를 ChestService를 통해 MVVM 시스템으로 전달
    /// </summary>
    private void UpdateChestServiceFromServer(
        ChestInventoryData serverData,
        ViewModels.UI.ChestViewModel chestViewModel
    )
    {
        if (serverData == null || chestViewModel == null)
            return;

        // 서버 데이터를 ChestSlot 배열로 변환
        var chestSlots = new Models.ChestSlot[9]; // 상자는 9슬롯 고정

        // 빈 슬롯으로 초기화
        for (int i = 0; i < chestSlots.Length; i++)
        {
            chestSlots[i] = new Models.ChestSlot();
        }

        // 서버 아이템을 슬롯에 배치
        for (int i = 0; i < serverData.items.Length && i < chestSlots.Length; i++)
        {
            var serverItem = serverData.items[i];
            chestSlots[i] = new Models.ChestSlot(serverItem.itemName, serverItem.quantity);
        }

        // ChestService를 통해 데이터 동기화 (Service → ViewModel → View)
        chestViewModel.SyncChestData(serverData.chestId, chestSlots);

        if (enableDebugLogs)
            Debug.Log(
                $"[ServerSyncManager] ChestService를 통해 상자 데이터 동기화: {serverData.chestId}, {serverData.items.Length}개 아이템"
            );
    }

    /// <summary>
    /// 타임아웃된 요청들 체크
    /// </summary>
    private void CheckRequestTimeouts()
    {
        var currentTime = Time.time;
        var timeoutRequests = new System.Collections.Generic.List<float>();

        foreach (var kvp in pendingRequests)
        {
            if (currentTime - kvp.Key > requestTimeout)
            {
                timeoutRequests.Add(kvp.Key);
            }
        }

        foreach (var timestamp in timeoutRequests)
        {
            var request = pendingRequests[timestamp];
            pendingRequests.Remove(timestamp);

            Debug.LogError(
                $"[ServerSyncManager] 요청 타임아웃: {request.action} - {request.itemName}"
            );

            // 타임아웃 응답 생성
            var timeoutResponse = new ItemActionResponse
            {
                action = request.action,
                success = false,
                message = "Request timeout",
                serverTimestamp = currentTime,
            };

            OnActionResponse?.Invoke(timeoutResponse);
        }
    }

    /// <summary>
    /// 연결 해제
    /// </summary>
    void OnDestroy()
    {
        if (useServerSimulator && ServerSimulator.Instance != null)
        {
            ServerSimulator.Instance.OnActionResponse -= HandleServerResponse;
        }
    }

    /// <summary>
    /// 디버그: 대기 중인 요청 수 출력
    /// </summary>
    [ContextMenu("Print Pending Requests")]
    public void PrintPendingRequests()
    {
        Debug.Log($"[ServerSyncManager] 대기 중인 요청: {pendingRequests.Count}개");
        foreach (var kvp in pendingRequests)
        {
            Debug.Log($"  {kvp.Value.action}: {kvp.Value.itemName} (시간: {kvp.Key})");
        }
    }
}
