using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Networks;
using Networks.Rooms.Domains;
using Features.PlayerList.Models;
using Features.PlayerList.Services;
using Features.Player.Systems;
using Features.Player.Views;
using Features.Player.Messages;
using VContainer;
using R3;
using MessagePipe;

namespace Features.Player.Services
{
    /// <summary>
    /// 플레이어 데이터 중앙 관리 서비스
    /// PlayerPacket 데이터와 GameObject 매핑을 관리하고
    /// 기존 PlayerList UI 시스템과 연동
    /// </summary>
    public class PlayerManagerService : IDisposable
    {
        private readonly IPlayerListService _playerListService;
        private readonly IPublisher<PlayerStateChangedMessage> _stateChangePublisher;
        private readonly ISubscriber<PlayerMoveResponseMessage> _playerMoveSubscriber;
        private readonly ISubscriber<PlayerJumpMessage> _playerJumpSubscriber;
        private readonly ISubscriber<PlayerAnimationStateMessage> _playerAnimationSubscriber;
        private IDisposable _playerMoveSubscription;
        private IDisposable _playerJumpSubscription;
        private IDisposable _playerAnimationSubscription;

        // 플레이어 데이터 저장소
        private readonly Dictionary<long, PlayerPacket> _playerPackets = new();
        private readonly Dictionary<long, GameObject> _playerObjects = new();
        private readonly Dictionary<long, PlayerInfo> _playerInfos = new();

        // 서버 ID → UI 슬롯 매핑 (0,1,2,3 → 1,2,3,4)
        private readonly Dictionary<long, int> _serverIdToUiSlot = new();
        private int _nextUiSlot = 1; // UI 슬롯은 1부터 시작

        // Observable Properties
        private readonly ReactiveProperty<int> _totalPlayersReactive = new(0);
        private readonly ReactiveProperty<int> _alivePlayersReactive = new(0);

        public ReadOnlyReactiveProperty<int> TotalPlayers => _totalPlayersReactive.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<int> AlivePlayers => _alivePlayersReactive.ToReadOnlyReactiveProperty();

        // Events
        public event Action<long, GameObject> OnPlayerRegistered;
        public event Action<long> OnPlayerRemoved;
        public event Action<long, string> OnPlayerStateChanged;

        [Header("Debug Settings")]
        private readonly bool _enableDebugLogs = false; // 과도한 이동 로그 방지

