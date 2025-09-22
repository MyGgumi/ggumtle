using System;
using UnityEngine;

namespace Features.PlayerHealth.Models
{
    /// <summary>
    /// 플레이어 상태 타입
    /// </summary>
    public enum PlayerState
    {
        Normal,     // 정상 상태
        LowHealth,  // 체력 부족 (30% 이하)
        Critical,   // 위험 상태 (10% 이하)
        Fainted,    // 기절 상태
        Dead        // 사망 상태
    }

    /// <summary>
    /// 체력바 타입 (3단계 체력바 시스템)
    /// </summary>
    public enum HealthBarType
    {
        Bar1,   // 0-40 HP
        Bar2,   // 41-80 HP
        Bar3    // 81-100 HP
    }

    /// <summary>
    /// 개별 체력바 데이터
    /// </summary>
    [Serializable]
    public class HealthBarData
    {
        [Header("Bar Info")]
        public HealthBarType barType;
        public int minHp;
        public int maxHp;
        public float currentPercent;
        public Color barColor;
        public bool isVisible;

        public HealthBarData(HealthBarType type, int min, int max)
        {
            barType = type;
            minHp = min;
            maxHp = max;
            currentPercent = 0f;
            barColor = Color.white;
            isVisible = false;
        }

        /// <summary>
        /// 체력에 따른 퍼센트 계산
        /// </summary>
        public void UpdatePercent(int currentHp)
        {
            if (currentHp < minHp)
            {
                currentPercent = 0f;
                isVisible = false;
            }
            else if (currentHp > maxHp)
            {
                currentPercent = 100f;
                isVisible = true;
            }
            else
            {
                int adjustedHp = currentHp - minHp;
                int adjustedMax = maxHp - minHp;
                currentPercent = adjustedMax > 0 ? (float)adjustedHp / adjustedMax * 100f : 0f;
                isVisible = true;
            }
        }
    }

    /// <summary>
    /// 플레이어 체력 시스템 전체 데이터 관리
    /// </summary>
    [Serializable]
    public class PlayerHealthData
    {
        [Header("Health State")]
        public int currentHp = 100;
        public int maxHp = 100;
        public bool isFainted = false;
        public float faintTimeRemaining = 0f;

        [Header("Settings")]
        public float faintReviveTime = 5f;
        public bool enableFaintSystem = true;

        [Header("Colors")]
        public Color normalBarColor = new Color(255f / 255f, 255f / 255f, 255f / 255f, 0.4f);
        public Color criticalBarColor = new Color(154f / 255f, 8f / 255f, 11f / 255f, 1.0f);
        public Color thirdBarColor = new Color(235f / 255f, 255f / 255f, 123f / 255f, 0.4f);

        [Header("Health Bars")]
        public HealthBarData bar1 = new(HealthBarType.Bar1, 0, 40);
        public HealthBarData bar2 = new(HealthBarType.Bar2, 40, 80);
        public HealthBarData bar3 = new(HealthBarType.Bar3, 80, 100);

        /// <summary>
        /// 체력 비율 (0.0 ~ 1.0)
        /// </summary>
        public float HealthPercentage => maxHp > 0 ? (float)currentHp / maxHp : 0f;

        /// <summary>
        /// 낮은 체력 상태 (30% 이하)
        /// </summary>
        public bool IsLowHealth => HealthPercentage <= 0.3f;

        /// <summary>
        /// 위험 체력 상태 (10% 이하)
        /// </summary>
        public bool IsCriticalHealth => HealthPercentage <= 0.1f;

        /// <summary>
        /// 생존 상태 (체력이 있거나 기절 상태)
        /// </summary>
        public bool IsAlive => currentHp > 0 || isFainted;

        /// <summary>
        /// 사망 상태 (체력 0이고 기절도 아님)
        /// </summary>
        public bool IsDead => currentHp <= 0 && !isFainted;

