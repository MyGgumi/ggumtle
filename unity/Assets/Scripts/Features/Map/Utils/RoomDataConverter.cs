using Features.Room.Models;
using Networks.Rooms;
using System.Linq;
using UnityEngine;

namespace Features.Map.Utils
{
    /// <summary>
    /// Networks.Rooms.Room을 Features.Room.Models.RoomData로 변환하는 유틸리티
    /// </summary>
    public static class RoomDataConverter
    {
        /// <summary>
        /// Room 객체를 RoomData로 변환
        /// </summary>
        public static RoomData ConvertToRoomData(Networks.Rooms.Room room)
        {
            if (room == null)
            {
                Debug.LogError("[RoomDataConverter] Room이 null입니다.");
                return null;
            }

            if (!room.IsInitialized())
            {
                Debug.LogError("[RoomDataConverter] Room이 초기화되지 않았습니다.");
                return null;
            }

            var roomData = new RoomData(room);

            Debug.Log($"[SERVER_ROOM_DATA] Room -> RoomData 변환 완료: " +
                     $"Chests={room.chests?.Count ?? 0}, " +
                     $"Ggumtles={room.ggumtles?.Count ?? 0}, " +
                     $"HealPacks={room.healPacks?.Count ?? 0}, " +
                     $"SpeedPacks={room.speedPacks?.Count ?? 0}, " +
                     $"Players={room.players?.Count ?? 0}");

            // 꿈틀이 변환 상세 정보
            if (room.ggumtles != null && room.ggumtles.Count > 0)
            {
                Debug.Log($"[SERVER_ROOM_DATA] RoomDataConverter 꿈틀이 변환 상세:");
                for (int i = 0; i < room.ggumtles.Count; i++)
                {
                    var ggumtle = room.ggumtles[i];
                    var unityPos = ggumtle.ToVector3();
                    Debug.Log($"[SERVER_ROOM_DATA]   [{i}] ID: {ggumtle.Id}, Position: {ggumtle.Position} -> Unity: {unityPos}");
                }
            }

            return roomData;
        }

        /// <summary>
        /// RoomData가 유효한지 검증
        /// </summary>
        public static bool ValidateRoomData(RoomData roomData)
        {
            if (roomData == null)
            {
                Debug.LogError("[RoomDataConverter] RoomData가 null입니다.");
                return false;
            }

            // 최소한 하나 이상의 데이터가 있어야 함
            bool hasData = (roomData.Chests != null && roomData.Chests.Count > 0) ||
                          (roomData.Ggumtles != null && roomData.Ggumtles.Count > 0) ||
                          (roomData.HealPacks != null && roomData.HealPacks.Count > 0) ||
                          (roomData.SpeedPacks != null && roomData.SpeedPacks.Count > 0);

            if (!hasData)
            {
                Debug.LogWarning("[RoomDataConverter] RoomData에 스폰할 오브젝트가 없습니다.");
            }

            return true; // 빈 맵도 유효할 수 있음
        }
    }
}