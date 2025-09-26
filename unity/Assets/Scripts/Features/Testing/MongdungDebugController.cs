using Features.Mongdung.Views;
using Features.Mongdung.Models;
using Features.Mongdung.Messages;
using MessagePipe;
using UnityEngine;
using VContainer;

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
        [SerializeField] private bool enableDebugLogs = true;

        // 컴포넌트 참조
        private MongdungGameObject mongdungGameObject;

        // MessagePipe Publisher (VContainer 주입)
        private IPublisher<MongdungActionRequestMessage> _actionRequestPublisher;
        private long _playerId = -1;

        [Inject]
        public void Initialize(IPublisher<MongdungActionRequestMessage> actionRequestPublisher)
        {
            _actionRequestPublisher = actionRequestPublisher;

            if (enableDebugLogs)
            {
                Debug.Log("[MongdungDebugController] MessagePipe Publisher 주입 완료");
            }
        }

        private void Start()
        {
            // MongdungGameObject 찾기 (PlayerId 가져오기 위해)
            mongdungGameObject = GetComponent<MongdungGameObject>();
            if (mongdungGameObject == null)
            {
                Debug.LogError($"[MongdungDebugController] MongdungGameObject를 찾을 수 없습니다: {gameObject.name}");
                enabled = false;
                return;
            }

            // PlayerId 가져오기
            _playerId = mongdungGameObject.PlayerId;
            if (_playerId <= 0)
            {
                Debug.LogWarning($"[MongdungDebugController] PlayerId가 설정되지 않음: {_playerId}");
            }

            // Publisher가 주입되지 않았다면 직접 찾기
            if (_actionRequestPublisher == null)
            {
                TryResolveMessagePipePublisher();
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungDebugController] 초기화 완료: PlayerId={_playerId}, Q={attackKey}, E={trapSettingKey}, R={frightenKey}");
            }
        }

        private void TryResolveMessagePipePublisher()
        {
            try
            {
                var lifetimeScope = FindFirstObjectByType<DI.MainLifetimeScope>();
                if (lifetimeScope != null && lifetimeScope.Container != null)
                {
                    _actionRequestPublisher = lifetimeScope.Container.Resolve<IPublisher<MongdungActionRequestMessage>>();
                    if (enableDebugLogs)
                        Debug.Log("[MongdungDebugController] MainLifetimeScope에서 MessagePipe Publisher 찾기 성공");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungDebugController] MessagePipe Publisher 찾기 실패: {e.Message}");
            }
        }

        private void Update()
        {
            if (mongdungGameObject == null)
            {
                if (enableDebugLogs && Time.frameCount % 60 == 0) // 1초마다
                {
                    Debug.LogWarning($"[MongdungDebugController] MongdungGameObject가 null: {gameObject.name}");
                }
                return;
            }

            HandleMongdungInput();
        }

        private void HandleMongdungInput()
        {
            if (_actionRequestPublisher == null)
            {
                if (enableDebugLogs && Time.frameCount % 60 == 0) // 1초마다
                {
                    Debug.LogWarning($"[MongdungDebugController] ActionRequestPublisher가 null: {gameObject.name}");
                }
                return;
            }

            if (_playerId <= 0)
            {
                if (enableDebugLogs && Time.frameCount % 60 == 0) // 1초마다
                {
                    Debug.LogWarning($"[MongdungDebugController] PlayerId가 유효하지 않음: {_playerId}, GameObject: {gameObject.name}");
                }
                return;
            }

            Vector3 position = transform.position;
            Vector3 direction = transform.forward;

            // Attack 액션 (Q키)
            if (Input.GetKeyDown(attackKey))
            {
                Debug.Log($"[MongdungDebugController] ✅ Attack 키 입력 감지: {attackKey}, PlayerId: {_playerId}");

                var message = new MongdungActionRequestMessage(
                    _playerId,
                    MongdungActionType.Attack,
                    position,
                    direction,
                    -1, // targetId는 나중에 AttackHitDetector에서 처리
                    "DebugInput"
                );

                _actionRequestPublisher.Publish(message);
                Debug.Log($"[MongdungDebugController] Attack 액션 요청 메시지 발행 완료");
            }

            // TrapSetting 액션 (E키)
            if (Input.GetKeyDown(trapSettingKey))
            {
                Debug.Log($"[MongdungDebugController] ✅ TrapSetting 키 입력 감지: {trapSettingKey}, PlayerId: {_playerId}");

                var message = new MongdungActionRequestMessage(
                    _playerId,
                    MongdungActionType.TrapSetting,
                    position,
                    direction,
                    -1,
                    "DebugInput"
                );

                _actionRequestPublisher.Publish(message);
                Debug.Log($"[MongdungDebugController] TrapSetting 액션 요청 메시지 발행 완료");
            }

            // Frighten 액션 (R키)
            if (Input.GetKeyDown(frightenKey))
            {
                Debug.Log($"[MongdungDebugController] ✅ Frighten 키 입력 감지: {frightenKey}, PlayerId: {_playerId}");

                var message = new MongdungActionRequestMessage(
                    _playerId,
                    MongdungActionType.Frighten,
                    position,
                    direction,
                    -1,
                    "DebugInput"
                );

                _actionRequestPublisher.Publish(message);
                Debug.Log($"[MongdungDebugController] Frighten 액션 요청 메시지 발행 완료");
            }

            // 디버그용 추가 기능들
            HandleDebugCommands();
        }

        private void HandleDebugCommands()
        {
            // F1키로 현재 상태 출력 (MongdungGameObject 통해)
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (mongdungGameObject != null)
                {
                    mongdungGameObject.LogCurrentStatus();
                }
                else
                {
                    Debug.Log($"[MongdungDebugController] 디버그 정보: PlayerId={_playerId}, GameObject={gameObject.name}");
                }
            }

            // 액션 취소는 MessagePipe로 따로 구현할 예정 (일단 제거)
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

        // CanExecuteAction, GetRemainingCooldown 제거
        // MessagePipe 방식에서는 MongdungGameObject나 UI에서 ViewModel을 통해 상태 확인

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
            // MessagePipe 방식에서는 상태 조회를 직접 할 수 없으므로 간단히 표시
            var style = new GUIStyle(GUI.skin.label) { normal = { textColor = Color.white } };
            GUILayout.Label($"{actionName}: MessagePipe 방식", style);
        }
    }
}