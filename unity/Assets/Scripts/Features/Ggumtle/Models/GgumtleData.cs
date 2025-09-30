using System;
using Config;
using UnityEngine;

namespace Features.Ggumtle.Models
{
    /// <summary>
    /// 꿈틀이 상태 정의
    /// </summary>
    public enum GgumtleState
    {
        Buried, // 땅에 묻혀있음 (초기 상태)
        Digging, // 파내는 중 (홀드 진행 중)
        Emerging, // 나오는 중 (애니메이션 실행 중, 상호작용 불가)
        Emerged, // 나와 있음 (먹이주기 대기 상태)
        Feeding, // 먹이 주기 중 상태
        Purified, // 정화 완료 (제거됨)
        Fake, // 짭꿈틀 (이펙트 재생 후 삭제)
    }

    /// <summary>
    /// 꿈틀이 데이터 모델
    /// </summary>
    [Serializable]
    public class GgumtleData
    {
        [Header("기본 정보")]
        public string ggumtleId;
        public string ggumtleName;
        public Vector3 position;

        [Header("상태")]
        public GgumtleState currentState;

        [Header("파내기 설정")]
        public float diggingHoldTime;

        [Header("먹이주기 설정")]
        public int maxFoodRequired;
        public int currentFoodAmount;
        public int feedingRate; // 0.5초당 먹이 개수
        public string acceptableFoodType;

        [Header("진행 상태")]
        public float holdProgress; // 홀드 진행도 (0.0 ~ 1.0)
        public bool isHoldInProgress;
        public bool isFeedingContinuously;
        public float lastFeedTime;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public GgumtleData()
        {
            ggumtleId = "";
            ggumtleName = "꿈틀이";
            position = Vector3.zero;
            currentState = GgumtleState.Buried;
            diggingHoldTime = 3f;
            maxFoodRequired = 5; // 테스트용으로 5개로 축소
            currentFoodAmount = 0;
            feedingRate = 1;
            acceptableFoodType = "Light";
            holdProgress = 0f;
            isHoldInProgress = false;
            isFeedingContinuously = false;
            lastFeedTime = 0f;
        }

        /// <summary>
        /// 꿈틀이 데이터 초기화
        /// </summary>
        public GgumtleData(string id, string name, Vector3 pos)
        {
            ggumtleId = id;
            ggumtleName = name;
            position = pos;
            currentState = GgumtleState.Buried;
            diggingHoldTime = 3f;
            maxFoodRequired = 5; // 테스트용으로 5개로 축소
            currentFoodAmount = 0;
            feedingRate = 1;
            acceptableFoodType = "Light";
            holdProgress = 0f;
            isHoldInProgress = false;
            isFeedingContinuously = false;
            lastFeedTime = 0f;
        }

        /// <summary>
        /// 상호작용 텍스트 반환
        /// </summary>
        public string GetInteractionText()
        {
            switch (currentState)
            {
                case GgumtleState.Buried:
                    return $"{ggumtleName} 파내기";
                case GgumtleState.Digging:
                    return $"{ggumtleName} 파내는 중...";
                case GgumtleState.Emerging:
                    return "꿈틀거리는중...";
                case GgumtleState.Feeding:
                    return $"빛젤리 먹이기 ({currentFoodAmount}/{maxFoodRequired})";
                case GgumtleState.Purified:
                default:
                    return "";
            }
        }

        /// <summary>
        /// 상호작용 가능한 객체 이름 반환
        /// </summary>
        public string GetInteractableName()
        {
            switch (currentState)
            {
                case GgumtleState.Buried:
                    return $"{ggumtleName} (파내기)";
                case GgumtleState.Feeding:
                    return $"{ggumtleName} (먹이주기 {currentFoodAmount}/{maxFoodRequired})";
                default:
                    return ggumtleName;
            }
        }

        /// <summary>
        /// 상호작용 가능 여부
        /// </summary>
        public bool CanInteract()
        {
            switch (currentState)
            {
                case GgumtleState.Buried:
                case GgumtleState.Digging:
                case GgumtleState.Emerging: // UI 표시를 위해 true (실제 상호작용은 핸들러에서 처리)
                case GgumtleState.Feeding:
                    return true;
                case GgumtleState.Purified:
                default:
                    return false;
            }
        }

        /// <summary>
        /// 홀드가 필요한 상태인지
        /// </summary>
        public bool RequiresHold()
        {
            return currentState == GgumtleState.Buried || currentState == GgumtleState.Feeding;
        }

        /// <summary>
        /// 상태를 다음 단계로 진행
        /// </summary>
        public void AdvanceToNextState()
        {
            switch (currentState)
            {
                case GgumtleState.Digging:
                    currentState = GgumtleState.Emerging;
                    break;
                case GgumtleState.Emerging:
                    currentState = GgumtleState.Feeding;
                    break;
                case GgumtleState.Feeding:
                    if (currentFoodAmount >= maxFoodRequired)
                    {
                        currentState = GgumtleState.Purified;
                    }
                    break;
            }
        }

        /// <summary>
        /// 먹이 추가
        /// </summary>
        public bool AddFood(int amount)
        {
            if (currentState != GgumtleState.Feeding)
                return false;

            currentFoodAmount += amount;
            if (currentFoodAmount >= maxFoodRequired)
            {
                currentFoodAmount = maxFoodRequired;
                return true; // 정화 조건 달성
            }
            return false;
        }

        /// <summary>
        /// 데이터 복사
        /// </summary>
        public GgumtleData Clone()
        {
            return new GgumtleData
            {
                ggumtleId = this.ggumtleId,
                ggumtleName = this.ggumtleName,
                position = this.position,
                currentState = this.currentState,
                diggingHoldTime = this.diggingHoldTime,
                maxFoodRequired = this.maxFoodRequired,
                currentFoodAmount = this.currentFoodAmount,
                feedingRate = this.feedingRate,
                acceptableFoodType = this.acceptableFoodType,
                holdProgress = this.holdProgress,
                isHoldInProgress = this.isHoldInProgress,
                isFeedingContinuously = this.isFeedingContinuously,
                lastFeedTime = this.lastFeedTime,
            };
        }
    }
}
