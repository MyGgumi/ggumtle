using System;
using UnityEngine;

namespace Features.Mongging.Models
{
    /// <summary>
    /// 개별 몽깅이 플레이어 데이터
    /// </summary>
    [Serializable]
    public class MonggingPlayerData
    {
        [Header("Player Info")]
        public long playerId;
        public string playerName;
        public MonggingPlayerType playerType;

        [Header("Health & State")]
        public int currentHp;
        public int maxHp;
        public MonggingPlayerState currentState;
        public int faintCount; // 기절 횟수 (3회 시 영구 사망)

        [Header("Status Effects")]
        public bool isStunned;
        public float stunRemainingTime;
        public bool isFrightened;
        public float frightenRemainingTime;

        [Header("Settings")]
        public bool isLocal; // 로컬 플레이어 여부

        public MonggingPlayerData()
        {
            playerId = -1;
            playerName = "";
            playerType = MonggingPlayerType.Worker;
            currentHp = 100;
            maxHp = 100;
            currentState = MonggingPlayerState.Normal;
            faintCount = 0;
            isStunned = false;
            stunRemainingTime = 0f;
            isFrightened = false;
            frightenRemainingTime = 0f;
            isLocal = false;
        }

        /// <summary>
        /// 체력 비율 (0.0 ~ 1.0)
        /// </summary>
        public float HealthPercentage => maxHp > 0 ? (float)currentHp / maxHp : 0f;

        /// <summary>
        /// 생존 상태인지 확인
        /// </summary>
        public bool IsAlive => currentState != MonggingPlayerState.Dead;

        /// <summary>
        /// 기절 상태인지 확인
        /// </summary>
        public bool IsFainted => currentState == MonggingPlayerState.Fainted;

        /// <summary>
        /// 부활 가능한지 확인 (3번 기절 전까지)
        /// </summary>
        public bool CanBeRevived => IsFainted && faintCount < 3;

        /// <summary>
        /// 데미지 적용
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHp = Mathf.Max(0, currentHp - damage);

            // 체력 0이면 기절 상태로 전환
            if (currentHp <= 0 && currentState != MonggingPlayerState.Fainted)
            {
                faintCount++;
                if (faintCount >= 3)
                {
                    currentState = MonggingPlayerState.Dead;
                }
                else
                {
                    currentState = MonggingPlayerState.Fainted;
                }
            }
        }

        /// <summary>
        /// 체력 회복
        /// </summary>
        public void Heal(int healAmount)
        {
            if (IsAlive && currentState != MonggingPlayerState.Fainted)
            {
                currentHp = Mathf.Min(maxHp, currentHp + healAmount);
            }
        }

        /// <summary>
        /// 상태 업데이트 (서버 동기화용)
        /// </summary>
        public void UpdateFromServer(int hp, MonggingPlayerState state, int faintCount)
        {
            this.currentHp = hp;
            this.currentState = state;
            this.faintCount = faintCount;
        }

        /// <summary>
        /// 감전 상태 적용
        /// </summary>
        public void ApplyStun(float duration = 2f)
        {
            if (IsAlive && currentState == MonggingPlayerState.Normal)
            {
                currentState = MonggingPlayerState.Stunned;
                isStunned = true;
                stunRemainingTime = duration;
            }
        }

        /// <summary>
        /// 공포 상태 적용
        /// </summary>
        public void ApplyFrighten(float duration = 5f)
        {
            if (IsAlive && currentState == MonggingPlayerState.Normal)
            {
                currentState = MonggingPlayerState.Frightened;
                isFrightened = true;
                frightenRemainingTime = duration;
            }
        }

        /// <summary>
        /// 상태이상 시간 업데이트
        /// </summary>
        public void UpdateStatusEffects(float deltaTime)
        {
            // 감전 상태 업데이트
            if (isStunned)
            {
                stunRemainingTime -= deltaTime;
                if (stunRemainingTime <= 0f)
                {
                    isStunned = false;
                    stunRemainingTime = 0f;
                    if (currentState == MonggingPlayerState.Stunned)
                    {
                        currentState = MonggingPlayerState.Normal;
                    }
                }
            }

            // 공포 상태 업데이트
            if (isFrightened)
            {
                frightenRemainingTime -= deltaTime;
                if (frightenRemainingTime <= 0f)
                {
                    isFrightened = false;
                    frightenRemainingTime = 0f;
                    if (currentState == MonggingPlayerState.Frightened)
                    {
                        currentState = MonggingPlayerState.Normal;
                    }
                }
            }
        }

        /// <summary>
        /// 부활
        /// </summary>
        public void Revive(int reviveHp = 30)
        {
            if (CanBeRevived)
            {
                currentHp = reviveHp;
                currentState = MonggingPlayerState.Normal;
            }
        }

        /// <summary>
        /// 탈출 완료
        /// </summary>
        public void Escape()
        {
            if (IsAlive)
            {
                currentState = MonggingPlayerState.Escaped;
            }
        }

        /// <summary>
        /// 초기화
        /// </summary>
        public void Reset()
        {
            currentHp = maxHp;
            currentState = MonggingPlayerState.Normal;
            faintCount = 0;
            isStunned = false;
            stunRemainingTime = 0f;
            isFrightened = false;
            frightenRemainingTime = 0f;
        }
    }
}