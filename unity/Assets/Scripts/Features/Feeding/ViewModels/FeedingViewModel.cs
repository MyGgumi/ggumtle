using System;
using Features.Feeding.Models;
using Features.Feeding.Services;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Feeding.ViewModels
{
    /// <summary>
    /// 꿈틀이 먹이 ViewModel
    /// </summary>
    public class FeedingViewModel : IDisposable
    {
        // 서비스 의존성
        [Inject] private readonly IFeedingService _feedingService;

        // 리액티브 프로퍼티
        public readonly ReactiveProperty<FeedingData> CurrentFeeding = new(new FeedingData());
        public readonly ReactiveProperty<int> FeedingCount = new(0);

        // 스프라이트 관리
        public readonly ReactiveProperty<Sprite> FeedingSprite = new();

        // 구독 관리
        private readonly CompositeDisposable _disposables = new();

        [Header("디버그 설정")]
        [SerializeField] private bool enableDebugLogs = true;

        #region Lifecycle

        [Inject]
        public void Initialize()
        {
            // 서비스 상태 구독
            _feedingService.CurrentFeeding
                .Subscribe(data =>
                {
                    CurrentFeeding.Value = data;
                    FeedingCount.Value = data.FeedingCount;

                    if (enableDebugLogs)
                        Debug.Log($"[FeedingViewModel] 먹이 데이터 업데이트: {data}");
                })
                .AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[FeedingViewModel] 초기화 완료");
        }

        public void Dispose()
        {
            _disposables.Dispose();
            CurrentFeeding?.Dispose();
            FeedingCount?.Dispose();
            FeedingSprite?.Dispose();

            if (enableDebugLogs)
                Debug.Log("[FeedingViewModel] 해제 완료");
        }

        #endregion

        #region Public API

        /// <summary>
        /// 먹이 추가
        /// </summary>
        public int AddFeeding(int amount)
        {
            return _feedingService.AddFeeding(amount);
        }

        /// <summary>
        /// 먹이 제거
        /// </summary>
        public bool RemoveFeeding(int amount)
        {
            return _feedingService.RemoveFeeding(amount);
        }

        /// <summary>
        /// 먹이 개수 설정
        /// </summary>
        public void SetFeedingCount(int count)
        {
            _feedingService.SetFeedingCount(count);
        }

        /// <summary>
        /// 충분한 먹이가 있는지 확인
        /// </summary>
        public bool HasEnoughFeeding(int requiredAmount)
        {
            return _feedingService.HasEnoughFeeding(requiredAmount);
        }

        /// <summary>
        /// 꿈틀이에게 먹이주기
        /// </summary>
        public bool FeedToGgumtle(int amount = 1)
        {
            bool success = _feedingService.FeedToGgumtle(amount);

            if (success && enableDebugLogs)
                Debug.Log($"[FeedingViewModel] 꿈틀이에게 먹이 {amount}개 제공 성공");

            return success;
        }

        /// <summary>
        /// 스프라이트 설정
        /// </summary>
        public void SetFeedingSprite(Sprite sprite)
        {
            FeedingSprite.Value = sprite;

            if (enableDebugLogs)
                Debug.Log($"[FeedingViewModel] 먹이 스프라이트 설정: {sprite?.name ?? "null"}");
        }

        /// <summary>
        /// 서버와 동기화
        /// </summary>
        public void SyncWithServer()
        {
            _feedingService.SyncWithServer();
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void ClearFeeding()
        {
            _feedingService.ClearFeeding();
        }

        #endregion

        #region Debug Methods

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugLogCurrentState()
        {
            Debug.Log(
                $"[FeedingViewModel] 현재 상태:\n"
                + $"  FeedingCount: {FeedingCount.Value}\n"
                + $"  HasFeeding: {CurrentFeeding.Value.HasFeeding}\n"
                + $"  FeedingSprite: {FeedingSprite.Value?.name ?? "null"}"
            );
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugAddFeeding(int amount = 10)
        {
            AddFeeding(amount);
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public void DebugFeedToGgumtle(int amount = 1)
        {
            FeedToGgumtle(amount);
        }

        #endregion
    }
}