using System.Collections.Generic;

namespace Features.Chest.Messages
{
    /// <summary>
    /// 서버로부터 받은 상자 데이터를 동기화하기 위한 메시지
    /// </summary>
    public readonly struct ChestServerDataSyncMessage
    {
        public readonly int ChestId;
        public readonly List<int> Items;

        public ChestServerDataSyncMessage(int chestId, List<int> items)
        {
            ChestId = chestId;
            Items = items;
        }
    }
}