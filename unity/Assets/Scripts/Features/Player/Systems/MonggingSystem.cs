using Networks.Rooms.Domains;
using UnityEngine;
using System;
using R3;

namespace Features.Player.Systems
{
    /// <summary>
    /// 몽깅이(일반 플레이어) 시스템 컴포넌트
    /// HP, 힐링, 작업 속도 등 몽깅이 전용 기능 관리
    /// </summary>
    public class MonggingSystem : MonoBehaviour
    {
        [Header("Mongging Stats")]
        [SerializeField] private int _maxHp = 100;
        [SerializeField] private int _currentHp = 100;
        [SerializeField] private int _healSpeed = 10;
        [SerializeField] private int _workSpeed = 10;

        [Header("Status")]
        [SerializeField] private MonggingStatus _currentStatus = MonggingStatus.Default;

        [Header("Debug Settings")]
        [SerializeField] private bool _enableDebugLogs = false;

        // Observable Properties
        private readonly ReactiveProperty<int> _currentHpReactive = new();
        private readonly ReactiveProperty<int> _maxHpReactive = new();
        private readonly ReactiveProperty<MonggingStatus> _statusReactive = new();

        // Public Reactive Properties (읽기 전용)
        public ReadOnlyReactiveProperty<int> CurrentHp => _currentHpReactive.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<int> MaxHp => _maxHpReactive.ToReadOnlyReactiveProperty();
        public ReadOnlyReactiveProperty<MonggingStatus> Status => _statusReactive.ToReadOnlyReactiveProperty();

        // Events
        public event Action<int, int> OnHpChanged; // (current, max)
        public event Action<MonggingStatus, MonggingStatus> OnStatusChanged; // (old, new)
        public event Action OnPlayerDied;
        public event Action OnPlayerRevived;

        // Properties
        public int MaxHpValue => _maxHp;
        public int CurrentHpValue => _currentHp;
        public int HealSpeed => _healSpeed;
        public int WorkSpeed => _workSpeed;
        public MonggingStatus CurrentStatus => _currentStatus;
        public float HpPercentage => _maxHp > 0 ? (float)_currentHp / _maxHp : 0f;

