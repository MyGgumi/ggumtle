using System;
using System.Collections.Generic;
using MessagePipe;
using Messages;
using Models;
using UnityEngine;
using VContainer;

namespace Services
{
    /// <summary>
    /// 꿈틀이 비즈니스 로직 서비스 (순수 C# 클래스)
    /// MonoBehaviour 의존성 제거하여 테스트 가능하고 DI 친화적으로 구현
    /// </summary>
    public class GgumtleServiceImpl : IGgumtleService
    {
        #region Dependencies

        private readonly IPublisher<GgumtleStateChangedMessage> _stateChangePublisher;
        private readonly IPublisher<GgumtleHoldProgressMessage> _holdProgressPublisher;
        private readonly IPublisher<GgumtleFoodAddedMessage> _foodAddedPublisher;
        private readonly IPublisher<GgumtlePurifiedMessage> _purifiedPublisher;
        private readonly IPublisher<NotificationMessage> _notificationPublisher;

        #endregion

        #region Private Fields

        private readonly Dictionary<string, GgumtleData> _ggumtleDataMap = new();
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Constructor

        [Inject]
        public GgumtleServiceImpl(
            IPublisher<GgumtleStateChangedMessage> stateChangePublisher,
            IPublisher<GgumtleHoldProgressMessage> holdProgressPublisher,
            IPublisher<GgumtleFoodAddedMessage> foodAddedPublisher,
            IPublisher<GgumtlePurifiedMessage> purifiedPublisher,
            IPublisher<NotificationMessage> notificationPublisher
        )
        {
            _stateChangePublisher = stateChangePublisher;
            _holdProgressPublisher = holdProgressPublisher;
            _foodAddedPublisher = foodAddedPublisher;
            _purifiedPublisher = purifiedPublisher;
            _notificationPublisher = notificationPublisher;

            DebugLog("[GgumtleServiceImpl] 서비스 초기화 완료");
        }

        #endregion

        #region Ggumtle Management

        public void RegisterGgumtle(string ggumtleId, string name, Vector3 position)
        {
            if (_ggumtleDataMap.ContainsKey(ggumtleId))
            {
                DebugLog($"[GgumtleServiceImpl] 이미 등록된 꿈틀이: {ggumtleId}");
                return;
            }

            var data = new GgumtleData(ggumtleId, name, position);
            _ggumtleDataMap[ggumtleId] = data;
            DebugLog($"[GgumtleServiceImpl] 꿈틀이 등록: {ggumtleId} - {name}");
        }

        public GgumtleData GetGgumtleData(string ggumtleId)
        {
            return _ggumtleDataMap.TryGetValue(ggumtleId, out GgumtleData data) ? data : null;
        }

        public Dictionary<string, GgumtleData> GetAllGgumtleData()
        {
            return new Dictionary<string, GgumtleData>(_ggumtleDataMap);
        }

        public void UnregisterGgumtle(string ggumtleId)
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
                    new NotificationMessage("빛젤리가 부족합니다!", 2f, NotificationType.Warning)
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

            // 정화 완료 알림
            _purifiedPublisher.Publish(new GgumtlePurifiedMessage(ggumtleId, data.position));
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

        #region Utility

        public void ResetService()
        {
            _ggumtleDataMap.Clear();
            DebugLog("[GgumtleServiceImpl] 서비스 리셋 완료");
        }

        #endregion
    }
}
