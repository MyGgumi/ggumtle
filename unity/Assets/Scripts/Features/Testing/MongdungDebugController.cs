using Features.Mongdung.Views;
using Features.Mongdung.Models;
using UnityEngine;

namespace Features.Testing
{
    /// <summary>
    /// 몽둥이 디버그 컨트롤러 - 테스트 환경에서 몽둥이 액션 키보드 입력 지원
    /// Q: Attack, E: TrapSetting, R: Frighten
    /// </summary>
    public class MongdungDebugController : MonoBehaviour
    {
        [Header("몽둥이 액션 키 설정")]
        [SerializeField] private KeyCode attackKey = KeyCode.Q;
        [SerializeField] private KeyCode trapSettingKey = KeyCode.E;
        [SerializeField] private KeyCode frightenKey = KeyCode.R;

        [Header("설정")]
        [SerializeField] private bool enableDebugLogs = false;

        // 컴포넌트 참조
        private MongdungGameObject mongdungGameObject;

        private void Start()
        {
            // MongdungGameObject 찾기
            mongdungGameObject = GetComponent<MongdungGameObject>();
            if (mongdungGameObject == null)
            {
                Debug.LogError($"[MongdungDebugController] MongdungGameObject를 찾을 수 없습니다: {gameObject.name}");
                enabled = false;
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungDebugController] 초기화 완료: Q={attackKey}, E={trapSettingKey}, R={frightenKey}");
            }
        }

        private void Update()
        {
            if (mongdungGameObject == null) return;

            HandleMongdungInput();
        }

        private void HandleMongdungInput()
        {
            // Attack 액션 (Q키)
            if (Input.GetKeyDown(attackKey))
            {
                if (enableDebugLogs)
                    Debug.Log($"[MongdungDebugController] Attack 키 입력: {attackKey}");

                mongdungGameObject.TryExecuteAction(MongdungActionType.Attack);
            }

            // TrapSetting 액션 (E키)
            if (Input.GetKeyDown(trapSettingKey))
            {
                if (enableDebugLogs)
                    Debug.Log($"[MongdungDebugController] TrapSetting 키 입력: {trapSettingKey}");

                mongdungGameObject.TryExecuteAction(MongdungActionType.TrapSetting);
            }

            // Frighten 액션 (R키)
            if (Input.GetKeyDown(frightenKey))
            {
                if (enableDebugLogs)
                    Debug.Log($"[MongdungDebugController] Frighten 키 입력: {frightenKey}");

                mongdungGameObject.TryExecuteAction(MongdungActionType.Frighten);
            }

            // 디버그용 추가 기능들
            HandleDebugCommands();
        }

        private void HandleDebugCommands()
        {
            // Ctrl + 숫자키로 강제 액션 취소
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    mongdungGameObject.CancelAction(MongdungActionType.Attack);
                    if (enableDebugLogs)
                        Debug.Log("[MongdungDebugController] Attack 액션 강제 취소");
                }
                else if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    mongdungGameObject.CancelAction(MongdungActionType.TrapSetting);
                    if (enableDebugLogs)
                        Debug.Log("[MongdungDebugController] TrapSetting 액션 강제 취소");
                }
                else if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    mongdungGameObject.CancelAction(MongdungActionType.Frighten);
                    if (enableDebugLogs)
                        Debug.Log("[MongdungDebugController] Frighten 액션 강제 취소");
                }
            }

            // F1키로 현재 상태 출력
            if (Input.GetKeyDown(KeyCode.F1))
            {
                mongdungGameObject.LogCurrentStatus();
            }
        }

        /// <summary>
        /// 액션 키 설정 변경
        /// </summary>
        public void SetActionKeys(KeyCode attack, KeyCode trapSetting, KeyCode frighten)
        {
            attackKey = attack;
            trapSettingKey = trapSetting;
            frightenKey = frighten;

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungDebugController] 액션 키 변경: Q={attackKey}, E={trapSettingKey}, R={frightenKey}");
            }
        }

        /// <summary>
        /// 특정 액션이 실행 가능한지 UI 표시용
        /// </summary>
        public bool CanExecuteAction(MongdungActionType actionType)
        {
            return mongdungGameObject?.CanExecuteAction(actionType) ?? false;
        }

        /// <summary>
        /// 특정 액션의 남은 쿨다운 시간
        /// </summary>
        public float GetRemainingCooldown(MongdungActionType actionType)
        {
            return mongdungGameObject?.GetRemainingCooldown(actionType) ?? 0f;
        }

        private void OnEnable()
        {
            if (enableDebugLogs)
                Debug.Log("[MongdungDebugController] 몽둥이 디버그 컨트롤러 활성화");
        }

        private void OnDisable()
        {
            if (enableDebugLogs)
                Debug.Log("[MongdungDebugController] 몽둥이 디버그 컨트롤러 비활성화");
        }

        private void OnGUI()
        {
            // 디버그 모드일 때만 GUI 표시
            if (!enableDebugLogs || mongdungGameObject == null) return;

            // 화면 우상단에 몽둥이 액션 상태 표시
            GUILayout.BeginArea(new Rect(Screen.width - 300, 10, 280, 120));
            GUILayout.BeginVertical("box");

            GUILayout.Label("=== 몽둥이 액션 디버그 ===", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            // 각 액션 상태 표시
            DisplayActionStatus("Attack (Q)", MongdungActionType.Attack);
            DisplayActionStatus("TrapSetting (E)", MongdungActionType.TrapSetting);
            DisplayActionStatus("Frighten (R)", MongdungActionType.Frighten);

            GUILayout.Label("F1: 상태 출력, Ctrl+1/2/3: 액션 취소", new GUIStyle(GUI.skin.label) { fontSize = 10 });

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DisplayActionStatus(string actionName, MongdungActionType actionType)
        {
            bool canExecute = CanExecuteAction(actionType);
            float cooldown = GetRemainingCooldown(actionType);

            string status = canExecute ? "Ready" : $"Cooldown: {cooldown:F1}s";
            Color color = canExecute ? Color.green : Color.red;

            var style = new GUIStyle(GUI.skin.label) { normal = { textColor = color } };
            GUILayout.Label($"{actionName}: {status}", style);
        }
    }
}