        private void Start()
        {
            // Reactive Property 초기값 설정
            _currentHpReactive.Value = _currentHp;
            _maxHpReactive.Value = _maxHp;
            _statusReactive.Value = _currentStatus;

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingSystem] 초기화 완료: HP={_currentHp}/{_maxHp}, HealSpeed={_healSpeed}, WorkSpeed={_workSpeed}");
            }
        }

        /// <summary>
        /// PlayerPacket 데이터로 초기화
        /// </summary>
        public void InitializeFromPacket(PlayerPacket packet)
        {
            if (!packet.IsMongging)
            {
                Debug.LogWarning("[MonggingSystem] 몽둥이 플레이어에게 MonggingSystem이 적용되었습니다!");
                return;
            }

            _maxHp = packet.MaxHp;
            _currentHp = packet.MaxHp; // 초기 HP는 최대 HP로 설정
            _healSpeed = packet.HealSpeed;
            _workSpeed = packet.WorkSpeed;

            // Reactive Property 업데이트
            _maxHpReactive.Value = _maxHp;
            _currentHpReactive.Value = _currentHp;

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingSystem] 패킷으로 초기화: MaxHP={_maxHp}, HealSpeed={_healSpeed}, WorkSpeed={_workSpeed}");
            }
        }

        /// <summary>
        /// 체력 설정
        /// </summary>
        public void SetHp(int newHp)
        {
            int oldHp = _currentHp;
            _currentHp = Mathf.Clamp(newHp, 0, _maxHp);

            if (oldHp != _currentHp)
            {
                _currentHpReactive.Value = _currentHp;
                OnHpChanged?.Invoke(_currentHp, _maxHp);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingSystem] HP 변경: {oldHp} → {_currentHp}/{_maxHp}");
                }

                // 체력이 0이 되면 사망 처리
                if (_currentHp <= 0 && oldHp > 0)
                {
                    SetStatus(MonggingStatus.Dead);
                    OnPlayerDied?.Invoke();
                }
                // 체력이 0에서 증가하면 부활 처리
                else if (_currentHp > 0 && oldHp <= 0)
                {
                    SetStatus(MonggingStatus.Default);
                    OnPlayerRevived?.Invoke();
                }
            }
        }

        /// <summary>
        /// 체력 증가
        /// </summary>
        public void Heal(int amount)
        {
            SetHp(_currentHp + amount);
        }

        /// <summary>
        /// 체력 감소
        /// </summary>
        public void TakeDamage(int damage)
        {
            SetHp(_currentHp - damage);
        }

        /// <summary>
        /// 최대 체력까지 회복
        /// </summary>
        public void HealToFull()
        {
            SetHp(_maxHp);
        }

        /// <summary>
        /// 상태 설정
        /// </summary>
        public void SetStatus(MonggingStatus newStatus)
        {
            MonggingStatus oldStatus = _currentStatus;
            if (oldStatus != newStatus)
            {
                _currentStatus = newStatus;
                _statusReactive.Value = newStatus;
                OnStatusChanged?.Invoke(oldStatus, newStatus);

                if (_enableDebugLogs)
                {
                    Debug.Log($"[MonggingSystem] 상태 변경: {oldStatus} → {newStatus}");
                }
            }
        }

        /// <summary>
        /// 힐링 수행 (HealSpeed 기반)
        /// </summary>
        public void PerformHeal()
        {
            if (_currentStatus == MonggingStatus.Dead)
            {
                if (_enableDebugLogs)
                    Debug.Log("[MonggingSystem] 사망 상태에서는 힐링할 수 없습니다.");
                return;
            }

            Heal(_healSpeed);

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingSystem] 힐링 수행: +{_healSpeed} HP");
            }
        }

        /// <summary>
        /// 작업 수행 (WorkSpeed 반환)
        /// </summary>
        public int PerformWork()
        {
            if (_currentStatus == MonggingStatus.Dead)
            {
                if (_enableDebugLogs)
                    Debug.Log("[MonggingSystem] 사망 상태에서는 작업할 수 없습니다.");
                return 0;
            }

            if (_enableDebugLogs)
            {
                Debug.Log($"[MonggingSystem] 작업 수행: {_workSpeed} 효율");
            }

            return _workSpeed;
        }

        /// <summary>
        /// 생존 여부 확인
        /// </summary>
        public bool IsAlive()
        {
            return _currentHp > 0 && _currentStatus != MonggingStatus.Dead;
        }

        /// <summary>
        /// 기절 상태 설정
        /// </summary>
        public void SetFainted()
        {
            SetStatus(MonggingStatus.Faint);
        }

        /// <summary>
        /// 탈출 상태 설정
        /// </summary>
        public void SetEscaped()
        {
            SetStatus(MonggingStatus.Escape);
        }

        /// <summary>
        /// 기본 상태로 복귀
        /// </summary>
        public void SetDefault()
        {
            SetStatus(MonggingStatus.Default);
        }

        /// <summary>
        /// PlayerList UI용 상태 문자열 반환
        /// </summary>
        public string GetStatusString()
        {
            return _currentStatus switch
            {
                MonggingStatus.Default => "default",
                MonggingStatus.Dead => "dead",
                MonggingStatus.Faint => "faint",
                MonggingStatus.Escape => "escape",
                _ => "default"
            };
        }

        private void OnDestroy()
        {
            // Reactive Property 정리
            _currentHpReactive?.Dispose();
            _maxHpReactive?.Dispose();
            _statusReactive?.Dispose();
        }

        private void OnValidate()
        {
            // Inspector에서 값 변경 시 클램프
            _currentHp = Mathf.Clamp(_currentHp, 0, _maxHp);
            _maxHp = Mathf.Max(1, _maxHp);
            _healSpeed = Mathf.Max(0, _healSpeed);
            _workSpeed = Mathf.Max(0, _workSpeed);
        }
    }

    /// <summary>
    /// 몽깅이 상태 enum
    /// </summary>
    public enum MonggingStatus
    {
        Default,    // 기본 상태
        Faint,      // 기절 상태
        Dead,       // 사망 상태
        Escape      // 탈출 상태
    }
}