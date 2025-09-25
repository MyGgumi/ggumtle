using UnityEngine;
using VContainer;
using Features.MobileControls.Testing;
using Features.Mongdung.Views;

namespace Features.Testing
{
    /// <summary>
    /// 디버그용 입력 컨트롤러 - 테스트 환경에서 키보드 입력 지원
    /// </summary>
    public class DebugInputController : MonoBehaviour
    {
        [Header("Components")]
        private KeyboardDebugController keyboardDebugController;
        private MongdungDebugController mongdungDebugController;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugInput = true;
        [SerializeField]
        private bool enableDebugLogs = true;

        void Awake()
        {
            InitializeDebugControllers();
        }

        private void InitializeDebugControllers()
        {
            if (!enableDebugInput) return;

            // 키보드 디버그 컨트롤러 추가 (모든 플레이어)
            keyboardDebugController = GetComponent<KeyboardDebugController>();
            if (keyboardDebugController == null)
            {
                keyboardDebugController = gameObject.AddComponent<KeyboardDebugController>();
                if (enableDebugLogs)
                    Debug.Log("[DebugInputController] 키보드 디버그 컨트롤러 추가 (WASD 이동, Space 점프)");
            }

            // 몽둥이 디버그 컨트롤러 추가 (몽둥이 플레이어만)
            if (IsMongdungPlayer())
            {
                mongdungDebugController = GetComponent<MongdungDebugController>();
                if (mongdungDebugController == null)
                {
                    mongdungDebugController = gameObject.AddComponent<MongdungDebugController>();
                    if (enableDebugLogs)
                        Debug.Log("[DebugInputController] 몽둥이 디버그 컨트롤러 추가 (Q: Attack, E: TrapSetting, R: Frighten)");
                }
            }
        }

        public void SetDebugInputEnabled(bool enabled)
        {
            enableDebugInput = enabled;

            if (keyboardDebugController != null)
            {
                keyboardDebugController.enabled = enabled;
            }

            if (mongdungDebugController != null)
            {
                mongdungDebugController.enabled = enabled;
            }

            if (enableDebugLogs)
                Debug.Log($"[DebugInputController] 디버그 입력 {(enabled ? "활성화" : "비활성화")}");
        }

        public bool IsDebugInputEnabled()
        {
            return enableDebugInput && keyboardDebugController != null && keyboardDebugController.enabled;
        }

        /// <summary>
        /// 몽둥이 플레이어인지 확인
        /// </summary>
        private bool IsMongdungPlayer()
        {
            // MongdungGameObject 컴포넌트가 있으면 몽둥이 플레이어
            var mongdungGameObject = GetComponent<MongdungGameObject>();
            if (mongdungGameObject != null) return true;

            // GameObject 이름으로 확인 (몽둥이는 "Mongdung"이 포함됨)
            if (gameObject.name.Contains("Mongdung")) return true;

            // 몽깅이가 아닌 플레이어를 몽둥이로 간주 (IsMongging이 false)
            // 이 부분은 실제 플레이어 타입 확인 로직으로 대체 필요
            return false;
        }

        /// <summary>
        /// 몽둥이 디버그 컨트롤러 가져오기 (외부에서 액세스용)
        /// </summary>
        public MongdungDebugController GetMongdungDebugController()
        {
            return mongdungDebugController;
        }
    }
}