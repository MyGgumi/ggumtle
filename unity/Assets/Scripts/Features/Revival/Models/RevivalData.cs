using System;
using UnityEngine;

namespace Features.Revival.Models
{
    /// <summary>
    /// 부활 진행 데이터
    /// </summary>
    [Serializable]
    public class RevivalProgressData
    {
        public long revivingPlayerId;    // 부활시키는 플레이어 ID
        public long targetPlayerId;     // 부활 대상 플레이어 ID
        public RevivalType revivalType; // 부활 타입
        public RevivalState state;      // 현재 상태
        public float progress;          // 진행률 (0.0 ~ 1.0)
        public float startTime;         // 시작 시간
        public float duration;          // 총 소요 시간
        public Vector3 revivalPosition; // 부활 위치 (직접 부활용)
        public int reviveHp;            // 부활 시 체력값 (-1이면 기본값 사용)

        public RevivalProgressData()
        {
            Reset();
        }

        public RevivalProgressData(long revivingPlayerId, long targetPlayerId, RevivalType type, float duration = 5f, int reviveHp = -1)
        {
            this.revivingPlayerId = revivingPlayerId;
            this.targetPlayerId = targetPlayerId;
            this.revivalType = type;
            this.state = RevivalState.None;
            this.progress = 0f;
            this.startTime = Time.time;
            this.duration = duration;
            this.revivalPosition = Vector3.zero;
            this.reviveHp = reviveHp;
        }

        /// <summary>
        /// 남은 시간 계산
        /// </summary>
        public float GetRemainingTime()
        {
            float elapsed = Time.time - startTime;
            return Mathf.Max(0, duration - elapsed);
        }

        /// <summary>
        /// 진행률 계산 및 업데이트
        /// </summary>
        public float UpdateProgress()
        {
            float elapsed = Time.time - startTime;
            progress = Mathf.Clamp01(elapsed / duration);
            return progress;
        }

        /// <summary>
        /// 완료 여부 확인
        /// </summary>
        public bool IsCompleted()
        {
            return progress >= 1.0f || state == RevivalState.Completed;
        }

        /// <summary>
        /// 데이터 초기화
        /// </summary>
        public void Reset()
        {
            revivingPlayerId = -1;
            targetPlayerId = -1;
            revivalType = RevivalType.None;
            state = RevivalState.None;
            progress = 0f;
            startTime = 0f;
            duration = 0f;
            revivalPosition = Vector3.zero;
            reviveHp = -1;
        }

        /// <summary>
        /// 부활 시작
        /// </summary>
        public void StartRevival()
        {
            startTime = Time.time;
            progress = 0f;

            switch (revivalType)
            {
                case RevivalType.DirectRevival:
                    state = RevivalState.DirectReviving;
                    break;
                case RevivalType.SelfDefibrillator:
                    state = RevivalState.SelfDefibReviving;
                    break;
            }
        }

        /// <summary>
        /// 부활 완료
        /// </summary>
        public void CompleteRevival()
        {
            state = RevivalState.Completed;
            progress = 1f;
        }

        /// <summary>
        /// 부활 취소
        /// </summary>
        public void CancelRevival()
        {
            Reset();
        }
    }

    /// <summary>
    /// 부활 상호작용 데이터
    /// </summary>
    [Serializable]
    public class RevivalInteractionData
    {
        public long targetPlayerId;     // 부활 대상 플레이어 ID
        public bool isInRange;          // 부활 범위 내에 있는지
        public bool canRevive;          // 부활 가능한지
        public float distance;          // 거리
        public Vector3 targetPosition;  // 대상 위치
        public string interactionText;  // 상호작용 텍스트

        public RevivalInteractionData()
        {
            Reset();
        }

        public void Reset()
        {
            targetPlayerId = -1;
            isInRange = false;
            canRevive = false;
            distance = float.MaxValue;
            targetPosition = Vector3.zero;
            interactionText = "";
        }

        public void UpdateInteraction(long playerId, bool inRange, bool canRevive, float dist, Vector3 pos, string text = "")
        {
            targetPlayerId = playerId;
            isInRange = inRange;
            this.canRevive = canRevive;
            distance = dist;
            targetPosition = pos;
            interactionText = text;
        }
    }
}