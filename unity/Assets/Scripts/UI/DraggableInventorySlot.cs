using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 드래그 가능한 인벤토리 슬롯
/// 인벤토리에서 상자로 아이템을 드래그해서 옮길 수 있음
/// </summary>
public class DraggableInventorySlot
    : MonoBehaviour,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
{
    [Header("드래그 설정")]
    public Canvas parentCanvas;
    public GraphicRaycaster graphicRaycaster;

    [Header("드래그 시각 효과")]
    public GameObject dragPreviewPrefab; // 드래그 중 표시할 프리뷰
    public Color dragColor = new Color(1f, 1f, 1f, 0.7f);

    private int slotIndex;
    private InventoryItem currentItem;
    private GameObject dragPreview;
    private Vector3 originalPosition;
    private CanvasGroup canvasGroup;
    private Image slotImage;
    private static bool isAnySlotProcessing = false; // 글로벌 처리 상태 (모든 슬롯 공유)
    private bool isDragging = false; // 유효한 드래그 상태 추적
    private static int requestCounter = 0; // 요청 추적용 카운터

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        slotImage = GetComponent<Image>();

        // Canvas와 GraphicRaycaster 자동 찾기
        if (parentCanvas == null)
            parentCanvas = GetComponentInParent<Canvas>();

        if (graphicRaycaster == null)
            graphicRaycaster = GetComponentInParent<GraphicRaycaster>();
    }

    void Start()
    {
        // 서버 응답 이벤트 연결
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.OnActionResponse += OnServerActionResponse;
        }
    }

    void OnDestroy()
    {
        // 이벤트 연결 해제
        if (ServerSyncManager.Instance != null)
        {
            ServerSyncManager.Instance.OnActionResponse -= OnServerActionResponse;
        }
    }

    /// <summary>
    /// 슬롯 초기화
    /// </summary>
    public void Initialize(int index)
    {
        slotIndex = index;
        UpdateSlotData();
    }

    /// <summary>
    /// 슬롯 데이터 업데이트
    /// </summary>
    public void UpdateSlotData()
    {
        if (PlayerInventory.Instance != null)
        {
            currentItem = PlayerInventory.Instance.GetItem(slotIndex);
        }
    }

    /// <summary>
    /// 드래그 시작
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 다른 슬롯에서 처리 중이면 드래그 불가
        if (isAnySlotProcessing)
        {
            Debug.LogWarning(
                $"[DraggableInventorySlot] 다른 슬롯 처리 중이므로 드래그 불가 - 슬롯 {slotIndex}"
            );
            isDragging = false;
            return;
        }

        // 최신 슬롯 데이터로 업데이트
        UpdateSlotData();

        // 빈 슬롯이면 드래그 불가
        if (currentItem == null || currentItem.IsEmpty())
        {
            Debug.Log($"[DraggableInventorySlot] 슬롯 {slotIndex} 빈 슬롯이라 드래그 불가");
            isDragging = false;
            return;
        }

        Debug.Log(
            $"[DraggableInventorySlot] 드래그 시작: {currentItem.itemName} (슬롯 {slotIndex})"
        );

        isDragging = true;
        originalPosition = transform.position;

        // 드래그 프리뷰 생성
        CreateDragPreview();

        // 원본 슬롯 투명하게 및 상호작용 비활성화
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.5f;
            canvasGroup.blocksRaycasts = false;
        }

        // 버튼 클릭 비활성화 (드래그 중에는 클릭 방지)
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.interactable = false;
        }
    }

    /// <summary>
    /// 드래그 중
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (isDragging && dragPreview != null)
        {
            dragPreview.transform.position = eventData.position;
        }
    }

    /// <summary>
    /// 드래그 종료
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log(
            $"[DraggableInventorySlot] OnEndDrag 시작 - 슬롯 {slotIndex}, 유효드래그: {isDragging}, 처리중: {isAnySlotProcessing}"
        );

        // 원본 슬롯 복구
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }

        // 버튼 클릭 다시 활성화
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.interactable = true;
        }

        // 드래그 프리뷰 제거
        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }

        // 유효하지 않은 드래그이거나 이미 처리 중이면 무시
        if (!isDragging || isAnySlotProcessing)
        {
            Debug.LogWarning(
                $"[DraggableInventorySlot] 무효한 드래그이므로 OnEndDrag 무시 - 슬롯 {slotIndex}"
            );
            isDragging = false;
            return;
        }

        // 드롭 위치 확인
        CheckDropTarget(eventData);

        // 드래그 상태 초기화
        isDragging = false;
    }

    /// <summary>
    /// 드래그 프리뷰 생성
    /// </summary>
    private void CreateDragPreview()
    {
        if (dragPreviewPrefab != null)
        {
            dragPreview = Instantiate(dragPreviewPrefab, parentCanvas.transform);
        }
        else
        {
            // 기본 프리뷰 생성 (원본 복사)
            dragPreview = new GameObject("DragPreview");
            dragPreview.transform.SetParent(parentCanvas.transform, false);

            // 이미지 복사
            Image previewImage = dragPreview.AddComponent<Image>();
            if (slotImage != null)
            {
                previewImage.sprite = slotImage.sprite;
            }

            // 아이템 아이콘 표시
            if (currentItem != null && currentItem.itemIcon != null)
            {
                previewImage.sprite = currentItem.itemIcon;
            }

            previewImage.color = dragColor;
            previewImage.raycastTarget = false;

            // 크기 설정
            RectTransform previewRect = dragPreview.GetComponent<RectTransform>();
            RectTransform originalRect = GetComponent<RectTransform>();
            previewRect.sizeDelta = originalRect.sizeDelta;
        }

        // 최상단에 표시
        dragPreview.transform.SetAsLastSibling();
    }

    /// <summary>
    /// 드롭 대상 확인
    /// </summary>
    private void CheckDropTarget(PointerEventData eventData)
    {
        if (currentItem == null || currentItem.IsEmpty())
            return;

        // 레이캐스트로 드롭 대상 찾기
        var results = new System.Collections.Generic.List<RaycastResult>();
        if (graphicRaycaster != null)
        {
            graphicRaycaster.Raycast(eventData, results);
        }

        foreach (var result in results)
        {
            // ChestBoxUI 패널인지 확인
            ChestBoxUI chestUI = result.gameObject.GetComponentInParent<ChestBoxUI>();
            if (chestUI != null)
            {
                Debug.Log($"[DraggableInventorySlot] 상자에 드롭: {currentItem.itemName}");
                TryDropToChest(chestUI);
                return;
            }
        }

        Debug.Log($"[DraggableInventorySlot] 유효하지 않은 드롭 위치");
    }

    /// <summary>
    /// 상자에 아이템 드롭 시도
    /// </summary>
    private void TryDropToChest(ChestBoxUI chestUI)
    {
        if (currentItem == null || currentItem.IsEmpty())
        {
            Debug.Log("[DraggableInventorySlot] currentItem이 비어있음 - 드롭 취소");
            return;
        }

        if (isAnySlotProcessing)
        {
            Debug.LogWarning(
                "[DraggableInventorySlot] 이미 다른 요청 처리 중입니다 - 중복 요청 방지"
            );
            return;
        }

        // 전역 처리 상태 설정
        isAnySlotProcessing = true;
        int currentRequestId = ++requestCounter;

        // 이동할 아이템 정보 미리 저장 (로컬 상태 변경 전에)
        string itemToMove = currentItem.itemName;
        Debug.Log(
            $"[DraggableInventorySlot] 상자로 아이템 이동 시도: {itemToMove} x1 (슬롯 {slotIndex}) [요청ID: {currentRequestId}]"
        );

        // 서버에 아이템 이동 요청
        if (ServerSimulator.Instance != null)
        {
            string chestId = chestUI.GetCurrentChestId();
            if (!string.IsNullOrEmpty(chestId))
            {
                ServerSimulator.Instance.RequestPutItemToChest(chestId, itemToMove, 1);
                Debug.Log(
                    $"[DraggableInventorySlot] 서버 요청 전송: {itemToMove} → {chestId} [요청ID: {currentRequestId}]"
                );

                // 로컬 상태는 서버 응답 후에 업데이트되므로 여기서 수정하지 않음
                // currentItem = null; // 제거

                // PlayerInventory에서 서버 응답 시 즉시 플래그 해제됨
            }
            else
            {
                Debug.LogError("[DraggableInventorySlot] 상자 ID를 가져올 수 없습니다!");
                isAnySlotProcessing = false;
            }
        }
        else
        {
            Debug.LogError("[DraggableInventorySlot] ServerSimulator를 찾을 수 없습니다!");
            isAnySlotProcessing = false; // 에러 시에도 플래그 해제
        }
    }

    /// <summary>
    /// 모든 드래그 가능한 슬롯의 데이터 새로고침
    /// </summary>
    private void RefreshAllDraggableSlots()
    {
        // PlayerInventoryUI의 모든 DraggableInventorySlot 찾기
        PlayerInventoryUI inventoryUI = FindFirstObjectByType<PlayerInventoryUI>();
        if (inventoryUI != null)
        {
            DraggableInventorySlot[] allDraggableSlots =
                inventoryUI.GetComponentsInChildren<DraggableInventorySlot>();
            foreach (var slot in allDraggableSlots)
            {
                slot.UpdateSlotData();
            }

            Debug.Log(
                $"[DraggableInventorySlot] {allDraggableSlots.Length}개 슬롯 데이터 새로고침"
            );
        }
    }

    /// <summary>
    /// 서버 액션 응답 처리 - Put 액션에 대한 즉시 플래그 해제
    /// </summary>
    private void OnServerActionResponse(object responseObj)
    {
        if (responseObj == null)
            return;

        // 리플렉션으로 ItemActionResponse 처리
        var responseType = responseObj.GetType();

        // action 필드 가져오기
        var actionField = responseType.GetField("action");
        var successField = responseType.GetField("success");

        if (actionField == null || successField == null)
            return;

        var actionValue = actionField.GetValue(responseObj);
        var successValue = (bool)successField.GetValue(responseObj);

        // Put 액션인지 확인 (인벤토리→상자 이동)
        if (actionValue.ToString() == "Put")
        {
            Debug.Log(
                $"[DraggableInventorySlot] Put 액션 응답 수신 - 성공: {successValue}, 플래그 즉시 해제"
            );

            // 즉시 플래그 해제
            isAnySlotProcessing = false;
        }
    }
}
