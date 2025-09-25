using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Features.Ggumtle.Messages;
using Features.Ggumtle.Models;
using Features.Ggumtle.NetworkSources;
using MessagePipe;
using UnityEngine;
using VContainer;

namespace Features.Ggumtle.Services
{
    /// <summary>
    /// 꿈틀이 비즈니스 로직 서비스 (순수 C# 클래스)
    /// MonoBehaviour 의존성 제거하여 테스트 가능하고 DI 친화적으로 구현
    /// </summary>
    public class GgumtleServiceImpl : IGgumtleService, IDisposable
    {
        #region Dependencies

        private readonly IPublisher<GgumtleStateChangedMessage> _stateChangePublisher;
        private readonly IPublisher<GgumtleHoldProgressMessage> _holdProgressPublisher;
        private readonly IPublisher<GgumtleFoodAddedMessage> _foodAddedPublisher;
        private readonly IPublisher<Features.Notification.Messages.NotificationMessage> _notificationPublisher;
        private readonly IGgumtleNetworkSource _networkSource;

        // 네트워크 이벤트 구독자들
        private readonly ISubscriber<GgumtleDiggingDoneMessage> _diggingDoneSubscriber;
        private readonly ISubscriber<GgumtleJellyForceQuitMessage> _jellyForceQuitSubscriber;
        private readonly ISubscriber<GgumtleSpawnMessage> _spawnSubscriber;

        #endregion

        #region Private Fields

        private readonly Dictionary<int, GgumtleData> _ggumtleDataMap = new();
        private readonly bool _enableDebugLogs = true;
        private readonly List<IDisposable> _disposables = new();

        #endregion

        #region Constructor

        [Inject]
        public GgumtleServiceImpl(
            IPublisher<GgumtleStateChangedMessage> stateChangePublisher,
            IPublisher<GgumtleHoldProgressMessage> holdProgressPublisher,
            IPublisher<GgumtleFoodAddedMessage> foodAddedPublisher,
            IPublisher<Features.Notification.Messages.NotificationMessage> notificationPublisher,
            IGgumtleNetworkSource networkSource,
            ISubscriber<GgumtleDiggingDoneMessage> diggingDoneSubscriber,
            ISubscriber<GgumtleJellyForceQuitMessage> jellyForceQuitSubscriber,
            ISubscriber<GgumtleSpawnMessage> spawnSubscriber
        )
        {
            _stateChangePublisher = stateChangePublisher;
            _holdProgressPublisher = holdProgressPublisher;
            _foodAddedPublisher = foodAddedPublisher;
            _notificationPublisher = notificationPublisher;
            _networkSource = networkSource;
            _diggingDoneSubscriber = diggingDoneSubscriber;
            _jellyForceQuitSubscriber = jellyForceQuitSubscriber;
            _spawnSubscriber = spawnSubscriber;

            // 네트워크 이벤트 구독
            _disposables.Add(_diggingDoneSubscriber.Subscribe(OnDiggingDoneReceived));
            _disposables.Add(_jellyForceQuitSubscriber.Subscribe(OnJellyForceQuitReceived));
            _disposables.Add(_spawnSubscriber.Subscribe(OnSpawnReceived));

            DebugLog("[GgumtleServiceImpl] 서비스 초기화 완료");
        }

        #endregion

        #region Ggumtle Management

        public void RegisterGgumtle(int ggumtleId, string name, Vector3 position)
        {
            if (_ggumtleDataMap.ContainsKey(ggumtleId))
            {
                DebugLog($"[GgumtleServiceImpl] 이미 등록된 꿈틀이: {ggumtleId}");
                return;
            }

            var data = new GgumtleData(ggumtleId.ToString(), name, position);
            _ggumtleDataMap[ggumtleId] = data;
            DebugLog($"[GgumtleServiceImpl] 꿈틀이 등록: {ggumtleId} - {name}");
        }

