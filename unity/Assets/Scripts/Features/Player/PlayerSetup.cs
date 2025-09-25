using Features.Player.Views;
using Features.Player.Systems;
using Networks.Rooms.Domains;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// 플레이어 GameObject 자동 설정 스크립트
    /// 필요한 모든 컴포넌트를 자동으로 추가하고 설정
    /// PlayerPacket 데이터 기반 초기화 지원
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerSetup : MonoBehaviour
    {
        // 서버 속도 값을 Unity 속도로 변환하는 상수
        private const float SERVER_SPEED_TO_UNITY_SPEED = 2.5f / 100f; // 서버 100 = Unity 2.5
        [Header("Setup Settings")]
        [SerializeField]
        private bool autoSetup = true;

        [SerializeField]
        private bool setupOnAwake = true;

        [Header("Player Data")]
        [SerializeField]
        private PlayerPacket _playerPacket;

        [Header("Debug Settings")]
        [SerializeField]
        private bool _enableDebugLogs = false;

        void Awake()
        {
            if (setupOnAwake && autoSetup)
            {
                SetupPlayer();
            }
        }

        [ContextMenu("Setup Player Components")]
        public void SetupPlayer()
        {
            if (_enableDebugLogs)
                Debug.Log("[PlayerSetup] 플레이어 컴포넌트 설정 시작...");

            // 1. Player 태그 설정
            if (!gameObject.CompareTag("Player"))
            {
                gameObject.tag = "Player";
                if (_enableDebugLogs)
                    Debug.Log("[PlayerSetup] Player 태그 설정 완료");
            }

            // 2. CharacterController 확인
            var characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                // 기본 설정
                characterController.center = new Vector3(0, 1, 0);
                characterController.height = 2;
                characterController.radius = 0.5f;
                if (_enableDebugLogs)
                    Debug.Log("[PlayerSetup] CharacterController 추가 완료");
            }

            // 3. 플레이어 타입별 컴포넌트 설정
            SetupPlayerTypeComponents();

            // 4. Animator 확인 (선택사항)
            var animator = GetComponent<Animator>();
            if (animator == null && _enableDebugLogs)
            {
                Debug.LogWarning("[PlayerSetup] Animator가 없습니다. 애니메이션이 작동하지 않을 수 있습니다.");
            }

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerSetup] ===== 플레이어 설정 완료 =====");
                LogCurrentComponents();
            }
        }

        /// <summary>
        /// PlayerPacket 데이터로 플레이어 초기화
        /// </summary>
        public void InitializeFromPacket(PlayerPacket packet)
        {
            if (packet == null)
            {
                Debug.LogError("[PlayerSetup] PlayerPacket이 null입니다!");
                return;
            }

            _playerPacket = packet;

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerSetup] PlayerPacket으로 초기화 시작: ID={packet.Id}, 타입={GetPlayerTypeString(packet)}");
            }

            // 기본 컴포넌트 설정
            SetupPlayer();

            // 패킷 데이터 적용
            ApplyPacketData(packet);

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerSetup] PlayerPacket 초기화 완료: ID={packet.Id}");
            }
        }

        /// <summary>
        /// 플레이어 타입별 컴포넌트 설정
        /// </summary>
        private void SetupPlayerTypeComponents()
        {
            bool isLocal = _playerPacket?.IsMine ?? true; // 기본값은 로컬
            bool isMongging = _playerPacket?.IsMongging ?? true; // 기본값은 몽깅이

            // 기존 GameObject 컴포넌트 확인 및 정리
            var existingPlayerGameObject = GetComponent<PlayerGameObject>();
            var existingRemotePlayerGameObject = GetComponent<RemotePlayerGameObject>();

            if (isLocal)
            {
                // 원격 플레이어 컴포넌트가 있으면 제거
                if (existingRemotePlayerGameObject != null)
                {
                    DestroyImmediate(existingRemotePlayerGameObject);
                    if (_enableDebugLogs)
                        Debug.Log("[PlayerSetup] 기존 RemotePlayerGameObject 제거");
                }

                // 로컬 플레이어 컴포넌트 추가
                if (existingPlayerGameObject == null)
                {
                    var playerGameObject = gameObject.AddComponent<PlayerGameObject>();
                    playerGameObject.GroundLayers = LayerMask.GetMask("Default"); // GroundLayer를 Default로 설정
                    if (_enableDebugLogs)
                        Debug.Log("[PlayerSetup] PlayerGameObject (Local) 추가 완료, GroundLayers=Default");
                }
            }
            else
            {
                // 로컬 플레이어 컴포넌트가 있으면 제거
                if (existingPlayerGameObject != null)
                {
                    DestroyImmediate(existingPlayerGameObject);
                    if (_enableDebugLogs)
                        Debug.Log("[PlayerSetup] 기존 PlayerGameObject 제거");
                }

                // 원격 플레이어 컴포넌트 추가
                if (existingRemotePlayerGameObject == null)
                {
                    var remotePlayerGameObject = gameObject.AddComponent<RemotePlayerGameObject>();
                    remotePlayerGameObject.GroundLayers = LayerMask.GetMask("Default"); // GroundLayer를 Default로 설정
                    if (_enableDebugLogs)
                        Debug.Log("[PlayerSetup] RemotePlayerGameObject 추가 완료, GroundLayers=Default");
                }
            }

            // 기존 시스템 컴포넌트 확인 및 정리
            var existingMonggingSystem = GetComponent<MonggingSystem>();
            var existingMongdungSystem = GetComponent<MongdungSystem>();

            // 몽깅이/몽둥이 시스템 컴포넌트 관리
            if (isMongging)
            {
                // 몽둥이 시스템이 있으면 제거
                if (existingMongdungSystem != null)
                {
                    DestroyImmediate(existingMongdungSystem);
                    if (_enableDebugLogs)
                        Debug.Log("[PlayerSetup] 기존 MongdungSystem 제거");
                }

                // 몽깅이 시스템 추가
                if (existingMonggingSystem == null)
                {
                    gameObject.AddComponent<MonggingSystem>();
                    if (_enableDebugLogs)
                        Debug.Log("[PlayerSetup] MonggingSystem 추가 완료");
                }
            }
            else
            {
                // 몽깅이 시스템이 있으면 제거
                if (existingMonggingSystem != null)
                {
                    DestroyImmediate(existingMonggingSystem);
                    if (_enableDebugLogs)
                        Debug.Log("[PlayerSetup] 기존 MonggingSystem 제거");
                }

                // 몽둥이 시스템 추가
                if (existingMongdungSystem == null)
                {
                    gameObject.AddComponent<MongdungSystem>();
                    if (_enableDebugLogs)
                        Debug.Log("[PlayerSetup] MongdungSystem 추가 완료");
                }
            }
        }

        /// <summary>
        /// PlayerPacket 데이터를 컴포넌트에 적용
        /// </summary>
        private void ApplyPacketData(PlayerPacket packet)
        {
            // 서버 속도를 Unity 속도로 변환
            float convertedSpeed = packet.MoveSpeed * SERVER_SPEED_TO_UNITY_SPEED;

            if (packet.IsMine)
            {
                // 로컬 플레이어: PlayerGameObject 설정
                var playerGameObject = GetComponent<PlayerGameObject>();
                if (playerGameObject != null)
                {
                    playerGameObject.MoveSpeed = convertedSpeed;
                    if (_enableDebugLogs)
                        Debug.Log($"[PlayerSetup] PlayerGameObject 속도 설정: 서버={packet.MoveSpeed} → Unity={convertedSpeed}");
                }
            }
            else
            {
                // 원격 플레이어: RemotePlayerGameObject 설정
                var remotePlayerGameObject = GetComponent<RemotePlayerGameObject>();
                if (remotePlayerGameObject != null)
                {
                    remotePlayerGameObject.InitializeFromPacket(packet);
                    if (_enableDebugLogs)
                        Debug.Log($"[PlayerSetup] RemotePlayerGameObject 초기화 완료");
                }
            }

            // MonggingSystem 설정
            var monggingSystem = GetComponent<MonggingSystem>();
            if (monggingSystem != null && packet.IsMongging)
            {
                monggingSystem.InitializeFromPacket(packet);
            }

            // MongdungSystem 설정
            var mongdungSystem = GetComponent<MongdungSystem>();
            if (mongdungSystem != null && !packet.IsMongging)
            {
                mongdungSystem.InitializeFromPacket(packet);
            }

            // GameObject 이름 설정
            string playerType = GetPlayerTypeString(packet);
            string localPrefix = packet.IsMine ? "Local" : "Remote";
            gameObject.name = $"{localPrefix}_{playerType}_{packet.Id}";
        }

        /// <summary>
        /// 플레이어 타입 문자열 반환
        /// </summary>
        private string GetPlayerTypeString(PlayerPacket packet)
        {
            if (!packet.IsMongging) return "Mongdung";

            return packet.ClassId switch
            {
                0 => "Mongging_Tanker",   // 서버에서 0부터 시작
                1 => "Mongging_Healer",
                2 => "Mongging_Worker",
                _ => "Mongging_Unknown"
            };
        }

        /// <summary>
        /// 현재 컴포넌트 상태 로깅
        /// </summary>
        private void LogCurrentComponents()
        {
            var characterController = GetComponent<CharacterController>();
            var playerGameObject = GetComponent<PlayerGameObject>();
            var remotePlayerGameObject = GetComponent<RemotePlayerGameObject>();
            var monggingSystem = GetComponent<MonggingSystem>();
            var mongdungSystem = GetComponent<MongdungSystem>();
            var animator = GetComponent<Animator>();

            Debug.Log($"[PlayerSetup] 현재 컴포넌트 목록:");
            Debug.Log($"  - CharacterController: {characterController != null}");
            Debug.Log($"  - PlayerGameObject: {playerGameObject != null}");
            Debug.Log($"  - RemotePlayerGameObject: {remotePlayerGameObject != null}");
            Debug.Log($"  - MonggingSystem: {monggingSystem != null}");
            Debug.Log($"  - MongdungSystem: {mongdungSystem != null}");
            Debug.Log($"  - Animator: {animator != null}");
            Debug.Log($"  - Tag: {gameObject.tag}");
        }

        [ContextMenu("Check Player Status")]
        public void CheckPlayerStatus()
        {
            Debug.Log("[PlayerSetup] ===== 플레이어 상태 체크 =====");

            var playerGameObject = GetComponent<PlayerGameObject>();
            if (playerGameObject != null)
            {
                Debug.Log($"[PlayerSetup] PlayerGameObject:");
                Debug.Log($"  - canMove: {playerGameObject.canMove}");
                Debug.Log($"  - Grounded: {playerGameObject.Grounded}");
                Debug.Log($"  - MoveSpeed: {playerGameObject.MoveSpeed}");
                Debug.Log($"  - JumpHeight: {playerGameObject.JumpHeight}");
            }
        }
    }
}
