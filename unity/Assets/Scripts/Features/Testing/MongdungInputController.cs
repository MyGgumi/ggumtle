using Features.Mongdung.Models;
using Features.Mongdung.Messages;
using Features.Player.Views;
using MessagePipe;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Features.Testing
{
    /// <summary>
    /// 몽둥이 액션 입력 컨트롤러 (독립적인 오브젝트)
    /// KeyboardDebugController와 유사하게 씬에 독립적으로 존재
    /// </summary>
    public class MongdungInputController : MonoBehaviour
    {
        [Header("몽둥이 액션 키 설정")]
        [SerializeField] private KeyCode attackKey = KeyCode.Q;
        [SerializeField] private KeyCode trapSettingKey = KeyCode.E;
        [SerializeField] private KeyCode frightenKey = KeyCode.R;

        [Header("설정")]
        [SerializeField] private bool enableMongdungInput = true;
        [SerializeField] private bool enableDebugLogs = true;

        // MessagePipe Publisher (VContainer 주입)
        private IPublisher<MongdungActionRequestMessage> _actionRequestPublisher;

        // 현재 로컬 플레이어 정보
        private long _currentPlayerId = -1;
        private PlayerGameObject _currentLocalPlayer;

        [Inject]
        public void Initialize(IPublisher<MongdungActionRequestMessage> actionRequestPublisher)
        {
            _actionRequestPublisher = actionRequestPublisher;

            if (enableDebugLogs)
            {
                Debug.Log("[MongdungInputController] MessagePipe Publisher 주입 완료");
            }
        }

        void Start()
        {
            // Publisher가 주입되지 않았다면 직접 찾기
            if (_actionRequestPublisher == null)
            {
                TryResolveMessagePipePublisher();
            }

            // 로컬 플레이어 찾기
            FindLocalPlayer();

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungInputController] 초기화 완료: PlayerId={_currentPlayerId}, Q={attackKey}, E={trapSettingKey}, R={frightenKey}");
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
                        Debug.Log("[MongdungInputController] MainLifetimeScope에서 MessagePipe Publisher 찾기 성공");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MongdungInputController] MessagePipe Publisher 찾기 실패: {e.Message}");
            }
        }

        private void FindLocalPlayer()
        {
            // 모든 PlayerGameObject 중에서 로컬 플레이어 찾기
            var allPlayers = FindObjectsByType<PlayerGameObject>(FindObjectsSortMode.None);

            foreach (var player in allPlayers)
            {
                if (player.gameObject.name.Contains("Local"))
                {
                    _currentLocalPlayer = player;
                    _currentPlayerId = player.PlayerId;

                    if (enableDebugLogs)
                    {
                        Debug.Log($"[MongdungInputController] 로컬 플레이어 발견: {player.gameObject.name}, PlayerId={_currentPlayerId}");
                    }
                    return;
                }
            }

            if (enableDebugLogs)
            {
                Debug.LogWarning("[MongdungInputController] 로컬 플레이어를 찾을 수 없습니다");
            }
        }

        void Update()
        {
            if (!enableMongdungInput || _actionRequestPublisher == null)
                return;

            // PlayerId가 유효하지 않으면 다시 찾기 시도
            if (_currentPlayerId <= 0)
            {
                if (Time.frameCount % 60 == 0) // 1초마다 재시도
                {
                    FindLocalPlayer();
                }
                return;
            }

            HandleMongdungInput();
        }

        private void HandleMongdungInput()
        {
            if (_currentLocalPlayer == null) return;

            Vector3 position = _currentLocalPlayer.transform.position;
            Vector3 direction = _currentLocalPlayer.transform.forward;

            // Attack 액션 (Q키)
            if (Input.GetKeyDown(attackKey))
            {
                Debug.Log($"[MongdungInputController] ✅ Attack 키 입력 감지: {attackKey}, PlayerId: {_currentPlayerId}");

                var message = new MongdungActionRequestMessage(
                    _currentPlayerId,
                    MongdungActionType.Attack,
                    position,
                    direction,
                    -1,
                    "KeyboardInput"
                );

                _actionRequestPublisher.Publish(message);
                Debug.Log($"[MongdungInputController] Attack 액션 요청 메시지 발행 완료");
            }

            // TrapSetting 액션 (E키)
            if (Input.GetKeyDown(trapSettingKey))
            {
                Debug.Log($"[MongdungInputController] ✅ TrapSetting 키 입력 감지: {trapSettingKey}, PlayerId: {_currentPlayerId}");

                var message = new MongdungActionRequestMessage(
                    _currentPlayerId,
                    MongdungActionType.TrapSetting,
                    position,
                    direction,
                    -1,
                    "KeyboardInput"
                );

                _actionRequestPublisher.Publish(message);
                Debug.Log($"[MongdungInputController] TrapSetting 액션 요청 메시지 발행 완료");
            }

            // Frighten 액션 (R키)
            if (Input.GetKeyDown(frightenKey))
            {
                Debug.Log($"[MongdungInputController] ✅ Frighten 키 입력 감지: {frightenKey}, PlayerId: {_currentPlayerId}");

                var message = new MongdungActionRequestMessage(
                    _currentPlayerId,
                    MongdungActionType.Frighten,
                    position,
                    direction,
                    -1,
                    "KeyboardInput"
                );

                _actionRequestPublisher.Publish(message);
                Debug.Log($"[MongdungInputController] Frighten 액션 요청 메시지 발행 완료");
            }
        }

        /// <summary>
        /// 몽둥이 입력 활성화/비활성화
        /// </summary>
        public void SetMongdungInputEnabled(bool enabled)
        {
            enableMongdungInput = enabled;

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungInputController] 몽둥이 입력 {(enabled ? "활성화" : "비활성화")}");
            }
        }

        /// <summary>
        /// 디버그 로그 표시 설정
        /// </summary>
        public void SetDebugLogsEnabled(bool enabled)
        {
            enableDebugLogs = enabled;
        }

        private void OnEnable()
        {
            if (enableDebugLogs)
                Debug.Log("[MongdungInputController] 몽둥이 입력 컨트롤러 활성화");
        }

        private void OnDisable()
        {
            if (enableDebugLogs)
                Debug.Log("[MongdungInputController] 몽둥이 입력 컨트롤러 비활성화");
        }
    }
}