using System;
using Features.Map.Services;
using Features.Room.Services;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Scenes.Loading.ViewModels
{
    /// <summary>
    /// 로딩 씬 UI 상태를 관리하는 ViewModel
    /// </summary>
    public class LoadingViewModel : IDisposable
    {
        private readonly IAddressableLoadService _addressableLoadService;
        private readonly IRoomService _roomService;
        private readonly R3.DisposableBag _disposables = new();
        private readonly bool _enableDebugLogs = true;

        // UI 상태
        private readonly ReactiveProperty<float> _overallProgress = new(0f);
        private readonly ReactiveProperty<string> _statusMessage = new("로딩 준비 중...");
        private readonly ReactiveProperty<bool> _isLoading = new(false);
        private readonly ReactiveProperty<bool> _isWaitingForGameStart = new(false);
        private readonly ReactiveProperty<bool> _isComplete = new(false);


        public ReadOnlyReactiveProperty<float> OverallProgress => _overallProgress;
        public ReadOnlyReactiveProperty<string> StatusMessage => _statusMessage;
        public ReadOnlyReactiveProperty<bool> IsLoading => _isLoading;
        public ReadOnlyReactiveProperty<bool> IsWaitingForGameStart => _isWaitingForGameStart;
        public ReadOnlyReactiveProperty<bool> IsComplete => _isComplete;

        [Inject]
        public LoadingViewModel(
            IAddressableLoadService addressableLoadService,
            IRoomService roomService
        )
        {
            _addressableLoadService = addressableLoadService ?? throw new ArgumentNullException(nameof(addressableLoadService));
            _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));

            if (_enableDebugLogs)
                Debug.Log("[LoadingViewModel] 초기화 완료");

            SubscribeToServices();
        }

        /// <summary>
        /// 서비스 이벤트 구독
        /// </summary>
        private void SubscribeToServices()
        {
            // Addressable 로딩 진행률
            _addressableLoadService.OnLoadProgress += OnLoadProgressUpdated;

            // Addressable 로딩 완료
            _addressableLoadService.OnAllAssetsLoaded += OnAllAssetsLoaded;

            // 게임 시작 이벤트
            _roomService.OnGameStarted += OnGameStarted;

            if (_enableDebugLogs)
                Debug.Log("[LoadingViewModel] 서비스 이벤트 구독 완료");
        }

        /// <summary>
        /// 로딩 시작
        /// </summary>
        public async void StartLoading(string assetTag = "InGame")
        {
            try
            {
                _isLoading.Value = true;
                _statusMessage.Value = "게임 에셋 로딩 중...";
                _overallProgress.Value = 0f;

                if (_enableDebugLogs)
                    Debug.Log($"[LoadingViewModel] 로딩 시작: {assetTag}");

                // Addressable 에셋 로딩
                await _addressableLoadService.LoadAssetsWithTagAsync(
                    assetTag,
                    OnLoadProgressUpdated
                );
            }
            catch (Exception e)
            {
                _statusMessage.Value = $"로딩 실패: {e.Message}";
                _isLoading.Value = false;
                Debug.LogError($"[LoadingViewModel] 로딩 실패: {e.Message}");
            }
        }

        /// <summary>
        /// 로딩 진행률 업데이트
        /// </summary>
        private void OnLoadProgressUpdated(float progress)
        {
            // 전체 진행률 계산: 에셋 로딩은 0~90% 구간에 매핑
            var overallProgress = progress * 0.9f;
            _overallProgress.Value = overallProgress;

            var percentage = Mathf.RoundToInt(overallProgress * 100);
            _statusMessage.Value = $"게임 에셋 로딩 중... {percentage}%";

            if (_enableDebugLogs)
                Debug.Log($"[LoadingViewModel] 전체 진행률: {overallProgress:P0}");
        }

        /// <summary>
        /// 모든 에셋 로딩 완료
        /// </summary>
        private void OnAllAssetsLoaded()
        {
            _isLoading.Value = false;
            _isWaitingForGameStart.Value = true;
            _statusMessage.Value = "다른 플레이어를 기다리는 중...";

            // 전체 진행률을 90%로 설정 (에셋 로딩 완료, 게임 시작 대기 단계)
            _overallProgress.Value = 0.9f;

            if (_enableDebugLogs)
                Debug.Log("[LoadingViewModel] 에셋 로딩 완료 - 게임 시작 대기, 전체 진행률: 90%");
        }

        /// <summary>
        /// 게임 시작 이벤트 수신
        /// </summary>
        private void OnGameStarted()
        {
            _isWaitingForGameStart.Value = false;
            _isComplete.Value = true;
            _statusMessage.Value = "게임 시작! 메인 씬으로 이동 중...";

            // 전체 진행률을 100%로 설정 (게임 시작)
            _overallProgress.Value = 1.0f;

            if (_enableDebugLogs)
                Debug.Log("[LoadingViewModel] 게임 시작 신호 수신");

            // 로딩 완료 이벤트 발행
            OnLoadingCompleted?.Invoke();
        }

        /// <summary>
        /// 로딩 완료 이벤트
        /// </summary>
        public event Action OnLoadingCompleted;

        /// <summary>
        /// 수동으로 게임 시작 (디버그용)
        /// </summary>
        public void ForceGameStart()
        {
            if (_enableDebugLogs)
                Debug.Log("[LoadingViewModel] 강제 게임 시작");

            OnGameStarted();
        }

        /// <summary>
        /// 현재 방 정보 가져오기
        /// </summary>
        public void GetRoomInfo()
        {
            var room = _roomService.CurrentRoom;
            if (room != null)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[LoadingViewModel] 현재 방 정보:");
                    room.DebugLogRoomInfo();
                }
            }
            else
            {
                Debug.LogWarning("[LoadingViewModel] 방 데이터가 없음");
            }
        }

        public void Dispose()
        {
            // 서비스 이벤트 구독 해제
            if (_addressableLoadService != null)
            {
                _addressableLoadService.OnLoadProgress -= OnLoadProgressUpdated;
                _addressableLoadService.OnAllAssetsLoaded -= OnAllAssetsLoaded;
            }

            if (_roomService != null)
            {
                _roomService.OnGameStarted -= OnGameStarted;
            }

            _disposables.Dispose();

            if (_enableDebugLogs)
                Debug.Log("[LoadingViewModel] 해제됨");
        }
    }
}
