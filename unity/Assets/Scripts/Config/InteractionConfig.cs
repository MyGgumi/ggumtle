using UnityEngine;

namespace Config
{
    /// <summary>
    /// 상호작용 관련 설정을 중앙에서 관리하는 클래스
    /// </summary>
    public static class InteractionConfig
    {
        [Header("플레이어 감지 범위")]
        /// <summary>
        /// 플레이어의 상호작용 감지 범위 (InteractionTriggerDetector)
        /// </summary>
        public static float PlayerDetectionRadius = 6f;

        [Header("개별 객체 상호작용 범위")]
        /// <summary>
        /// 꿈틀이 상호작용 범위
        /// </summary>
        public static float GgumtleInteractionRange = 10f;

        /// <summary>
        /// 상자 상호작용 범위
        /// </summary>
        public static float ChestInteractionRange = 3f;

        /// <summary>
        /// 기본 상호작용 범위 (다른 객체들용)
        /// </summary>
        public static float DefaultInteractionRange = 2f;

        [Header("특수 설정")]
        /// <summary>
        /// 꿈틀이가 땅에 박혀있을 때 높이 오프셋
        /// </summary>
        public static float GgumtleBuriedHeightOffset = 1.5f;

        /// <summary>
        /// 런타임에서 모든 범위 값을 로그로 출력 (디버깅용)
        /// </summary>
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogAllRanges()
        {
            Debug.Log($"[InteractionConfig] 상호작용 범위 설정:");
            Debug.Log($"  - 플레이어 감지 범위: {PlayerDetectionRadius}");
            Debug.Log($"  - 꿈틀이 범위: {GgumtleInteractionRange}");
            Debug.Log($"  - 상자 범위: {ChestInteractionRange}");
            Debug.Log($"  - 기본 범위: {DefaultInteractionRange}");
        }

        /// <summary>
        /// 모든 범위를 확대/축소 (테스트용)
        /// </summary>
        public static void ScaleAllRanges(float multiplier)
        {
            PlayerDetectionRadius *= multiplier;
            GgumtleInteractionRange *= multiplier;
            ChestInteractionRange *= multiplier;
            DefaultInteractionRange *= multiplier;

            Debug.Log($"[InteractionConfig] 모든 범위를 {multiplier}배로 조정");
            LogAllRanges();
        }
    }
}
