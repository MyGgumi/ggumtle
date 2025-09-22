using Cysharp.Threading.Tasks;
using Networks.chests;

namespace Features.Chest.NetworkSources
{
    /// <summary>
    /// 상자 관련 네트워크 통신을 담당하는 인터페이스
    /// Service 레이어와 Network 레이어를 분리하여 테스트 용이성과 유연성 제공
    /// </summary>
    public interface IChestNetworkSource
    {
        /// <summary>
        /// 상자 열기
        /// </summary>
        /// <param name="chestId">상자 ID</param>
        /// <returns>상자 열기 결과</returns>
        UniTask<ChestOpenCommand> OpenChestAsync(int chestId);

        /// <summary>
        /// 상자 닫기
        /// </summary>
        /// <param name="chestId">상자 ID</param>
        /// <returns>상자 닫기 결과</returns>
        UniTask<ChestCloseCommand> CloseChestAsync(int chestId);

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
    }
}