using Networks.Rooms.Domains;
using UnityEngine;
using System;
using R3;

namespace Features.Player.Systems
{
    /// <summary>
    /// 몽둥이(술래) 시스템 컴포넌트
    /// HP가 없고 술래 전용 기능 관리
    /// </summary>
    public class MongdungSystem : MonoBehaviour
    {
        [Header("Mongdung Stats")]
        [SerializeField] private int _moveSpeed = 10;
        [SerializeField] private int _workSpeed = 10;

        [Header("Status")]
        [SerializeField] private MongdungStatus _currentStatus = MongdungStatus.Default;

        [Header("Mongdung Abilities")]
        [SerializeField] private float _catchRange = 2.0f;
        [SerializeField] private float _specialAbilityCooldown = 10.0f;

        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugLogs = false;

        // Observable Properties
        private readonly ReactiveProperty<MongdungStatus> _statusReactive = new();
        private readonly ReactiveProperty<float> _specialAbilityCooldownReactive = new();

        // Public Reactive Properties (읽기 전용)
        public ReadOnlyReactiveProperty<MongdungStatus> Status => _statusReactive.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<float> SpecialAbilityCooldown => _specialAbilityCooldownReactive.ToReadOnlyReactiveProperty();

        // Events
        public event Action<MongdungStatus, MongdungStatus> OnStatusChanged; // (old, new)
        public event Action<GameObject> OnPlayerCaught; // 플레이어 잡기 성공
        public event Action OnSpecialAbilityUsed; // 특수 능력 사용
        public event Action OnSpecialAbilityReady; // 특수 능력 쿨다운 완료

        // Properties
        public int MoveSpeed => _moveSpeed;
        public int WorkSpeed => _workSpeed;
        public MongdungStatus CurrentStatus => _currentStatus;
        public float CatchRange => _catchRange;
        public float CurrentCooldown => _specialAbilityCooldownReactive.Value;
        public bool IsSpecialAbilityReady => _specialAbilityCooldownReactive.Value <= 0f;

        // 쿨다운 타이머
        private float _currentCooldownTimer = 0f;

