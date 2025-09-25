using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Networks;
using UnityEngine;
using VContainer;

namespace Features.Player.NetworkSources
{
    /// <summary>
    /// 플레이어 네트워크 통신 구현체
    /// 로컬 플레이어의 이동 데이터를 서버로 전송
    /// </summary>
    public class PlayerNetworkSource : IPlayerNetworkSource, IDisposable
    {
        private readonly NetworkApi _networkApi;

        private PlayerNetworkSettings _settings;
        private CancellationTokenSource _transmissionCts;

        // 이전 전송 데이터 저장 (중복 전송 방지)
        private Vector3 _lastSentPosition;
        private Vector3 _lastSentRotation;
        private bool _lastSentIsMoving;
        private float _lastSentSpeed;
        private bool _lastSentIsJumping;

        private float _lastSendTime;
        private bool _isTransmissionActive;

        [Inject]
        public PlayerNetworkSource(NetworkApi networkApi)
        {
            _networkApi = networkApi ?? throw new ArgumentNullException(nameof(networkApi));

            // 기본 설정 적용
            _settings = new PlayerNetworkSettings();
            _settings.enableNetworkDebugLogs = true; // 디버깅을 위해 기본으로 활성화
            _settings.ValidateSettings();

            Debug.Log($"[PlayerNetworkSource] 생성 완료 - 전송 주기: {_settings.networkSendRate}Hz ({_settings.SendInterval:F3}s 간격)");
        }

        public void UpdateSettings(PlayerNetworkSettings settings)
        {
            _settings = settings ?? new PlayerNetworkSettings();
            _settings.ValidateSettings();

            if (_settings.enableNetworkDebugLogs)
            {
                Debug.Log($"[PlayerNetworkSource] 설정 업데이트: SendRate={_settings.networkSendRate}Hz, Interval={_settings.SendInterval:F3}s");
            }
        }

        public void StartNetworkTransmission()
        {
            if (_isTransmissionActive)
            {
                Debug.LogWarning("[PlayerNetworkSource] 네트워크 전송이 이미 활성화되어 있습니다.");
                return;
            }

            _isTransmissionActive = true;
            _transmissionCts = new CancellationTokenSource();

            if (_settings.enableNetworkDebugLogs)
            {
                Debug.Log("[PlayerNetworkSource] 네트워크 전송 시작");
            }
        }

        public void StopNetworkTransmission()
        {
            if (!_isTransmissionActive)
                return;

            _isTransmissionActive = false;
            _transmissionCts?.Cancel();
            _transmissionCts?.Dispose();
            _transmissionCts = null;

            if (_settings.enableNetworkDebugLogs)
            {
                Debug.Log("[PlayerNetworkSource] 네트워크 전송 중지");
            }
        }

        public async UniTask<bool> SendPlayerMoveAsync(Vector3 position, Vector3 direction, bool isMoving, float speed)
        {
            if (!_isTransmissionActive || _networkApi == null)
            {
                return false;
            }

            try
            {
                // 전송 주기 체크
                float currentTime = Time.time;
                float timeSinceLastSend = currentTime - _lastSendTime;
                if (timeSinceLastSend < _settings.SendInterval)
                {
                    if (_settings.enableNetworkDebugLogs && Time.frameCount % 600 == 0) // 10초마다
                    {
                        Debug.Log($"[PlayerNetworkSource] 전송 주기 대기 중: {timeSinceLastSend:F3}s < {_settings.SendInterval:F3}s");
                    }
                    return false; // 아직 전송할 시간이 아님
                }

                // 변화량 체크 (최적화)
                bool shouldSend = ShouldSendMovementUpdate(position, direction, isMoving, speed);
                if (!shouldSend)
                {
                    if (_settings.enableNetworkDebugLogs && Time.frameCount % 600 == 0) // 10초마다
                    {
                        Debug.Log($"[PlayerNetworkSource] 변화량 부족으로 전송 생략: Pos차이={Vector3.Distance(position, _lastSentPosition):F3}");
                    }
                    return false;
                }

                // 서버로 전송 (위치와 방향 정보 모두 전송)
                _networkApi.SendPlayerMove(position, direction);

                if (_settings.enableNetworkDebugLogs)
                {
                    // 5초마다 또는 이동 상태가 변할 때만 로그
                    if (currentTime - _lastSendTime > 5f || isMoving != _lastSentIsMoving)
                    {
                        Debug.Log($"[PlayerNetworkSource] ✅ 이동 데이터 전송 완료: Pos={position}, Dir={direction}, Moving={isMoving}, Speed={speed:F2}, 간격={timeSinceLastSend:F3}s");
                    }
                }

                // 전송된 데이터 저장
                _lastSentPosition = position;
                _lastSentRotation = direction; // direction을 rotation 필드에 저장 (호환성)
                _lastSentIsMoving = isMoving;
                _lastSentSpeed = speed;
                _lastSendTime = currentTime;

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerNetworkSource] 이동 데이터 전송 실패: {ex.Message}");
                return false;
            }
        }

