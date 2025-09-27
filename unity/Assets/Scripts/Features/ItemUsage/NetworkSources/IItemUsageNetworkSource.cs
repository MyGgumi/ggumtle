using Cysharp.Threading.Tasks;
using Networks.Players;
using UnityEngine;

namespace Features.ItemUsage.NetworkSources
{
    /// <summary>
    /// 아이템 사용 관련 네트워크 통신 인터페이스
    /// </summary>
    public interface IItemUsageNetworkSource
    {
        /// <summary>
        /// 몽깅이 아이템 사용 (테이저건, 섬광탄, 자가제세동기)
        /// </summary>
        /// <param name="itemId">아이템 ID (2: 섬광탄, 3: 테이저건, 4: 자가제세동기)</param>
        /// <param name="direction">사용 방향</param>
        /// <returns>아이템 사용 응답</returns>
        UniTask<MonggingItemUseCommand> UseItemAsync(int itemId, Vector3 direction);

        /// <summary>
        /// 필드 아이템 사용 (필요시)
        /// </summary>
        /// <param name="fieldItemId">필드 아이템 ID</param>
        /// <returns>필드 아이템 사용 응답</returns>
        UniTask<MonggingFieldItemUseCommand> UseFieldItemAsync(int fieldItemId);

        /// <summary>
        /// 자가제세동기 사용 (전용 네트워크 함수)
        /// </summary>
        /// <returns>자가제세동기 사용 응답 (성공시 HP 포함)</returns>
        UniTask<UseDefibrillatorCommand> UseDefibrillatorAsync();
    }
}