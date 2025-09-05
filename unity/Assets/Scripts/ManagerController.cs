using UnityEngine;

/// <summary>
/// 모든 매니저들을 담는 컨테이너 오브젝트 관리
/// </summary>
public class ManagerController : MonoBehaviour
{
    public static ManagerController Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Managers 오브젝트 전체를 유지
            Debug.Log("[ManagerController] 매니저 컨테이너 초기화 완료");
        }
        else
        {
            Destroy(gameObject);
        }
    }
}