        public async UniTask<bool> SendPlayerJumpAsync(bool isJumping)
        {
            if (!_isTransmissionActive || _networkApi == null)
            {
                return false;
            }

            try
            {
                // 점프 상태가 변경되었을 때만 전송
                if (_lastSentIsJumping == isJumping)
                {
                    return false;
                }

                // TODO: 점프 전용 패킷이 있다면 여기서 전송
                // 현재는 위치 정보에 점프 상태가 포함되어 있다고 가정

                _lastSentIsJumping = isJumping;

                if (_settings.enableNetworkDebugLogs)
                {
                    Debug.Log($"[PlayerNetworkSource] 점프 데이터 전송: IsJumping={isJumping}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PlayerNetworkSource] 점프 데이터 전송 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 이동 데이터 전송 여부 판단
        /// </summary>
        private bool ShouldSendMovementUpdate(Vector3 position, Vector3 direction, bool isMoving, float speed)
        {
            // 위치 변화 체크
            float positionDelta = Vector3.Distance(position, _lastSentPosition);
            if (positionDelta > _settings.positionThreshold)
            {
                if (_settings.enableNetworkDebugLogs && Time.frameCount % 300 == 0) // 5초마다만
                {
                    Debug.Log($"[PlayerNetworkSource] 위치 변화로 전송: 거리={positionDelta:F3} > 임계값={_settings.positionThreshold}");
                }
                return true;
            }

            // 방향 변화 체크
            float directionDelta = Vector3.Angle(direction, _lastSentRotation);
            if (directionDelta > _settings.directionThreshold)
            {
                if (_settings.enableNetworkDebugLogs && Time.frameCount % 300 == 0) // 5초마다만
                {
                    Debug.Log($"[PlayerNetworkSource] 방향 변화로 전송: 각도={directionDelta:F3} > 임계값={_settings.directionThreshold}");
                }
                return true;
            }

            // 이동 상태 변화 체크
            if (isMoving != _lastSentIsMoving)
            {
                if (_settings.enableNetworkDebugLogs) // 상태 변화는 항상 로그
                {
                    Debug.Log($"[PlayerNetworkSource] 이동 상태 변화로 전송: {_lastSentIsMoving} → {isMoving}");
                }
                return true;
            }

            // 속도 변화 체크 (이동 중일 때만)
            if (isMoving && Mathf.Abs(speed - _lastSentSpeed) > 0.1f)
            {
                if (_settings.enableNetworkDebugLogs)
                {
                    Debug.Log($"[PlayerNetworkSource] 속도 변화로 전송: {_lastSentSpeed:F2} → {speed:F2}");
                }
                return true;
            }

            // 전송하지 않음
            if (_settings.enableNetworkDebugLogs && Time.frameCount % 600 == 0) // 10초마다
            {
                Debug.Log($"[PlayerNetworkSource] 전송 생략 - Pos차이:{positionDelta:F3}, Dir차이:{directionDelta:F3}, 이동상태:{isMoving}");
            }

            return false;
        }

        public void Dispose()
        {
            StopNetworkTransmission();
        }
    }
}