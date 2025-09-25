using System;
using UnityEngine;

namespace Features.Player.NetworkSources
{
    /// <summary>
    /// 플레이어 네트워크 동기화 설정
    /// </summary>
    [Serializable]
    public class PlayerNetworkSettings
    {
        [Header("Network Transmission")]
        [Tooltip("서버로 위치 정보를 전송하는 주기 (Hz) - 모바일 최적화 고려 80Hz 사용")]
        public float networkSendRate = 80f; // 80fps = 12.5ms 간격 (모바일 최적화 + 부드러운 동기화)

        [Tooltip("위치 변화 임계값 - 이 값 이하의 변화는 전송하지 않음 (정밀한 동기화)")]
        public float positionThreshold = 0.02f; // 더 정밀한 위치 동기화

        [Tooltip("방향 변화 임계값 - 이 값 이하의 변화는 전송하지 않음 (도 단위)")]
        public float directionThreshold = 1.0f; // 방향 변화 감지를 위한 각도 임계값 (더 민감하게)

        /// <summary>
        /// 호환성을 위한 회전 임계값 프로퍼티 (directionThreshold와 동일)
        /// </summary>
        [System.Obsolete("Use directionThreshold instead")]
        public float rotationThreshold => directionThreshold;

        [Header("Interpolation Settings")]
        [Tooltip("네트워크 데이터 보간 시간 (초)")]
        public float interpolationTime = 0.1f; // 100ms 버퍼

        [Tooltip("보간 속도 - 높을수록 빠르게 목표 위치로 이동")]
        public float interpolationSpeed = 25f; // SmoothDamp 최적화를 위해 증가

        [Tooltip("텔레포트 거리 임계값 - 이 거리 이상 차이날 때 즉시 이동")]
        public float teleportThreshold = 10.0f; // 텔레포트 빈도 감소

        [Header("Jump Detection")]
        [Tooltip("점프 감지를 위한 Y축 변화 임계값")]
        public float jumpDetectionThreshold = 0.5f;

        [Tooltip("점프 보간 시간 - 점프 동작의 부드러움 조절")]
        public float jumpInterpolationTime = 0.3f;

        [Tooltip("점프 예측 시간 - 네트워크 지연을 고려한 점프 예측")]
        public float jumpPredictionTime = 0.1f;

        [Header("Prediction")]
        [Tooltip("클라이언트 예측 사용 여부")]
        public bool useClientPrediction = true;

        [Tooltip("예측 시간 - 네트워크 지연을 고려한 위치 예측")]
        public float predictionTime = 0.05f; // 50ms

        [Header("Performance")]
        [Tooltip("네트워크 히스토리 최대 저장 개수")]
        public int maxNetworkHistorySize = 10;

        [Tooltip("거리 기반 업데이트 최적화 - 멀리 있는 플레이어는 낮은 주기로 업데이트")]
        public bool useDistanceBasedLOD = false; // 모든 플레이어 동일하게 처리

        [Tooltip("LOD 거리 임계값 - 이 거리 이상일 때 업데이트 주기 감소")]
        public float lodDistanceThreshold = 20f;

        [Header("Debug")]
        [Tooltip("네트워크 디버그 로그 활성화")]
        public bool enableNetworkDebugLogs = true; // 디버깅을 위해 활성화

        [Tooltip("보간 디버그 기즈모 표시")]
        public bool showInterpolationGizmos = false;

        /// <summary>
        /// 전송 간격을 초 단위로 반환
        /// </summary>
        public float SendInterval => 1f / networkSendRate;

        /// <summary>
        /// 거리 기반 전송 간격 계산
        /// </summary>
        public float GetSendInterval(float distance)
        {
            if (!useDistanceBasedLOD)
                return SendInterval;

            // 거리가 멀수록 전송 간격 증가 (최대 2배)
            float lodMultiplier = distance > lodDistanceThreshold ? 2f : 1f;
            return SendInterval * lodMultiplier;
        }

        /// <summary>
        /// 설정값 유효성 검증
        /// </summary>
        public void ValidateSettings()
        {
            networkSendRate = Mathf.Clamp(networkSendRate, 1f, 144f);
            interpolationTime = Mathf.Clamp(interpolationTime, 0.01f, 1f);
            interpolationSpeed = Mathf.Clamp(interpolationSpeed, 1f, 100f);
            teleportThreshold = Mathf.Clamp(teleportThreshold, 1f, 50f);
            jumpDetectionThreshold = Mathf.Clamp(jumpDetectionThreshold, 0.1f, 2f);
            maxNetworkHistorySize = Mathf.Clamp(maxNetworkHistorySize, 2, 50);
        }
    }

    /// <summary>
    /// 플레이어 네트워크 설정 ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerNetworkSettings", menuName = "Game/Player/Network Settings")]
    public class PlayerNetworkSettingsAsset : ScriptableObject
    {
        [SerializeField]
        private PlayerNetworkSettings _settings = new PlayerNetworkSettings();

        public PlayerNetworkSettings Settings
        {
            get
            {
                _settings.ValidateSettings();
                return _settings;
            }
        }

        private void OnValidate()
        {
            if (_settings != null)
            {
                _settings.ValidateSettings();
            }
        }
    }
}
