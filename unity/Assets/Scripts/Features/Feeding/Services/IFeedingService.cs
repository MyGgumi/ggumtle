using System;
using Features.Feeding.Models;
using R3;

namespace Features.Feeding.Services
{
    /// <summary>
    /// 꿈틀이 먹이 관리 서비스 인터페이스
    /// </summary>
    public interface IFeedingService : IDisposable
    {
        /// <summary>
        /// 현재 먹이 데이터 (읽기 전용)
        /// </summary>
        ReadOnlyReactiveProperty<FeedingData> CurrentFeeding { get; }

        /// <summary>
        /// 현재 먹이 개수
        /// </summary>
        int FeedingCount { get; }

        /// <summary>
        /// 먹이 추가
        /// </summary>
        /// <param name="amount">추가할 개수</param>
        /// <returns>실제 추가된 개수</returns>
        int AddFeeding(int amount);

        /// <summary>
        /// 먹이 제거
        /// </summary>
        /// <param name="amount">제거할 개수</param>
        /// <returns>성공 여부</returns>
        bool RemoveFeeding(int amount);

        /// <summary>
        /// 먹이 개수 설정
        /// </summary>
        /// <param name="count">설정할 개수</param>
        void SetFeedingCount(int count);

        /// <summary>
        /// 충분한 먹이가 있는지 확인
        /// </summary>
        /// <param name="requiredAmount">필요한 개수</param>
        /// <returns>충분한지 여부</returns>
        bool HasEnoughFeeding(int requiredAmount);

        /// <summary>
        /// 꿈틀이에게 먹이주기 (상호작용)
        /// </summary>
        /// <param name="amount">먹이줄 개수</param>
        /// <returns>성공 여부</returns>
        bool FeedToGgumtle(int amount);

        /// <summary>
        /// 서버와 동기화
        /// </summary>
        void SyncWithServer();

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        void ClearFeeding();
    }
}