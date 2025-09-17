using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Models;

namespace Services
{
    /// <summary>
    /// 꿈틀이 비즈니스 로직 관리 서비스
    /// </summary>
    public class GgumtleService : MonoBehaviour
    {
        public static GgumtleService Instance { get; private set; }

        [Header("서비스 설정")]
        [SerializeField] private bool enableDebugLogs = true;

        // 꿈틀이 데이터 관리
        private Dictionary<string, GgumtleData> ggumtleDataMap = new Dictionary<string, GgumtleData>();
        private Dictionary<string, Coroutine> feedingCoroutines = new Dictionary<string, Coroutine>();

        #region Events

        /// <summary>
        /// 꿈틀이 상태 변경 이벤트 (ggumtleId, 이전상태, 새상태)
        /// </summary>
        public event Action<string, GgumtleState, GgumtleState> OnGgumtleStateChanged;

        /// <summary>
        /// 홀드 진행 이벤트 (ggumtleId, progress)
        /// </summary>
        public event Action<string, float> OnGgumtleHoldProgress;

        /// <summary>
        /// 먹이 추가 이벤트 (ggumtleId, 현재량, 최대량)
        /// </summary>
        public event Action<string, int, int> OnGgumtleFoodAdded;

        /// <summary>
        /// 꿈틀이 정화 완료 이벤트 (ggumtleId)
        /// </summary>
        public event Action<string> OnGgumtlePurified;

        #endregion

        #region Unity Lifecycle

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                DebugLog("[GgumtleService] 서비스 초기화 완료");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void OnDestroy()
        {
            // 모든 코루틴 정리
            foreach (var coroutine in feedingCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            feedingCoroutines.Clear();
        }

        #endregion

        #region Ggumtle Management

        /// <summary>
        /// 상태 변경 (중복 이벤트 방지)
        /// </summary>
        private void ChangeGgumtleState(string ggumtleId, GgumtleState newState)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null) return;

            var previousState = data.currentState;
            if (previousState == newState)
            {
                DebugLog($"[GgumtleService] 같은 상태 변경 시도 무시: {ggumtleId} - {newState}");
                return;
            }

            data.currentState = newState;
            DebugLog($"[GgumtleService] 상태 변경: {ggumtleId} - {previousState} → {newState}");
            OnGgumtleStateChanged?.Invoke(ggumtleId, previousState, newState);
        }

        /// <summary>
        /// 꿈틀이 등록
        /// </summary>
        public void RegisterGgumtle(string ggumtleId, string name, Vector3 position)
        {
            if (ggumtleDataMap.ContainsKey(ggumtleId))
            {
                DebugLog($"[GgumtleService] 이미 등록된 꿈틀이: {ggumtleId}");
                return;
            }

            var data = new GgumtleData(ggumtleId, name, position);
            ggumtleDataMap[ggumtleId] = data;
            DebugLog($"[GgumtleService] 꿈틀이 등록: {ggumtleId} - {name}");
        }

        /// <summary>
        /// 꿈틀이 데이터 조회
        /// </summary>
        public GgumtleData GetGgumtleData(string ggumtleId)
        {
            ggumtleDataMap.TryGetValue(ggumtleId, out GgumtleData data);
            return data;
        }

        /// <summary>
        /// 모든 꿈틀이 데이터 조회
        /// </summary>
        public Dictionary<string, GgumtleData> GetAllGgumtleData()
        {
            return new Dictionary<string, GgumtleData>(ggumtleDataMap);
        }

        /// <summary>
        /// 꿈틀이 제거
        /// </summary>
        public void UnregisterGgumtle(string ggumtleId)
        {
            if (ggumtleDataMap.ContainsKey(ggumtleId))
            {
                // 홀드 기반으로 변경되어 별도 정지 불필요

                ggumtleDataMap.Remove(ggumtleId);
                DebugLog($"[GgumtleService] 꿈틀이 제거: {ggumtleId}");
            }
        }

        #endregion

        #region Hold Interaction