        public void RegisterGgumtle(string ggumtleId, string name, Vector3 position)
        {
            if (int.TryParse(ggumtleId, out int id))
            {
                RegisterGgumtle(id, name, position);
            }
            else
            {
                DebugLog($"[GgumtleServiceImpl] 잘못된 꿈틀이 ID 형식: {ggumtleId}");
            }
        }

        public GgumtleData GetGgumtleData(string ggumtleId)
        {
            if (int.TryParse(ggumtleId, out int id))
            {
                return GetGgumtleData(id);
            }
            DebugLog($"[GgumtleServiceImpl] 잘못된 꿈틀이 ID 형식: {ggumtleId}");
            return null;
        }

        public GgumtleData GetGgumtleData(int ggumtleId)
        {
            // 성능 최적화: 자주 호출되는 메서드에서 디버그 로그 제거
            // 필요시 컨텍스트 메뉴나 특정 상황에서만 로그 출력
            return _ggumtleDataMap.TryGetValue(ggumtleId, out GgumtleData data) ? data : null;
        }

        public Dictionary<int, GgumtleData> GetAllGgumtleData()
        {
            return new Dictionary<int, GgumtleData>(_ggumtleDataMap);
        }

        public void UnregisterGgumtle(string ggumtleId)
        {
            if (int.TryParse(ggumtleId, out int id))
            {
                UnregisterGgumtle(id);
            }
        }

        public void UnregisterGgumtle(int ggumtleId)
        {
            if (_ggumtleDataMap.Remove(ggumtleId))
            {
                DebugLog($"[GgumtleServiceImpl] 꿈틀이 제거: {ggumtleId}");
            }
        }

        #endregion

        #region Hold Interaction

        public void StartHold(string ggumtleId)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null)
                return;

