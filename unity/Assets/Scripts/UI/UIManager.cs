using UnityEngine;
using UnityEngine.Rendering;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Toolkit 매니저 참조")]
    public UniversalHUDController universalHUDController;

    void Awake()
    {
        DebugManager.instance.enableRuntimeUI = false;
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
        // UniversalHUDController 자동 찾기
        if (universalHUDController == null)
        {
            universalHUDController = FindFirstObjectByType<UniversalHUDController>();
        }
    }

    public void ShowChestInventory(InteractableChest chest)
    {
        if (universalHUDController != null)
        {
            universalHUDController.ShowChestInventory(chest);
        }
        else
        {
            Debug.LogError("[UIManager] UniversalHUDController가 null입니다!");
        }
    }

    public void CloseAllOverlays()
    {
        if (universalHUDController != null)
        {
            universalHUDController.HideChestBox();
        }

        // ViewModels.UI.InteractionViewModel.Instance?.ClearInteraction(); // InteractionViewModel 삭제로 인해 주석 처리
    }
}