        private void Start()
        {
            // Reactive Property 초기값 설정
            _statusReactive.Value = _currentStatus;
            _specialAbilityCooldownReactive.Value = 0f;

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungSystem] 초기화 완료: MoveSpeed={_moveSpeed}, WorkSpeed={_workSpeed}, CatchRange={_catchRange}");
            }
        }

        private void Update()
        {
            // 쿨다운 타이머 업데이트
            if (_currentCooldownTimer > 0f)
            {
                _currentCooldownTimer -= Time.deltaTime;
                _specialAbilityCooldownReactive.Value = _currentCooldownTimer;

                // 쿨다운 완료
                if (_currentCooldownTimer <= 0f)
                {
                    _currentCooldownTimer = 0f;
                    _specialAbilityCooldownReactive.Value = 0f;
                    OnSpecialAbilityReady?.Invoke();

                    if (_enableDebugLogs)
                    {
                        Debug.Log("[MongdungSystem] 특수 능력 쿨다운 완료");
                    }
                }
            }
        }

        /// <summary>
        /// PlayerPacket 데이터로 초기화
        /// </summary>
        public void InitializeFromPacket(PlayerPacket packet)
        {
            if (packet.IsMongging)
            {
                Debug.LogWarning("[MongdungSystem] 몽깅이 플레이어에게 MongdungSystem이 적용되었습니다!");
                return;
            }

            _moveSpeed = packet.MoveSpeed;
            _workSpeed = packet.WorkSpeed;

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungSystem] 패킷으로 초기화: MoveSpeed={_moveSpeed}, WorkSpeed={_workSpeed}");
            }
        }

        /// <summary>
        /// 상태 설정
        /// </summary>
        public void SetStatus(MongdungStatus newStatus)
        {
            MongdungStatus oldStatus = _currentStatus;
            if (oldStatus != newStatus)
            {
                _currentStatus = newStatus;
                _statusReactive.Value = newStatus;
                OnStatusChanged?.Invoke(oldStatus, newStatus);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungSystem] 상태 변경: {oldStatus} → {newStatus}");
                }
            }
        }

        /// <summary>
        /// 플레이어 잡기 시도
        /// </summary>
        public bool TryCatchPlayer(GameObject targetPlayer)
        {
            if (_currentStatus != MongdungStatus.Default)
            {
                if (_enableDebugLogs)
                    Debug.Log("[MongdungSystem] 기본 상태가 아니어서 플레이어를 잡을 수 없습니다.");
                return false;
            }

            if (targetPlayer == null)
                return false;

            float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
            if (distance <= _catchRange)
            {
                OnPlayerCaught?.Invoke(targetPlayer);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungSystem] 플레이어 잡기 성공: {targetPlayer.name}, 거리: {distance:F2}");
                }

                return true;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungSystem] 플레이어 잡기 실패 - 거리: {distance:F2} > {_catchRange}");
            }

            return false;
        }

        /// <summary>
        /// 특수 능력 사용
        /// </summary>
        public bool UseSpecialAbility()
        {
            if (!IsSpecialAbilityReady)
            {
                if (_enableDebugLogs)
                    Debug.Log($"[MongdungSystem] 특수 능력 쿨다운 중: {_currentCooldownTimer:F1}초 남음");
                return false;
            }

            if (_currentStatus != MongdungStatus.Default)
            {
                if (_enableDebugLogs)
                    Debug.Log("[MongdungSystem] 기본 상태가 아니어서 특수 능력을 사용할 수 없습니다.");
                return false;
            }

            // 쿨다운 시작
            _currentCooldownTimer = _specialAbilityCooldown;
            _specialAbilityCooldownReactive.Value = _currentCooldownTimer;

            OnSpecialAbilityUsed?.Invoke();

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungSystem] 특수 능력 사용 - 쿨다운: {_specialAbilityCooldown}초");
            }

            return true;
        }

        /// <summary>
        /// 작업 수행 (WorkSpeed 반환)
        /// </summary>
        public int PerformWork()
        {
            if (_currentStatus != MongdungStatus.Default)
            {
                if (_enableDebugLogs)
                    Debug.Log("[MongdungSystem] 기본 상태가 아니어서 작업할 수 없습니다.");
                return 0;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungSystem] 작업 수행: {_workSpeed} 효율");
            }

            return _workSpeed;
        }

        /// <summary>
        /// 게임 상태 설정
        /// </summary>
        public void SetGameStatus(MongdungStatus status)
        {
            SetStatus(status);
        }

        /// <summary>
        /// 기본 상태로 복귀
        /// </summary>
        public void SetDefault()
        {
            SetStatus(MongdungStatus.Default);
        }

        /// <summary>
        /// 게임 종료 상태 설정
        /// </summary>
        public void SetGameEnd()
        {
            SetStatus(MongdungStatus.GameEnd);
        }

        /// <summary>
        /// PlayerList UI용 상태 문자열 반환
        /// </summary>
        public string GetStatusString()
        {
            return _currentStatus switch
            {
                MongdungStatus.Default => "default",
                MongdungStatus.Hunting => "hunting", // 추적 중 (UI에서 특별 표시)
                MongdungStatus.GameEnd => "escape",   // 게임 종료 시 탈출로 표시
                _ => "default"
            };
        }

        /// <summary>
        /// 범위 내 플레이어들 찾기
        /// </summary>
        public GameObject[] GetPlayersInRange()
        {
            var colliders = Physics.OverlapSphere(transform.position, _catchRange);
            var players = new System.Collections.Generic.List<GameObject>();

            foreach (var collider in colliders)
            {
                if (collider.gameObject != this.gameObject && collider.CompareTag("Player"))
                {
                    players.Add(collider.gameObject);
                }
            }

            return players.ToArray();
        }

        private void OnDestroy()
        {
            // Reactive Property 정리
            _statusReactive?.Dispose();
            _specialAbilityCooldownReactive?.Dispose();
        }

        private void OnValidate()
        {
            // Inspector에서 값 변경 시 클램프
            _moveSpeed = Mathf.Max(0, _moveSpeed);
            _workSpeed = Mathf.Max(0, _workSpeed);
            _catchRange = Mathf.Max(0.1f, _catchRange);
            _specialAbilityCooldown = Mathf.Max(0f, _specialAbilityCooldown);
        }

        private void OnDrawGizmosSelected()
        {
            // 잡기 범위 표시
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _catchRange);
        }
    }

    /// <summary>
    /// 몽둥이 상태 enum
    /// </summary>
    public enum MongdungStatus
    {
        Default,    // 기본 상태
        Hunting,    // 추적 중
        GameEnd     // 게임 종료
    }
}