            if (data.currentState == GgumtleState.Buried)
            {
                // 파내기 홀드 시작 - Buried → Digging
                data.isHoldInProgress = true;
                data.holdProgress = 0f;
                ChangeGgumtleState(ggumtleId, GgumtleState.Digging);
            }
            else if (data.currentState == GgumtleState.Digging)
            {
                // 이미 Digging 상태 - 홀드만 시작 (상태 변경 없음)
                if (!data.isHoldInProgress)
                {
                    data.isHoldInProgress = true;
                    data.holdProgress = 0f;
                    DebugLog(
                        $"[GgumtleServiceImpl] 이미 Digging 상태 - 홀드만 재시작: {ggumtleId}"
                    );
                }
            }
            else if (data.currentState == GgumtleState.Feeding)
            {
                // 먹이주기 홀드 시작
                data.isHoldInProgress = true;
                data.holdProgress = 0f;
                DebugLog($"[GgumtleServiceImpl] 먹이주기 홀드 시작: {ggumtleId}");
            }
            else
            {
                DebugLog(
                    $"[GgumtleServiceImpl] StartHold 불가능한 상태: {ggumtleId} - {data.currentState}"
                );
            }
        }

        public void UpdateHoldProgress(string ggumtleId, float progress)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null || !data.isHoldInProgress)
                return;

            data.holdProgress = progress;

            // MessagePipe로 진행률 알림
            _holdProgressPublisher.Publish(
                new GgumtleHoldProgressMessage(ggumtleId, progress, data.currentState)
            );
        }

        public void CompleteHold(string ggumtleId)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null || !data.isHoldInProgress)
                return;

            data.isHoldInProgress = false;
            data.holdProgress = 1f;

            if (data.currentState == GgumtleState.Digging)
            {
                // 파내기 완료 - Digging → Emerging
                ChangeGgumtleState(ggumtleId, GgumtleState.Emerging);

                // 2초 후 Feeding 상태로 전환 (코루틴 없이 처리)
                // 실제로는 애니메이션 완료 신호를 받아서 처리하는 것이 좋음
                DelayedStateChange(ggumtleId, GgumtleState.Feeding, 2f);
            }
            else if (data.currentState == GgumtleState.Feeding)
            {
                // 먹이주기 완료
                ProcessFeeding(ggumtleId, data);
            }
        }

        public void CancelHold(string ggumtleId)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null)
                return;

            if (data.isHoldInProgress)
            {
                data.isHoldInProgress = false;
                data.holdProgress = 0f;

                if (data.currentState == GgumtleState.Digging)
                {
                    // 파내기 홀드 취소 - Digging → Buried
                    ChangeGgumtleState(ggumtleId, GgumtleState.Buried);
                }
                else if (data.currentState == GgumtleState.Feeding)
                {
                    // 먹이주기 홀드 취소 - 아무것도 하지 않음
                    DebugLog($"[GgumtleServiceImpl] 먹이주기 홀드 취소: {ggumtleId}");
                }
            }
        }

        #endregion

        #region Feeding System

        public bool HasLightJelly()
        {
            // InventoryService와 연동 필요
            // 임시로 항상 true 반환 (나중에 실제 구현)
            return true;
        }

        public bool FeedGgumtle(string ggumtleId, int amount)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null || data.currentState != GgumtleState.Feeding)
                return false;

            if (!TryConsumeLightJelly(amount))
            {
                // 빛젤리 부족 알림
                _notificationPublisher.Publish(
                    new Features.Notification.Messages.NotificationMessage("빛젤리가 부족합니다!", 2f, Features.Notification.Models.NotificationType.Warning)
                );
                return false;
            }

            // 먹이 추가
            bool isPurified = data.AddFood(amount);

            // 먹이 추가 이벤트 발행
            _foodAddedPublisher.Publish(
                new GgumtleFoodAddedMessage(ggumtleId, data.currentFoodAmount, data.maxFoodRequired)
            );

            DebugLog(
                $"[GgumtleServiceImpl] 먹이 추가: {ggumtleId} ({data.currentFoodAmount}/{data.maxFoodRequired})"
            );

            // 정화 완료 확인
            if (isPurified)
            {
                CompletePurification(ggumtleId);
            }

            return true;
        }

        public void CompletePurification(string ggumtleId)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null)
                return;

            var previousState = data.currentState;
            data.currentState = GgumtleState.Purified;
            data.isFeedingContinuously = false;

            DebugLog($"[GgumtleServiceImpl] 정화 완료: {ggumtleId}");

            // 상태 변경 알림
            _stateChangePublisher.Publish(
                new GgumtleStateChangedMessage(ggumtleId, previousState, GgumtleState.Purified)
            );

            // 정화 완료 알림 (GgumtleStateBroadcastMessage를 통해 처리됨)
        }

        #endregion

        #region Private Methods

        private void ChangeGgumtleState(string ggumtleId, GgumtleState newState)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null)
                return;

            var previousState = data.currentState;
            if (previousState == newState)
            {
                DebugLog(
                    $"[GgumtleServiceImpl] 같은 상태 변경 시도 무시: {ggumtleId} - {newState}"
                );
                return;
            }

            data.currentState = newState;
            DebugLog($"[GgumtleServiceImpl] 상태 변경: {ggumtleId} - {previousState} → {newState}");

            // MessagePipe로 상태 변경 알림
            _stateChangePublisher.Publish(
                new GgumtleStateChangedMessage(ggumtleId, previousState, newState)
            );
        }

        private void ProcessFeeding(string ggumtleId, GgumtleData data)
        {
            if (!FeedGgumtle(ggumtleId, 1))
            {
                DebugLog($"[GgumtleServiceImpl] 먹이주기 실패: {ggumtleId}");
            }
        }

        private bool TryConsumeLightJelly(int amount)
        {
            // TODO: 실제 InventoryService와 연동
            // 임시로 항상 성공
            return true;
        }

        private async void DelayedStateChange(
            string ggumtleId,
            GgumtleState targetState,
            float delay
        )
        {
            // UniTask 사용하여 딜레이 후 상태 변경
            await Cysharp.Threading.Tasks.UniTask.Delay(TimeSpan.FromSeconds(delay));

            var data = GetGgumtleData(ggumtleId);
            if (data != null && data.currentState == GgumtleState.Emerging)
            {
                ChangeGgumtleState(ggumtleId, targetState);
            }
        }

        private void DebugLog(string message)
        {
            if (_enableDebugLogs)
            {
                Debug.Log(message);
            }
        }

        #endregion

        #region Network Integration

        /// <summary>
        /// 네트워크를 통해 꿈틀이 파기 시작
        /// </summary>
        public async UniTask<bool> StartNetworkDiggingAsync(string ggumtleId)
        {
            try
            {
                var data = GetGgumtleData(ggumtleId);
                if (data == null)
                {
                    DebugLog($"[GgumtleServiceImpl] 꿈틀이를 찾을 수 없음: {ggumtleId}");
                    return false;
                }

                // int로 변환 (서버에서는 int ID를 사용)
                if (!int.TryParse(ggumtleId, out int numericId))
                {
                    DebugLog($"[GgumtleServiceImpl] 잘못된 꿈틀이 ID 형식: {ggumtleId}");
                    return false;
                }

                var result = await _networkSource.StartDiggingAsync(numericId);

                if (result.Success)
                {
                    DebugLog($"[GgumtleServiceImpl] 네트워크 파기 시작 성공: {ggumtleId}");
                    StartHold(ggumtleId);
                    return true;
                }
                else
                {
                    DebugLog($"[GgumtleServiceImpl] 네트워크 파기 실패: {result.Result}");
                    HandleDiggingError(result.Result);
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleServiceImpl] 네트워크 파기 시작 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 네트워크를 통해 꿈틀이 파기 중단
        /// </summary>
        public async UniTask<bool> StopNetworkDiggingAsync()
        {
            try
            {
                var result = await _networkSource.QuitDiggingAsync();

                if (result.Success)
                {
                    DebugLog("[GgumtleServiceImpl] 네트워크 파기 중단 성공");
                    // 현재 파고 있던 꿈틀이 찾아서 홀드 취소
                    foreach (var kvp in _ggumtleDataMap)
                    {
                        if (kvp.Value.currentState == GgumtleState.Digging)
                        {
                            CancelHold(kvp.Key.ToString());
                            break;
                        }
                    }
                    return true;
                }
                else
                {
                    DebugLog($"[GgumtleServiceImpl] 네트워크 파기 중단 실패: {result.Result}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleServiceImpl] 네트워크 파기 중단 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 네트워크를 통해 빛젤리 먹이기 시작
        /// </summary>
        public async UniTask<bool> StartNetworkFeedingAsync(string ggumtleId)
        {
            try
            {
                var data = GetGgumtleData(ggumtleId);
                if (data == null)
                {
                    DebugLog($"[GgumtleServiceImpl] 꿈틀이를 찾을 수 없음: {ggumtleId}");
                    return false;
                }

                // int로 변환
                if (!int.TryParse(ggumtleId, out int numericId))
                {
                    DebugLog($"[GgumtleServiceImpl] 잘못된 꿈틀이 ID 형식: {ggumtleId}");
                    return false;
                }

                var result = await _networkSource.StartJellyFeedingAsync(numericId);

                if (result.Success)
                {
                    DebugLog($"[GgumtleServiceImpl] 네트워크 먹이기 시작 성공: {ggumtleId}");
                    // 상태를 Feeding으로 변경
                    ChangeGgumtleState(ggumtleId, GgumtleState.Feeding);
                    return true;
                }
                else
                {
                    DebugLog($"[GgumtleServiceImpl] 네트워크 먹이기 실패: {result.Result}");
                    HandleJellyError(result.Result);
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleServiceImpl] 네트워크 먹이기 시작 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 네트워크를 통해 빛젤리 먹이기 중단
        /// </summary>
        public async UniTask<bool> StopNetworkFeedingAsync()
        {
            try
            {
                var result = await _networkSource.QuitJellyFeedingAsync();

                if (result.Success)
                {
                    DebugLog(
                        $"[GgumtleServiceImpl] 네트워크 먹이기 중단 성공, 남은 젤리: {result.LeftJellyCount}"
                    );
                    // 현재 먹이기 중인 꿈틀이 찾아서 홀드 취소
                    foreach (var kvp in _ggumtleDataMap)
                    {
                        if (kvp.Value.currentState == GgumtleState.Feeding)
                        {
                            CancelHold(kvp.Key.ToString());
                            break;
                        }
                    }
                    return true;
                }
                else
                {
                    DebugLog($"[GgumtleServiceImpl] 네트워크 먹이기 중단 실패: {result.Result}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleServiceImpl] 네트워크 먹이기 중단 오류: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 서버에서 파기 완료 이벤트 처리
        /// </summary>
        public void HandleDiggingDone(int ggumtleId, bool isRealGgumtle)
        {
            var ggumtleIdStr = ggumtleId.ToString();
            DebugLog(
                $"[GgumtleServiceImpl] 파기 완료 이벤트: {ggumtleIdStr}, 진짜 꿈틀이: {isRealGgumtle}"
            );

            if (isRealGgumtle)
            {
                CompleteHold(ggumtleIdStr);
            }
            else
            {
                // 가짜 꿈틀이인 경우
                _notificationPublisher.Publish(
                    new Features.Notification.Messages.NotificationMessage("가짜 꿈틀이였습니다!", 2f, Features.Notification.Models.NotificationType.Info)
                );
                UnregisterGgumtle(ggumtleIdStr);
            }
        }

        /// <summary>
        /// 서버에서 강제 먹이기 종료 이벤트 처리
        /// </summary>
        public void HandleJellyForceQuit(int ggumtleId, int leftJellyCount)
        {
            var ggumtleIdStr = ggumtleId.ToString();
            DebugLog(
                $"[GgumtleServiceImpl] 강제 먹이기 종료: {ggumtleIdStr}, 남은 젤리: {leftJellyCount}"
            );

            CancelHold(ggumtleIdStr);
            _notificationPublisher.Publish(
                new Features.Notification.Messages.NotificationMessage(
                    $"먹이기가 중단되었습니다. 남은 젤리: {leftJellyCount}",
                    2f,
                    Features.Notification.Models.NotificationType.Warning
                )
            );
        }

        /// <summary>
        /// 서버에서 꿈틀이 스폰 이벤트 처리
        /// </summary>
        public void HandleGgumtleSpawn(int ggumtleId, Vector3 position)
        {
            var ggumtleName = $"Ggumtle_{ggumtleId}";

            DebugLog($"[GgumtleServiceImpl] 꿈틀이 스폰: {ggumtleId}, Position={position}");

            // 꿈틀이 등록 (Buried 상태로 시작)
            RegisterGgumtle(ggumtleId, ggumtleName, position);

            // 스폰 알림 발행
            _notificationPublisher.Publish(
                new Features.Notification.Messages.NotificationMessage($"새로운 꿈틀이가 나타났습니다!", 2f, Features.Notification.Models.NotificationType.Info)
            );
        }

        /// <summary>
        /// 서버에서 꿈틀이 성불 이벤트 처리
        /// </summary>
        public void HandleGgumtleNirvana(int ggumtleId)
        {
            var data = GetGgumtleData(ggumtleId);

            DebugLog($"[GgumtleServiceImpl] 꿈틀이 성불: {ggumtleId}");

            if (data != null)
            {
                // 성불 알림 발행
                _notificationPublisher.Publish(
                    new Features.Notification.Messages.NotificationMessage(
                        $"{data.ggumtleName}이(가) 성불했습니다!",
                        3f,
                        Features.Notification.Models.NotificationType.Success
                    )
                );

                // 꿈틀이 제거
                UnregisterGgumtle(ggumtleId);
            }
            else
            {
                DebugLog($"[GgumtleServiceImpl] 성불할 꿈틀이를 찾을 수 없음: {ggumtleId}");
            }
        }

        private void HandleDiggingError(Networks.Ggumtle.DiggingStartResult result)
        {
            string message = result switch
            {
                Networks.Ggumtle.DiggingStartResult.GgumtleNotFound => "꿈틀이를 찾을 수 없습니다",
                Networks.Ggumtle.DiggingStartResult.PlayerNotFoundOrNotMongging =>
                    "몽깅이 상태가 아닙니다",
                Networks.Ggumtle.DiggingStartResult.AlreadyDug => "이미 파낸 꿈틀이입니다",
                Networks.Ggumtle.DiggingStartResult.TooFar => "꿈틀이가 너무 멀리 있습니다",
                _ => "파기를 시작할 수 없습니다",
            };

            _notificationPublisher.Publish(
                new Features.Notification.Messages.NotificationMessage(message, 2f, Features.Notification.Models.NotificationType.Warning)
            );
        }

        private void HandleJellyError(Networks.Ggumtle.JellyStartResult result)
        {
            string message = result switch
            {
                Networks.Ggumtle.JellyStartResult.GgumtleNotFound => "꿈틀이를 찾을 수 없습니다",
                Networks.Ggumtle.JellyStartResult.PlayerNotFoundOrNotMongging =>
                    "몽깅이 상태가 아닙니다",
                Networks.Ggumtle.JellyStartResult.AlreadyPurified => "이미 정화된 꿈틀이입니다",
                Networks.Ggumtle.JellyStartResult.AlreadyPurifiedGgumtle =>
                    "이미 정화가 완료되었습니다",
                Networks.Ggumtle.JellyStartResult.NoJelly => "빛젤리가 없습니다",
                Networks.Ggumtle.JellyStartResult.TooFar => "꿈틀이가 너무 멀리 있습니다",
                _ => "먹이를 줄 수 없습니다",
            };

            _notificationPublisher.Publish(
                new Features.Notification.Messages.NotificationMessage(message, 2f, Features.Notification.Models.NotificationType.Warning)
            );
        }

        #endregion

        #region Network Event Handlers

        private void OnDiggingDoneReceived(GgumtleDiggingDoneMessage message)
        {
            try
            {
                DebugLog(
                    $"[GgumtleServiceImpl] 파기 완료 이벤트 수신: Id={message.GgumtleId}, IsRealGgumtle={message.IsRealGgumtle}"
                );
                HandleDiggingDone(message.GgumtleId, message.IsRealGgumtle);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleServiceImpl] 파기 완료 이벤트 처리 실패: {e.Message}");
            }
        }

        private void OnJellyForceQuitReceived(GgumtleJellyForceQuitMessage message)
        {
            try
            {
                DebugLog(
                    $"[GgumtleServiceImpl] 젤리 강제 종료 이벤트 수신: GgumtleId={message.GgumtleId}, LeftJellyCount={message.LeftJellyCount}"
                );
                HandleJellyForceQuit(message.GgumtleId, message.LeftJellyCount);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleServiceImpl] 젤리 강제 종료 이벤트 처리 실패: {e.Message}");
            }
        }

        private void OnSpawnReceived(GgumtleSpawnMessage message)
        {
            try
            {
                DebugLog(
                    $"[GgumtleServiceImpl] 꿈틀이 스폰 이벤트 수신: Id={message.GgumtleId}, Position={message.Position}"
                );
                HandleGgumtleSpawn(message.GgumtleId, message.Position);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GgumtleServiceImpl] 꿈틀이 스폰 이벤트 처리 실패: {e.Message}");
            }
        }


        #endregion

        #region Utility

        /// <summary>
        /// 디버그용 - 등록된 꿈틀이 목록과 상태 출력
        /// </summary>
        public void LogGgumtleStatus()
        {
            DebugLog($"[GgumtleServiceImpl] 등록된 꿈틀이 목록: [{string.Join(", ", _ggumtleDataMap.Keys)}]");
            foreach (var kvp in _ggumtleDataMap)
            {
                var data = kvp.Value;
                DebugLog($"[GgumtleServiceImpl] ID: {kvp.Key}, 이름: {data.ggumtleName}, 상태: {data.currentState}, 홀드중: {data.isHoldInProgress}");
            }
        }

        public void ResetService()
        {
            _ggumtleDataMap.Clear();
            DebugLog("[GgumtleServiceImpl] 서비스 리셋 완료");
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
            DebugLog("[GgumtleServiceImpl] Dispose 완료");
        }

        #endregion
    }
}
