using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("오버레이 UI들")]
    public ChestInventoryUI chestInventoryUI;
    public GameObject shopUI; // 나중에 상점 UI
    public GameObject dialogueUI; // 나중에 대화 UI
    public GameObject inventoryUI; // 플레이어 인벤토리 UI

    // 활성화된 오버레이들을 추적
    private Stack<GameObject> activeOverlays = new Stack<GameObject>();
    private Dictionary<System.Type, GameObject> uiComponents =
        new Dictionary<System.Type, GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUIComponents();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 모든 오버레이 초기 비활성화
        CloseAllOverlays();
    }

    void Update()
    {
        // ESC 키로 최상단 오버레이 닫기
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTopOverlay();
        }
#endif
    }

    private void InitializeUIComponents()
    {
        // 자동으로 UI 컴포넌트들 찾아서 등록
        if (chestInventoryUI != null)
            uiComponents[typeof(ChestInventoryUI)] = chestInventoryUI.gameObject;
    }

    public void ShowOverlay(GameObject overlay)
    {
        if (overlay == null)
            return;

        // 이미 활성화되어 있으면 최상단으로 이동
        if (overlay.activeInHierarchy)
        {
            BringToFront(overlay);
            return;
        }

        // 오버레이 활성화
        overlay.SetActive(true);
        activeOverlays.Push(overlay);

        // Canvas Order 설정 (최상단에 표시)
        Canvas canvas = overlay.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 100 + activeOverlays.Count;
        }

        Debug.Log($"오버레이 표시: {overlay.name}");
    }

    public void CloseOverlay(GameObject overlay)
    {
        if (overlay == null || !overlay.activeInHierarchy)
            return;

        overlay.SetActive(false);

        // 스택에서 제거
        var tempStack = new Stack<GameObject>();
        while (activeOverlays.Count > 0)
        {
            var current = activeOverlays.Pop();
            if (current != overlay)
            {
                tempStack.Push(current);
            }
        }

        // 나머지 오버레이들 다시 스택에 추가
        while (tempStack.Count > 0)
        {
            activeOverlays.Push(tempStack.Pop());
        }

        Debug.Log($"오버레이 닫음: {overlay.name}");
    }

    public void CloseTopOverlay()
    {
        if (activeOverlays.Count > 0)
        {
            GameObject topOverlay = activeOverlays.Pop();
            if (topOverlay != null)
            {
                topOverlay.SetActive(false);
                Debug.Log($"최상단 오버레이 닫음: {topOverlay.name}");
            }
        }

        // 모든 오버레이가 닫혔으면 상호작용 종료
        if (activeOverlays.Count == 0)
        {
            InteractionManager.Instance?.EndCurrentInteraction();
        }
    }

    public void CloseAllOverlays()
    {
        while (activeOverlays.Count > 0)
        {
            GameObject overlay = activeOverlays.Pop();
            if (overlay != null)
            {
                overlay.SetActive(false);
            }
        }

        // 개별 UI들도 명시적으로 닫기
        if (chestInventoryUI != null)
            chestInventoryUI.HideChestInventory();

        Debug.Log("모든 오버레이 닫음");
    }

    private void BringToFront(GameObject overlay)
    {
        // 해당 오버레이를 스택 최상단으로 이동
        var tempStack = new Stack<GameObject>();
        bool found = false;

        while (activeOverlays.Count > 0)
        {
            var current = activeOverlays.Pop();
            if (current == overlay)
            {
                found = true;
                break;
            }
            tempStack.Push(current);
        }

        // 나머지 오버레이들 다시 추가
        while (tempStack.Count > 0)
        {
            activeOverlays.Push(tempStack.Pop());
        }

        // 찾은 오버레이를 최상단에 추가
        if (found)
        {
            activeOverlays.Push(overlay);

            // Canvas Order 업데이트
            Canvas canvas = overlay.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 100 + activeOverlays.Count;
            }
        }
    }

    // 타입별 UI 접근 메서드들
    public T GetUIComponent<T>()
        where T : MonoBehaviour
    {
        if (uiComponents.TryGetValue(typeof(T), out GameObject uiObject))
        {
            return uiObject.GetComponent<T>();
        }
        return null;
    }

    public void ShowChestInventory(InteractableChest chest)
    {
        if (chestInventoryUI != null)
        {
            ShowOverlay(chestInventoryUI.gameObject);
            chestInventoryUI.ShowChestInventory(chest);
        }
    }

    public void ShowShop()
    {
        if (shopUI != null)
        {
            ShowOverlay(shopUI);
        }
    }

    public void ShowDialogue()
    {
        if (dialogueUI != null)
        {
            ShowOverlay(dialogueUI);
        }
    }

    public void ShowPlayerInventory()
    {
        if (inventoryUI != null)
        {
            ShowOverlay(inventoryUI);
        }
    }

    // 현재 활성화된 오버레이 정보
    public bool HasActiveOverlay()
    {
        return activeOverlays.Count > 0;
    }

    public GameObject GetTopOverlay()
    {
        return activeOverlays.Count > 0 ? activeOverlays.Peek() : null;
    }

    public int GetActiveOverlayCount()
    {
        return activeOverlays.Count;
    }
}