        [Inject]
        public PlayerManagerService(
            IPlayerListService playerListService,
            IPublisher<PlayerStateChangedMessage> stateChangePublisher,
            ISubscriber<PlayerMoveResponseMessage> playerMoveSubscriber,
            ISubscriber<PlayerJumpMessage> playerJumpSubscriber,
            ISubscriber<PlayerAnimationStateMessage> playerAnimationSubscriber)
        {
            _playerListService = playerListService;
            _stateChangePublisher = stateChangePublisher;
            _playerMoveSubscriber = playerMoveSubscriber;
            _playerJumpSubscriber = playerJumpSubscriber;
            _playerAnimationSubscriber = playerAnimationSubscriber;

            // 메시지 구독
            _playerMoveSubscription = _playerMoveSubscriber.Subscribe(OnPlayerMoveReceived);
            _playerJumpSubscription = _playerJumpSubscriber.Subscribe(OnPlayerJumpReceived);
            _playerAnimationSubscription = _playerAnimationSubscriber.Subscribe(OnPlayerAnimationReceived);

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerManagerService] 서비스 초기화 완료 - 모든 플레이어 메시지 구독 시작");
            }
        }

        /// <summary>
        /// 플레이어 등록 (PlayerSpawnService에서 호출)
        /// </summary>
        public void RegisterPlayer(PlayerPacket packet, GameObject playerObject)
        {
            if (packet == null || playerObject == null)
            {
                Debug.LogError("[PlayerManagerService] RegisterPlayer: null 매개변수");
                return;
            }

            // 기본 정보 저장
            _playerPackets[packet.Id] = packet;
            _playerObjects[packet.Id] = playerObject;

            // PlayerInfo 생성
            var playerInfo = new PlayerInfo
            {
                Id = packet.Id,
                IsMine = packet.IsMine,
                IsMongging = packet.IsMongging,
                ClassId = packet.ClassId,
                NickName = packet.NickName,
                MoveSpeed = packet.MoveSpeed,
                MaxHp = packet.MaxHp,
                HealSpeed = packet.HealSpeed,
                WorkSpeed = packet.WorkSpeed,
                CurrentStatus = "default",
                GameObject = playerObject
            };

            _playerInfos[packet.Id] = playerInfo;

            // PlayerList UI 시스템에 동기화
            SyncToPlayerListUI(packet);

            // 시스템 컴포넌트 이벤트 구독
            SubscribeToPlayerEvents(playerObject, packet);

            // Reactive Property 업데이트
            UpdateCounts();

            OnPlayerRegistered?.Invoke(packet.Id, playerObject);

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerManagerService] 플레이어 등록 완료: ID={packet.Id}, 타입={GetPlayerTypeString(packet)}, 로컬={packet.IsMine}");
            }
        }

        /// <summary>
        /// 플레이어 제거
        /// </summary>
        public bool RemovePlayer(long playerId)
        {
            if (!_playerPackets.ContainsKey(playerId))
            {
                Debug.LogWarning($"[PlayerManagerService] 존재하지 않는 플레이어 제거 시도: {playerId}");
                return false;
            }

            // 데이터 제거
            _playerPackets.Remove(playerId);
            _playerObjects.Remove(playerId);
            _playerInfos.Remove(playerId);

            // PlayerList UI에서도 제거 (오프라인으로 설정)
            if (_serverIdToUiSlot.TryGetValue(playerId, out int uiSlot))
            {
                _playerListService.SetPlayerOnlineStatus(uiSlot, false);
                ReleaseUiSlot(playerId); // 슬롯 해제
            }

            // Reactive Property 업데이트
            UpdateCounts();

            OnPlayerRemoved?.Invoke(playerId);

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerManagerService] 플레이어 제거 완료: ID={playerId}");
            }

            return true;
        }

        /// <summary>
        /// 플레이어 상태 업데이트
        /// </summary>
        public void UpdatePlayerState(long playerId, string newStatus)
        {
            if (!_playerInfos.ContainsKey(playerId))
            {
                Debug.LogWarning($"[PlayerManagerService] 존재하지 않는 플레이어 상태 업데이트: {playerId}");
                return;
            }

            string oldStatus = _playerInfos[playerId].CurrentStatus;
            _playerInfos[playerId].CurrentStatus = newStatus;

            // PlayerList UI 동기화
            if (_serverIdToUiSlot.TryGetValue(playerId, out int uiSlot))
            {
                _playerListService.SetPlayerStatus(uiSlot, newStatus);
            }

            // 메시지 발행
            var message = new PlayerStateChangedMessage(playerId, oldStatus, newStatus);
            _stateChangePublisher.Publish(message);

            // Reactive Property 업데이트 (생존자 수)
            UpdateCounts();

            OnPlayerStateChanged?.Invoke(playerId, newStatus);

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerManagerService] 플레이어 상태 업데이트: ID={playerId}, {oldStatus} → {newStatus}");
            }
        }

        /// <summary>
        /// PlayerList UI 시스템에 데이터 동기화
        /// </summary>
        private void SyncToPlayerListUI(PlayerPacket packet)
        {
            // 서버 ID → UI 슬롯 매핑
            int uiSlot = GetOrAssignUiSlot(packet.Id);

            if (uiSlot == -1)
            {
                if (_enableDebugLogs)
                    Debug.Log($"[PlayerManagerService] UI 슬롯 할당 실패: 서버ID={packet.Id}");
                return;
            }

            // 색상 테마 결정 (ClassId 기반)
            string colorTheme = GetColorTheme(packet);

            _playerListService.UpdatePlayer(
                uiSlot,
                packet.NickName,
                colorTheme,
                "default",
                true, // isOnline
                packet.IsMine // isHost (로컬 플레이어를 호스트로)
            );

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerManagerService] PlayerList UI 동기화: 서버ID={packet.Id} → UI슬롯={uiSlot}, 닉네임={packet.NickName}, 색상={colorTheme}");
            }
        }

        /// <summary>
        /// 플레이어 시스템 컴포넌트 이벤트 구독
        /// </summary>
        private void SubscribeToPlayerEvents(GameObject playerObject, PlayerPacket packet)
        {
            // MonggingSystem 이벤트 구독
            var monggingSystem = playerObject.GetComponent<MonggingSystem>();
            if (monggingSystem != null)
            {
                monggingSystem.OnStatusChanged += (oldStatus, newStatus) =>
                {
                    UpdatePlayerState(packet.Id, monggingSystem.GetStatusString());
                };
            }

            // MongdungSystem 이벤트 구독
            var mongdungSystem = playerObject.GetComponent<MongdungSystem>();
            if (mongdungSystem != null)
            {
                mongdungSystem.OnStatusChanged += (oldStatus, newStatus) =>
                {
                    UpdatePlayerState(packet.Id, mongdungSystem.GetStatusString());
                };
            }
        }

        /// <summary>
        /// 색상 테마 결정
        /// </summary>
        private string GetColorTheme(PlayerPacket packet)
        {
            if (packet.IsMongging)
            {
                // 몽깅이는 ClassId 기반 (서버에서 0부터 시작)
                return packet.ClassId switch
                {
                    0 => "yellow",  // Tanker
                    1 => "mint",    // Healer
                    2 => "pink",    // Worker
                    _ => "yellow"
                };
            }
            else
            {
                // 몽둥이는 파란색
                return "blue";
            }
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
        /// 카운트 업데이트
        /// </summary>
        private void UpdateCounts()
        {
            _totalPlayersReactive.Value = _playerInfos.Count;

            int aliveCount = _playerInfos.Values.Count(p =>
                p.CurrentStatus != "dead" && p.CurrentStatus != "escape");
            _alivePlayersReactive.Value = aliveCount;
        }

        // Public 메서드들

        /// <summary>
        /// 플레이어 정보 가져오기
        /// </summary>
        public PlayerInfo GetPlayerInfo(long playerId)
        {
            return _playerInfos.TryGetValue(playerId, out var info) ? info : null;
        }

        /// <summary>
        /// 플레이어 GameObject 가져오기
        /// </summary>
        public GameObject GetPlayerObject(long playerId)
        {
            return _playerObjects.TryGetValue(playerId, out var obj) ? obj : null;
        }

        /// <summary>
        /// 모든 플레이어 정보 가져오기
        /// </summary>
        public List<PlayerInfo> GetAllPlayers()
        {
            return _playerInfos.Values.ToList();
        }

        /// <summary>
        /// 로컬 플레이어 정보 가져오기
        /// </summary>
        public PlayerInfo GetLocalPlayer()
        {
            return _playerInfos.Values.FirstOrDefault(p => p.IsMine);
        }

        /// <summary>
        /// 생존한 플레이어들 가져오기
        /// </summary>
        public List<PlayerInfo> GetAlivePlayers()
        {
            return _playerInfos.Values.Where(p =>
                p.CurrentStatus != "dead" && p.CurrentStatus != "escape").ToList();
        }

        /// <summary>
        /// 특정 타입의 플레이어들 가져오기
        /// </summary>
        public List<PlayerInfo> GetPlayersByType(bool isMongging)
        {
            return _playerInfos.Values.Where(p => p.IsMongging == isMongging).ToList();
        }

        /// <summary>
        /// 플레이어 역할 가져오기
        /// </summary>
        public PlayerRole GetPlayerRole(long playerId)
        {
            if (!_playerInfos.TryGetValue(playerId, out var playerInfo))
            {
                Debug.LogWarning($"[PlayerManagerService] 존재하지 않는 플레이어의 역할 조회: {playerId}");
                return PlayerRole.Mongging; // 기본값
            }

            return playerInfo.IsMongging ? PlayerRole.Mongging : PlayerRole.Mongdung;
        }

        /// <summary>
        /// 로컬 플레이어의 역할을 가져옵니다 (Mongdung/Mongging)
        /// RoomStorage에서 서버 원본 데이터를 직접 사용
        /// </summary>
        public PlayerRole GetLocalPlayerRole()
        {
            var roomStorage = RoomStorage.Instance;
            if (roomStorage?.Room?.players == null)
            {
                Debug.LogError("[PlayerManagerService] RoomStorage에 플레이어 데이터가 없습니다");
                return PlayerRole.Mongging; // 기본값
            }

            var localPlayer = roomStorage.Room.players.FirstOrDefault(p => p.IsMine);
            if (localPlayer == null)
            {
                Debug.LogError("[PlayerManagerService] RoomStorage에서 로컬 플레이어를 찾을 수 없습니다");
                return PlayerRole.Mongging; // 기본값
            }

            Debug.Log($"[PlayerManagerService] RoomStorage에서 로컬 플레이어 역할 확인: IsMongging={localPlayer.IsMongging}");
            return localPlayer.IsMongging ? PlayerRole.Mongging : PlayerRole.Mongdung;
        }

        /// <summary>
        /// 원격 플레이어의 이동 정보 업데이트
        /// </summary>
        public bool UpdateRemotePlayerTransform(long playerId, Vector3 position, Vector3 direction, bool isMoving, float speed)
        {
            var playerObject = GetPlayerObject(playerId);
            if (playerObject == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerManagerService] 원격 플레이어 GameObject를 찾을 수 없습니다: ID={playerId}");
                }
                return false;
            }

            // RemotePlayerGameObject 찾기
            var remotePlayerGameObject = playerObject.GetComponent<Features.Player.Views.RemotePlayerGameObject>();
            if (remotePlayerGameObject == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerManagerService] RemotePlayerGameObject 컴포넌트를 찾을 수 없습니다: ID={playerId}");
                }
                return false;
            }

            // 네트워크 이동 정보 업데이트 (direction 사용)
            remotePlayerGameObject.UpdateNetworkTransform(position, direction, isMoving, speed);

            if (_enableDebugLogs && Time.frameCount % 300 == 0) // 5초마다 로그
            {
                Debug.Log($"[PlayerManagerService] 원격 플레이어 이동 업데이트: ID={playerId}, Position={position}");
            }

            return true;
        }

        /// <summary>
        /// 원격 플레이어의 점프 정보 업데이트
        /// </summary>
        public bool UpdateRemotePlayerJump(long playerId, bool isJumping)
        {
            var playerObject = GetPlayerObject(playerId);
            if (playerObject == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerManagerService] 원격 플레이어 GameObject를 찾을 수 없습니다: ID={playerId}");
                }
                return false;
            }

            var remotePlayerGameObject = playerObject.GetComponent<Features.Player.Views.RemotePlayerGameObject>();
            if (remotePlayerGameObject == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerManagerService] RemotePlayerGameObject 컴포넌트를 찾을 수 없습니다: ID={playerId}");
                }
                return false;
            }

            remotePlayerGameObject.UpdateNetworkJump(isJumping);

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerManagerService] 원격 플레이어 점프 업데이트: ID={playerId}, IsJumping={isJumping}");
            }

            return true;
        }

        /// <summary>
        /// 원격 플레이어의 애니메이션 트리거
        /// </summary>
        public bool TriggerRemotePlayerAnimation(long playerId, string triggerName)
        {
            var playerObject = GetPlayerObject(playerId);
            if (playerObject == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerManagerService] 원격 플레이어 GameObject를 찾을 수 없습니다: ID={playerId}");
                }
                return false;
            }

            var remotePlayerGameObject = playerObject.GetComponent<Features.Player.Views.RemotePlayerGameObject>();
            if (remotePlayerGameObject == null)
            {
                if (_enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerManagerService] RemotePlayerGameObject 컴포넌트를 찾을 수 없습니다: ID={playerId}");
                }
                return false;
            }

            remotePlayerGameObject.TriggerAnimation(triggerName);

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerManagerService] 원격 플레이어 애니메이션 트리거: ID={playerId}, Animation={triggerName}");
            }

            return true;
        }

        /// <summary>
        /// PlayerMoveResponseMessage 수신 시 처리
        /// </summary>
        private void OnPlayerMoveReceived(PlayerMoveResponseMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[PlayerManagerService] 플레이어 이동 메시지 수신: ID={message.playerId}, Position={message.position}");
                }

                // 로컬 플레이어 메시지는 무시 (로컬 플레이어는 RemotePlayerGameObject가 아님)
                if (IsLocalPlayer(message.playerId))
                {
                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[PlayerManagerService] 로컬 플레이어 메시지 무시: ID={message.playerId}");
                    }
                    return;
                }

                // UpdateRemotePlayerTransform 메서드 호출
                var success = UpdateRemotePlayerTransform(
                    message.playerId,
                    message.position,
                    message.direction, // rotation에서 direction으로 변경
                    message.isMoving,
                    message.speed
                );

                if (!success && _enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerManagerService] 플레이어 이동 업데이트 실패: ID={message.playerId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerManagerService] 플레이어 이동 메시지 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 로컬 플레이어인지 확인
        /// </summary>
        private bool IsLocalPlayer(long playerId)
        {
            if (!_playerInfos.TryGetValue(playerId, out var playerInfo))
            {
                return false;
            }

            return playerInfo.IsMine;
        }

        /// <summary>
        /// 서버 ID에 대한 UI 슬롯 할당 또는 조회
        /// </summary>
        private int GetOrAssignUiSlot(long serverId)
        {
            // 이미 할당된 슬롯이 있으면 반환
            if (_serverIdToUiSlot.TryGetValue(serverId, out int existingSlot))
            {
                return existingSlot;
            }

            // 새 슬롯 할당 (최대 4개 슬롯)
            if (_nextUiSlot <= 4)
            {
                int assignedSlot = _nextUiSlot++;
                _serverIdToUiSlot[serverId] = assignedSlot;

                if (_enableDebugLogs)
                {
                    Debug.Log($"[PlayerManagerService] UI 슬롯 할당: 서버ID={serverId} → UI슬롯={assignedSlot}");
                }

                return assignedSlot;
            }

            // 슬롯 부족 시 -1 반환
            Debug.LogWarning($"[PlayerManagerService] UI 슬롯 부족: 서버ID={serverId}");
            return -1;
        }

        /// <summary>
        /// 서버 ID의 UI 슬롯 해제
        /// </summary>
        private void ReleaseUiSlot(long serverId)
        {
            if (_serverIdToUiSlot.Remove(serverId))
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[PlayerManagerService] UI 슬롯 해제: 서버ID={serverId}");
                }
            }
        }

        /// <summary>
        /// PlayerJumpMessage 수신 시 처리
        /// </summary>
        private void OnPlayerJumpReceived(PlayerJumpMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[PlayerManagerService] 플레이어 점프 메시지 수신: ID={message.playerId}, IsJumping={message.isJumping}");
                }

                var success = UpdateRemotePlayerJump(message.playerId, message.isJumping);

                if (!success && _enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerManagerService] 플레이어 점프 업데이트 실패: ID={message.playerId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerManagerService] 플레이어 점프 메시지 처리 실패: {e.Message}");
            }
        }

        /// <summary>
        /// PlayerAnimationStateMessage 수신 시 처리
        /// </summary>
        private void OnPlayerAnimationReceived(PlayerAnimationStateMessage message)
        {
            try
            {
                if (_enableDebugLogs)
                {
                    Debug.Log($"[PlayerManagerService] 플레이어 애니메이션 메시지 수신: ID={message.playerId}, State={message.animationState}");
                }

                var success = TriggerRemotePlayerAnimation(message.playerId, message.animationState);

                if (!success && _enableDebugLogs)
                {
                    Debug.LogWarning($"[PlayerManagerService] 플레이어 애니메이션 업데이트 실패: ID={message.playerId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerManagerService] 플레이어 애니메이션 메시지 처리 실패: {e.Message}");
            }
        }

        public void Dispose()
        {
            _totalPlayersReactive?.Dispose();
            _alivePlayersReactive?.Dispose();
            _playerMoveSubscription?.Dispose();
            _playerJumpSubscription?.Dispose();
            _playerAnimationSubscription?.Dispose();

            _playerPackets.Clear();
            _playerObjects.Clear();
            _playerInfos.Clear();

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerManagerService] 서비스 정리 완료");
            }
        }
    }

    /// <summary>
    /// 플레이어 정보 클래스
    /// </summary>
    public class PlayerInfo
    {
        public long Id;
        public bool IsMine;
        public bool IsMongging;
        public long ClassId;
        public string NickName;
        public int MoveSpeed;
        public int MaxHp;
        public int HealSpeed;
        public int WorkSpeed;
        public string CurrentStatus;
        public GameObject GameObject;
    }

    /// <summary>
    /// 플레이어 상태 변경 메시지
    /// </summary>
    public readonly struct PlayerStateChangedMessage
    {
        public readonly long playerId;
        public readonly string oldStatus;
        public readonly string newStatus;

        public PlayerStateChangedMessage(long playerId, string oldStatus, string newStatus)
        {
            this.playerId = playerId;
            this.oldStatus = oldStatus;
            this.newStatus = newStatus;
        }
    }
}