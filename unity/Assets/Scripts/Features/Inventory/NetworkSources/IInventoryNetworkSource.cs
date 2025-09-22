using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.Inventory.NetworkSources
{
    /// <summary>
    /// 인벤토리 관련 네트워크 통신을 담당하는 인터페이스
    /// Service 레이어와 Network 레이어를 분리하여 테스트 용이성과 유연성 제공
    /// </summary>
    public interface IInventoryNetworkSource
    {
        /// <summary>
        /// 상자에서 아이템 가져오기
        /// </summary>
        /// <param name="chestId">상자 ID</param>
        /// <param name="slotIndex">슬롯 인덱스</param>
        void GetItemFromChest(int chestId, int slotIndex);

        /// <summary>
        /// 상자에 아이템 넣기
        /// </summary>
        /// <param name="itemId">아이템 ID</param>
        void PutItemToChest(int itemId);

        /// <summary>
        /// 아이템 사용
        /// </summary>
        /// <param name="itemId">아이템 ID</param>
        /// <param name="direction">사용 방향</param>
        /// <returns>사용 성공 여부</returns>
        UniTask<bool> UseItemAsync(int itemId, Vector3 direction);

        /// <summary>
        /// 필드 아이템 사용
        /// </summary>
        /// <param name="fieldItemId">필드 아이템 ID</param>
        /// <returns>사용 성공 여부</returns>
        UniTask<bool> UseFieldItemAsync(int fieldItemId);
    }
}