using Features.Room.Models;
using Features.Map.Utils;
using Networks.Rooms;
using Networks.Rooms.Domains;
using System;
using UnityEngine;

namespace Features.Room.Services
{
    /// <summary>
    /// 방 데이터 관리 서비스 구현체
    /// 기존 RoomStorage를 대체하여 DI와 이벤트 기반으로 관리
    /// </summary>
    public class RoomServiceImpl : IRoomService
    {
        private RoomData _currentRoom;
        private readonly bool _enableDebugLogs = true;

        public RoomData CurrentRoom => _currentRoom;

        public event Action<RoomData> OnRoomChanged;
        public event Action<RoomData> OnMapInitialized;
        public event Action<RoomData> OnPlayersInitialized;
        public event Action OnGameStarted;

        public RoomServiceImpl()
        {
            if (_enableDebugLogs)
                Debug.Log("[RoomService] 초기화 완료");
        }

        public void SetCurrentRoom(Networks.Rooms.Room room)
        {
            if (room == null)
            {
                Debug.LogError("[RoomService] null Room을 설정하려고 시도");
                return;
            }

            _currentRoom = new RoomData(room);

            if (_enableDebugLogs)
                Debug.Log("[RoomService] 방 데이터 설정 완료");

            OnRoomChanged?.Invoke(_currentRoom);
        }

        public void InitializeMap(InitializeMapCommand command)
        {
            if (_currentRoom == null)
            {
                Debug.LogError("[RoomService] 방 데이터가 설정되지 않음");
                return;
            }

            _currentRoom.InitializeMap(command);

            if (_enableDebugLogs)
            {
                Debug.Log("[RoomService] 맵 초기화 완료");
                _currentRoom.DebugLogRoomInfo();
            }

            OnMapInitialized?.Invoke(_currentRoom);
        }

        public void InitializePlayers(InitializePlayerCommand command)
        {
            if (_currentRoom == null)
            {
                Debug.LogError("[RoomService] 방 데이터가 설정되지 않음");
                return;
            }

            _currentRoom.InitializePlayers(command);

            if (_enableDebugLogs)
                Debug.Log($"[RoomService] 플레이어 초기화 완료: {command.players?.Count ?? 0}명");

            OnPlayersInitialized?.Invoke(_currentRoom);
        }

        public void StartGame()
        {
            if (_currentRoom == null)
            {
                Debug.LogError("[RoomService] 방 데이터가 설정되지 않음");
                return;
            }

            if (!_currentRoom.IsInitialized)
            {
                Debug.LogError("[RoomService] 방이 완전히 초기화되지 않음");
                return;
            }

            if (_enableDebugLogs)
                Debug.Log("[RoomService] 게임 시작!");

            OnGameStarted?.Invoke();
        }

        public void SpawnGgumtle(int id, UnityEngine.Vector3 position)
        {
            if (_currentRoom == null)
            {
                Debug.LogError("[RoomService] 방 데이터가 설정되지 않음");
                return;
            }

            var (x, y, z) = position.ToXYZ();
            var ggumtlePacket = new GgumtlePacket(id, x, y, z);

            _currentRoom.AddGgumtle(ggumtlePacket);

            if (_enableDebugLogs)
                Debug.Log($"[RoomService] 꿈틀이 스폰: ID={id}, Position={position}");
        }

        public void RemoveGgumtle(int id)
        {
            if (_currentRoom == null)
            {
                Debug.LogError("[RoomService] 방 데이터가 설정되지 않음");
                return;
            }

            bool removed = _currentRoom.RemoveGgumtle(id);

            if (_enableDebugLogs && removed)
                Debug.Log($"[RoomService] 꿈틀이 제거: ID={id}");
        }

        public bool IsRoomInitialized()
        {
            return _currentRoom?.IsInitialized ?? false;
        }

        public void ClearRoom()
        {
            _currentRoom = null;

            if (_enableDebugLogs)
                Debug.Log("[RoomService] 방 데이터 정리 완료");

            OnRoomChanged?.Invoke(null);
        }
    }
}