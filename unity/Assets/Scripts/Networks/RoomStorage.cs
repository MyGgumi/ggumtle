using Networks.Rooms;
using UnityEngine;

namespace Networks
{
    public class RoomStorage
    {
        public static RoomStorage Instance { get; } = new();

        private RoomStorage()
        {
        }

        public Room Room { get; private set; }

        // 서버에서 새로운 Room 데이터를 받았는지 추적
        private bool _hasReceivedNewRoomData = false;

        public void UploadRoom(Room room)
        {
            Room = room;
            _hasReceivedNewRoomData = true;
            Debug.Log("[RoomStorage] 새로운 방 데이터 업로드 완료");
        }

        public void ClearRoom()
        {
            Room = null;
            _hasReceivedNewRoomData = false;
            Debug.Log("[RoomStorage] Room 데이터 초기화");
        }

        public bool HasReceivedNewRoomData()
        {
            return Room != null && Room.IsInitialized() && _hasReceivedNewRoomData;
        }
    }
}