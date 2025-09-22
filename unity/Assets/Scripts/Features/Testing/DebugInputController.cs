using UnityEngine;
using VContainer;
using Features.MobileControls.Testing;

namespace Features.Testing
{
    /// <summary>
    /// 디버그용 입력 컨트롤러 - 테스트 환경에서 키보드 입력 지원
    /// </summary>
    public class DebugInputController : MonoBehaviour
    {
        [Header("Components")]
        private KeyboardDebugController keyboardDebugController;

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

            // 키보드 디버그 컨트롤러 추가
            keyboardDebugController = GetComponent<KeyboardDebugController>();
            if (keyboardDebugController == null)
            {
                keyboardDebugController = gameObject.AddComponent<KeyboardDebugController>();
                if (enableDebugLogs)
                    Debug.Log("[DebugInputController] 키보드 디버그 컨트롤러 추가 (WASD 이동, Space 점프)");
            }
        }

        public void SetDebugInputEnabled(bool enabled)
        {
            enableDebugInput = enabled;

            if (keyboardDebugController != null)
            {
                keyboardDebugController.enabled = enabled;
            }

            if (enableDebugLogs)
                Debug.Log($"[DebugInputController] 디버그 입력 {(enabled ? "활성화" : "비활성화")}");
        }

        public bool IsDebugInputEnabled()
        {
            return enableDebugInput && keyboardDebugController != null && keyboardDebugController.enabled;
        }
    }
}