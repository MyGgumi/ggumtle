using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Features.ItemUsage.Services
{
    /// <summary>
    /// 아이템 사용 서비스 인터페이스
    /// 테이저건, 섬광탄, 자가제세동기 사용 로직 담당
    /// </summary>
    public interface IItemUsageService : IDisposable
    {
        /// <summary>
        /// 테이저건 사용
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="targetId">대상 ID</param>
        /// <param name="direction">사용 방향</param>
        /// <returns>사용 성공 여부</returns>
        UniTask<bool> UseTaserGunAsync(long userId, long targetId, Vector3 direction);

        /// <summary>
        /// 섬광탄 사용
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="direction">사용 방향</param>
        /// <returns>사용 성공 여부</returns>
        UniTask<bool> UseFlashBangAsync(long userId, Vector3 direction);

        /// <summary>
        /// 자가제세동기 사용
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <returns>사용 성공 여부</returns>
        UniTask<bool> UseSelfDefibrillatorAsync(long userId);

        /// <summary>
        /// 아이템 사용 가능 여부 확인
        /// </summary>
        /// <param name="userId">사용자 ID</param>
        /// <param name="itemId">아이템 ID</param>
        /// <returns>사용 가능 여부</returns>
        bool CanUseItem(long userId, int itemId);

        /// <summary>
        /// 아이템 쿨다운 확인
        /// </summary>
        /// <param name="itemId">아이템 ID</param>
        /// <returns>쿨다운 남은 시간 (초)</returns>
        float GetItemCooldownRemaining(int itemId);
    }
}