        /// <summary>
        /// 홀드 시작
        /// </summary>
        public void StartHold(string ggumtleId)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null) return;

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
                    DebugLog($"[GgumtleService] 이미 Digging 상태 - 홀드만 재시작: {ggumtleId}");
                }
                else
                {
                    DebugLog($"[GgumtleService] 이미 홀드 진행 중: {ggumtleId}");
                }
            }
            else if (data.currentState == GgumtleState.Feeding)
            {
                // 먹이주기 홀드 시작 - 단일 먹이주기 준비만
                data.isHoldInProgress = true;
                data.holdProgress = 0f;
                DebugLog($"[GgumtleService] 먹이주기 홀드 시작: {ggumtleId}");
            }
            else
            {
                DebugLog($"[GgumtleService] StartHold 불가능한 상태: {ggumtleId} - {data.currentState}");
            }
        }

        /// <summary>
        /// 홀드 진행
        /// </summary>
        public void UpdateHoldProgress(string ggumtleId, float progress)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null || !data.isHoldInProgress)
                return;

            data.holdProgress = progress;
            OnGgumtleHoldProgress?.Invoke(ggumtleId, progress);
        }

        /// <summary>
        /// 홀드 완료
        /// </summary>
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

                // Emerging 상태에서 애니메이션 시간 후 Feeding 상태로 전환
                StartCoroutine(TransitionToFeedingAfterDelay(ggumtleId, 2f)); // 2초 후 전환
            }
            else if (data.currentState == GgumtleState.Feeding)
            {
                // 먹이주기 완료 - 한 개만 먹이기
                if (TryConsumeFoodFromPlayer(1))
                {
                    bool isPurified = data.AddFood(1);

                    DebugLog($"[GgumtleService] 먹이 추가: {ggumtleId} ({data.currentFoodAmount}/{data.maxFoodRequired})");
                    OnGgumtleFoodAdded?.Invoke(ggumtleId, data.currentFoodAmount, data.maxFoodRequired);

                    // 서버에 상태 전송
                    SendFeedingStatusToServer(ggumtleId, data);

                    // 정화 완료 확인
                    if (isPurified)
                    {
                        CompletePurification(ggumtleId);
                    }
                }
                else
                {
                    DebugLog($"[GgumtleService] 플레이어 먹이 부족: {ggumtleId}");

                    // 알림 메시지 표시
                    ShowInsufficientFeedingNotification();
                }
            }
        }

        /// <summary>
        /// 애니메이션 시간 후 먹이주기 상태로 전환
        /// </summary>
        private System.Collections.IEnumerator TransitionToFeedingAfterDelay(string ggumtleId, float delay)
        {
            yield return new UnityEngine.WaitForSeconds(delay);

            var data = GetGgumtleData(ggumtleId);
            if (data != null && data.currentState == GgumtleState.Emerging)
            {
                // Emerging → Feeding
                ChangeGgumtleState(ggumtleId, GgumtleState.Feeding);
            }
        }

        /// <summary>
        /// 홀드 취소
        /// </summary>
        public void CancelHold(string ggumtleId)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null) return;

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
                    DebugLog($"[GgumtleService] 먹이주기 홀드 취소: {ggumtleId}");
                }
            }
        }

        #endregion

        #region Feeding System

        // 지속적 먹이주기 코루틴 제거 - 홀드 방식으로 변경됨

        /// <summary>
        /// 정화 완료 처리
        /// </summary>
        private void CompletePurification(string ggumtleId)
        {
            var data = GetGgumtleData(ggumtleId);
            if (data == null) return;

            var previousState = data.currentState;
            data.currentState = GgumtleState.Purified;
            data.isFeedingContinuously = false;

            DebugLog($"[GgumtleService] 정화 완료: {ggumtleId}");
            OnGgumtleStateChanged?.Invoke(ggumtleId, previousState, data.currentState);
            OnGgumtlePurified?.Invoke(ggumtleId);

            // 서버에 정화 완료 전송
            SendPurificationStatusToServer(ggumtleId);
        }

        #endregion

        #region Player Inventory Integration

        /// <summary>
        /// 플레이어 인벤토리에서 먹이 소모 시도
        /// </summary>
        private bool TryConsumeFoodFromPlayer(int amount)
        {
            // PlayerInventory와 연동 필요
            var playerInventory = ViewModels.UI.InventoryViewModel.Instance;
            if (playerInventory != null)
            {
                // Light 아이템 소모 시도
                return playerInventory.RemoveFeeding(amount);
            }

            // 임시: 디버그 모드에서는 항상 성공
            return enableDebugLogs;
        }

        #endregion

        #region Server Communication

        /// <summary>
        /// 서버에 먹이주기 상태 전송
        /// </summary>
        private void SendFeedingStatusToServer(string ggumtleId, GgumtleData data)
        {
            if (ServerSyncManager.Instance != null)
            {
                // TODO: 실제 서버 통신 구현
                DebugLog($"[GgumtleService] 서버 전송 - 먹이주기: {ggumtleId}, 양: {data.currentFoodAmount}");
            }
        }

        /// <summary>
        /// 서버에 정화 완료 전송
        /// </summary>
        private void SendPurificationStatusToServer(string ggumtleId)
        {
            if (ServerSyncManager.Instance != null)
            {
                // TODO: 실제 서버 통신 구현
                DebugLog($"[GgumtleService] 서버 전송 - 정화 완료: {ggumtleId}");
            }
        }

        /// <summary>
        /// 먹이 부족 알림 표시
        /// </summary>
        private void ShowInsufficientFeedingNotification()
        {
            var universalHUD = UnityEngine.GameObject.FindFirstObjectByType<UniversalHUDController>();
            if (universalHUD != null && universalHUD.notificationViewModel != null)
            {
                universalHUD.notificationViewModel.ShowNotification("빛젤리가 부족합니다!", 2f);
                DebugLog("[GgumtleService] 빛젤리 부족 알림 표시");
            }
            else
            {
                DebugLog("[GgumtleService] UniversalHUDController 또는 NotificationViewModel을 찾을 수 없음");
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// 디버그 로그 출력
        /// </summary>
        private void DebugLog(string message)
        {
            if (enableDebugLogs)
            {
                Debug.Log(message);
            }
        }

        /// <summary>
        /// 서비스 상태 리셋
        /// </summary>
        public void ResetService()
        {
            // 모든 코루틴 정지
            foreach (var coroutine in feedingCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            feedingCoroutines.Clear();

            // 모든 데이터 초기화
            ggumtleDataMap.Clear();

            DebugLog("[GgumtleService] 서비스 리셋 완료");
        }

        #endregion
    }
}