        /// <summary>
        /// 현재 플레이어 상태
        /// </summary>
        public PlayerState CurrentState
        {
            get
            {
                if (IsDead) return PlayerState.Dead;
                if (isFainted) return PlayerState.Fainted;
                if (IsCriticalHealth) return PlayerState.Critical;
                if (IsLowHealth) return PlayerState.LowHealth;
                return PlayerState.Normal;
            }
        }

        /// <summary>
        /// 체력 설정 (최대 체력도 함께 설정 가능)
        /// </summary>
        public void SetHealth(int newHp, int newMaxHp = -1)
        {
            if (newMaxHp > 0)
            {
                maxHp = newMaxHp;
            }
            currentHp = Mathf.Clamp(newHp, 0, maxHp);
            UpdateHealthBars();
        }

        /// <summary>
        /// 데미지 적용
        /// </summary>
        public void DamageHealth(int damage)
        {
            currentHp = Mathf.Clamp(currentHp - damage, 0, maxHp);
            UpdateHealthBars();
        }

        /// <summary>
        /// 체력 회복
        /// </summary>
        public void HealHealth(int healAmount)
        {
            currentHp = Mathf.Clamp(currentHp + healAmount, 0, maxHp);
            UpdateHealthBars();
        }

        /// <summary>
        /// 기절 상태 설정
        /// </summary>
        public void SetFaintState(bool fainted)
        {
            isFainted = fainted;
            if (fainted)
            {
                faintTimeRemaining = faintReviveTime;
            }
            else
            {
                faintTimeRemaining = 0f;
            }
            UpdateHealthBars();
        }

        /// <summary>
        /// 기절 시간 업데이트
        /// </summary>
        public void UpdateFaintTime(float deltaTime)
        {
            if (isFainted && faintTimeRemaining > 0)
            {
                faintTimeRemaining = Mathf.Max(0f, faintTimeRemaining - deltaTime);
            }
        }

        /// <summary>
        /// 플레이어 부활
        /// </summary>
        public void RevivePlayer(int reviveHp = 30)
        {
            if (isFainted)
            {
                SetFaintState(false);
                SetHealth(reviveHp);
            }
        }

        /// <summary>
        /// 체력바 색상 설정
        /// </summary>
        public void SetBarColors(Color normal, Color critical, Color third)
        {
            normalBarColor = normal;
            criticalBarColor = critical;
            thirdBarColor = third;
            UpdateHealthBars();
        }

        /// <summary>
        /// 체력바들 업데이트
        /// </summary>
        public void UpdateHealthBars()
        {
            // 기절 상태일 때 특별 처리
            if (isFainted && currentHp <= 0)
            {
                bar1.currentPercent = 12f; // 기절 시 첫 번째 바는 12%로 고정
                bar1.barColor = criticalBarColor;
                bar1.isVisible = true;

                bar2.currentPercent = 0f;
                bar2.isVisible = false;

                bar3.currentPercent = 0f;
                bar3.isVisible = false;
            }
            else
            {
                // 정상 상태일 때 각 바 업데이트
                bar1.UpdatePercent(currentHp);
                bar1.barColor = normalBarColor;

                bar2.UpdatePercent(currentHp);
                bar2.barColor = normalBarColor;

                bar3.UpdatePercent(currentHp);
                bar3.barColor = thirdBarColor;
            }
        }

        /// <summary>
        /// 기절 상태 확인 (체력이 0일 때 자동 기절)
        /// </summary>
        public bool ShouldBeFainted()
        {
            return enableFaintSystem && currentHp <= 0 && !isFainted;
        }

        /// <summary>
        /// 사망 상태 확인 (기절 시간 만료)
        /// </summary>
        public bool ShouldDie()
        {
            return isFainted && faintTimeRemaining <= 0;
        }

        /// <summary>
        /// 모든 상태 초기화
        /// </summary>
        public void Reset()
        {
            currentHp = maxHp;
            isFainted = false;
            faintTimeRemaining = 0f;
            UpdateHealthBars();
        }
    }
}