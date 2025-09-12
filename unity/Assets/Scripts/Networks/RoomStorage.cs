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

        public void UploadRoom(Room room)
        {
            Room = room;
            Debug.Log("방 업로드함");
        }
    }
}