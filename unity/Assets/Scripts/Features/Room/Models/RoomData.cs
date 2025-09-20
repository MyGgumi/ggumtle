using Networks.Rooms;
using Networks.Rooms.Domains;
using Features.Map.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace Features.Room.Models
{
    /// <summary>
    /// 방 데이터를 관리하는 확장된 모델
    /// 기존 Room 클래스를 래핑하여 추가 기능 제공
    /// </summary>
    public class RoomData
    {
        private readonly Networks.Rooms.Room _originalRoom;
        private readonly bool _enableDebugLogs = true;

        public RoomData(Networks.Rooms.Room room)
        {
            _originalRoom = room ?? new Networks.Rooms.Room();

            if (_enableDebugLogs)
                Debug.Log("[RoomData] RoomData 생성 완료");
        }

        /// <summary>
        /// 원본 Room 객체 참조
        /// </summary>
        public Networks.Rooms.Room OriginalRoom => _originalRoom;

        /// <summary>
        /// 상자 데이터 목록
        /// </summary>
        public List<ChestPacket> Chests => _originalRoom.chests;

        /// <summary>
        /// 꿈틀이 데이터 목록
        /// </summary>
        public List<GgumtlePacket> Ggumtles => _originalRoom.ggumtles;

        /// <summary>
        /// 힐팩 데이터 목록
        /// </summary>
        public List<HealPackPacket> HealPacks => _originalRoom.healPacks;

        /// <summary>
        /// 스피드팩 데이터 목록
        /// </summary>
        public List<SpeedPackPacket> SpeedPacks => _originalRoom.speedPacks;

        /// <summary>
        /// 플레이어 데이터 목록
        /// </summary>
        public List<PlayerPacket> Players => _originalRoom.players;

        /// <summary>
        /// 방이 초기화되었는지 확인
        /// </summary>
        public bool IsInitialized => _originalRoom.IsInitialized();

        /// <summary>
        /// 맵 데이터로 초기화
        /// </summary>
        public void InitializeMap(InitializeMapCommand command)
        {
            _originalRoom.InitMap(command);

            if (_enableDebugLogs)
            {
                Debug.Log($"[SERVER_ROOM_DATA] RoomData 맵 초기화 완료:");
                Debug.Log($"[SERVER_ROOM_DATA]   - 상자: {Chests?.Count ?? 0}개");
                Debug.Log($"[SERVER_ROOM_DATA]   - 꿈틀이: {Ggumtles?.Count ?? 0}개");
                Debug.Log($"[SERVER_ROOM_DATA]   - 힐팩: {HealPacks?.Count ?? 0}개");
                Debug.Log($"[SERVER_ROOM_DATA]   - 스피드팩: {SpeedPacks?.Count ?? 0}개");

                // 꿈틀이 상세 정보
                if (Ggumtles != null && Ggumtles.Count > 0)
                {
                    Debug.Log($"[SERVER_ROOM_DATA] RoomData 꿈틀이 상세 정보:");
                    for (int i = 0; i < Ggumtles.Count; i++)
                    {
                        var ggumtle = Ggumtles[i];
                        var unityPos = ggumtle.ToVector3();
                        Debug.Log($"[SERVER_ROOM_DATA]   [{i}] ID: {ggumtle.Id}, Unity Position: {unityPos}");
                    }
                }
            }
        }

        /// <summary>
        /// 플레이어 데이터로 초기화
        /// </summary>
        public void InitializePlayers(InitializePlayerCommand command)
        {
            _originalRoom.InitPlayer(command);

            if (_enableDebugLogs)
            {
                Debug.Log($"[RoomData] 플레이어 초기화 완료: {Players?.Count ?? 0}명");
            }
        }

        /// <summary>
        /// 꿈틀이 추가
        /// </summary>
        public void AddGgumtle(GgumtlePacket ggumtle)
        {
            if (Ggumtles == null)
            {
                Debug.LogError("[RoomData] Ggumtles 리스트가 초기화되지 않음");
                return;
            }

            Ggumtles.Add(ggumtle);

            if (_enableDebugLogs)
                Debug.Log($"[RoomData] 꿈틀이 추가: ID={ggumtle.Id}, Position={ggumtle.ToVector3()}");
        }

        /// <summary>
        /// 꿈틀이 제거
        /// </summary>
        public bool RemoveGgumtle(int ggumtleId)
        {
            if (Ggumtles == null)
            {
                Debug.LogError("[RoomData] Ggumtles 리스트가 초기화되지 않음");
                return false;
            }

            var removed = Ggumtles.RemoveAll(g => g.Id == ggumtleId);

            if (_enableDebugLogs && removed > 0)
                Debug.Log($"[RoomData] 꿈틀이 제거: ID={ggumtleId}, 제거된 개수={removed}");

            return removed > 0;
        }

        /// <summary>
        /// 특정 ID의 꿈틀이 찾기
        /// </summary>
        public GgumtlePacket FindGgumtle(int ggumtleId)
        {
            if (Ggumtles == null)
                return null;

            return Ggumtles.Find(g => g.Id == ggumtleId);
        }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public void DebugLogRoomInfo()
        {
            Debug.Log($"[RoomData] 방 정보:");
            Debug.Log($"  - 초기화 상태: {IsInitialized}");
            Debug.Log($"  - 상자: {Chests?.Count ?? 0}개");
            Debug.Log($"  - 꿈틀이: {Ggumtles?.Count ?? 0}개");
            Debug.Log($"  - 힐팩: {HealPacks?.Count ?? 0}개");
            Debug.Log($"  - 스피드팩: {SpeedPacks?.Count ?? 0}개");
            Debug.Log($"  - 플레이어: {Players?.Count ?? 0}명");
        }
    }
}