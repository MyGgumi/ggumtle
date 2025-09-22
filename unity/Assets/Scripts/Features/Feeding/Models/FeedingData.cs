using System;

namespace Features.Feeding.Models
{
    /// <summary>
    /// 꿈틀이 먹이(빛 젤리) 데이터
    /// </summary>
    [Serializable]
    public class FeedingData
    {
        /// <summary>
        /// 현재 보유 먹이 개수
        /// </summary>
        public int FeedingCount { get; set; } = 0;

        /// <summary>
        /// 최대 보유 가능 개수
        /// </summary>
        public int MaxFeedingCount { get; set; } = 999;

        /// <summary>
        /// 먹이가 있는지 확인
        /// </summary>
        public bool HasFeeding => FeedingCount > 0;

        /// <summary>
        /// 충분한 먹이가 있는지 확인
        /// </summary>
        public bool HasEnoughFeeding(int requiredAmount)
        {
            return FeedingCount >= requiredAmount;
        }

        /// <summary>
        /// 먹이 추가
        /// </summary>
        public int AddFeeding(int amount)
        {
            if (amount <= 0) return 0;

            int actualAdded = Math.Min(amount, MaxFeedingCount - FeedingCount);
            FeedingCount += actualAdded;
            return actualAdded;
        }

        /// <summary>
        /// 먹이 제거
        /// </summary>
        public bool RemoveFeeding(int amount)
        {
            if (amount <= 0 || FeedingCount < amount) return false;

            FeedingCount -= amount;
            return true;
        }

        /// <summary>
        /// 먹이 개수 설정
        /// </summary>
        public void SetFeedingCount(int count)
        {
            FeedingCount = Math.Clamp(count, 0, MaxFeedingCount);
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Clear()
        {
            FeedingCount = 0;
        }

        /// <summary>
        /// 데이터 복사
        /// </summary>
        public FeedingData Clone()
        {
            return new FeedingData
            {
                FeedingCount = FeedingCount,
                MaxFeedingCount = MaxFeedingCount
            };
        }

        public override string ToString()
        {
            return $"FeedingData(Count: {FeedingCount}/{MaxFeedingCount})";
        }
    }
}