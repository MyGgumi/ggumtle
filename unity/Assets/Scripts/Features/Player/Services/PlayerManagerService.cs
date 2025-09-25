using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Networks.Rooms.Domains;
using Features.PlayerList.Models;
using Features.PlayerList.Services;
using Features.Player.Systems;
using Features.Player.Views;
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

        // 플레이어 데이터 저장소
        private readonly Dictionary<long, PlayerPacket> _playerPackets = new();
        private readonly Dictionary<long, GameObject> _playerObjects = new();
        private readonly Dictionary<long, PlayerInfo> _playerInfos = new();

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
        private readonly bool _enableDebugLogs = true;

        [Inject]
        public PlayerManagerService(
            IPlayerListService playerListService,
            IPublisher<PlayerStateChangedMessage> stateChangePublisher)
        {
            _playerListService = playerListService;
            _stateChangePublisher = stateChangePublisher;

            if (_enableDebugLogs)
            {
                Debug.Log("[PlayerManagerService] 서비스 초기화 완료");
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
            int uiPlayerId = (int)playerId;
            if (uiPlayerId >= 1 && uiPlayerId <= 4)
            {
                _playerListService.SetPlayerOnlineStatus(uiPlayerId, false);
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
            int uiPlayerId = (int)playerId;
            if (uiPlayerId >= 1 && uiPlayerId <= 4)
            {
                _playerListService.SetPlayerStatus(uiPlayerId, newStatus);
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
            int uiPlayerId = (int)packet.Id;

            // PlayerList는 1-4 ID만 지원
            if (uiPlayerId < 1 || uiPlayerId > 4)
            {
                if (_enableDebugLogs)
                    Debug.Log($"[PlayerManagerService] PlayerList UI 범위 외 ID: {packet.Id}");
                return;
            }

            // 색상 테마 결정 (ClassId 기반)
            string colorTheme = GetColorTheme(packet);

            _playerListService.UpdatePlayer(
                uiPlayerId,
                packet.NickName,
                colorTheme,
                "default",
                true, // isOnline
                packet.IsMine // isHost (로컬 플레이어를 호스트로)
            );

            if (_enableDebugLogs)
            {
                Debug.Log($"[PlayerManagerService] PlayerList UI 동기화: ID={uiPlayerId}, 닉네임={packet.NickName}, 색상={colorTheme}");
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

        public void Dispose()
        {
            _totalPlayersReactive?.Dispose();
            _alivePlayersReactive?.Dispose();

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