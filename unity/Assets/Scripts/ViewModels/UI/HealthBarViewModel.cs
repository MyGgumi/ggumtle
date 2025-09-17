using System;
using System.Collections;
using MVVM.Core;
using UnityEngine;

namespace MVVM.UI
{
    public class HealthBarViewModel : BaseViewModel
    {
        [Header("Health State")]
        [SerializeField]
        private int _currentHP = 100;

        [SerializeField]
        private int _maxHP = 100;

        [SerializeField]
        private bool _isFainted = false;

        [SerializeField]
        private float _faintTimeRemaining = 0f;

        [Header("Settings")]
        [SerializeField]
        private float _faintReviveTime = 5f;

        [SerializeField]
        private bool _enableFaintSystem = true;

        [Header("Colors")]
        [SerializeField]
        private Color _normalBarColor = new Color(255f / 255f, 255f / 255f, 255f / 255f, 0.4f);

        [SerializeField]
        private Color _criticalBarColor = new Color(154f / 255f, 8f / 255f, 11f / 255f, 1.0f);

        [SerializeField]
        private Color _thirdBarColor = new Color(235f / 255f, 255f / 255f, 123f / 255f, 0.4f);

        public event Action<int, int> HealthChanged;
        public event Action<bool, float> FaintStateChanged;
        public event Action<float> FaintTimeUpdated;
        public event Action PlayerDied;
        public event Action PlayerRevived;

        private Coroutine _faintCountdownCoroutine;

        #region Properties

        public int CurrentHP
        {
            get => _currentHP;
            set
            {
                var clampedValue = Mathf.Clamp(value, 0, _maxHP);
                if (SetProperty(ref _currentHP, clampedValue))
                {
                    HealthChanged?.Invoke(_currentHP, _maxHP);
                    CheckFaintState();
                }
            }
        }

        public int MaxHP
        {
            get => _maxHP;
            set
            {
                if (SetProperty(ref _maxHP, Mathf.Max(1, value)))
                {
                    CurrentHP = Mathf.Clamp(_currentHP, 0, _maxHP);
                }
            }
        }

        public bool IsFainted
        {
            get => _isFainted;
            private set => SetProperty(ref _isFainted, value);
        }

        public float FaintTimeRemaining
        {
            get => _faintTimeRemaining;
            private set
            {
                if (SetProperty(ref _faintTimeRemaining, value))
                {
                    FaintTimeUpdated?.Invoke(value);
                }
            }
        }

        public float FaintReviveTime
        {
            get => _faintReviveTime;
            set => SetProperty(ref _faintReviveTime, Mathf.Max(0f, value));
        }

        public bool EnableFaintSystem
        {
            get => _enableFaintSystem;
            set => SetProperty(ref _enableFaintSystem, value);
        }

        public Color NormalBarColor
        {
            get => _normalBarColor;
            set => SetProperty(ref _normalBarColor, value);
        }

        public Color CriticalBarColor
        {
            get => _criticalBarColor;
            set => SetProperty(ref _criticalBarColor, value);
        }

        public Color ThirdBarColor
        {
            get => _thirdBarColor;
            set => SetProperty(ref _thirdBarColor, value);
        }

        public float HealthPercentage => _maxHP > 0 ? (float)_currentHP / _maxHP : 0f;
        public bool IsLowHealth => HealthPercentage <= 0.3f;
        public bool IsCriticalHealth => HealthPercentage <= 0.1f;
        public bool IsAlive => _currentHP > 0 || _isFainted;
        public bool IsDead => _currentHP <= 0 && !_isFainted;

        #endregion

        #region Public Methods

        public void SetHealth(int newHP, int newMaxHP = -1)
        {
            if (newMaxHP > 0)
            {
                MaxHP = newMaxHP;
            }
            CurrentHP = newHP;
        }

        public void DamageHealth(int damage)
        {
            CurrentHP -= damage;
        }

        public void HealHealth(int healAmount)
        {
            CurrentHP += healAmount;
        }

        public void RevivePlayer(int reviveHP = 30)
        {
            if (!_isFainted)
                return;

            SetFaintState(false);
            CurrentHP = reviveHP;
            PlayerRevived?.Invoke();

            if (EnableDebugLogs)
            {
                Debug.Log($"[HealthBarViewModel] 플레이어 부활 (체력: {reviveHP})");
            }
        }

        public void SetBarColors(Color normal, Color critical, Color third)
        {
            NormalBarColor = normal;
            CriticalBarColor = critical;
            ThirdBarColor = third;
        }

        #endregion

        #region Private Methods

        private void CheckFaintState()
        {
            if (!_enableFaintSystem)
                return;

            if (_currentHP <= 0 && !_isFainted)
            {
                SetFaintState(true);
                HUDEvents.TriggerFaintState(true);
            }
            else if (_currentHP > 0 && _isFainted)
            {
                SetFaintState(false);
                HUDEvents.TriggerFaintState(false);
            }
        }

        private void SetFaintState(bool isFainted)
        {
            IsFainted = isFainted;

            if (isFainted)
            {
                FaintTimeRemaining = _faintReviveTime;
                StartFaintCountdown();
                FaintStateChanged?.Invoke(true, _faintReviveTime);

                if (EnableDebugLogs)
                {
                    Debug.Log($"[HealthBarViewModel] 기절 상태 진입 ({_faintReviveTime}초)");
                }
            }
            else
            {
                FaintTimeRemaining = 0f;
                StopFaintCountdown();
                FaintStateChanged?.Invoke(false, 0f);

                if (EnableDebugLogs)
                {
                    Debug.Log("[HealthBarViewModel] 기절 상태 해제");
                }
            }
        }

        private void StartFaintCountdown()
        {
            StopFaintCountdown();
            _faintCountdownCoroutine = StartCoroutine(FaintCountdownCoroutine());
        }

        private void StopFaintCountdown()
        {
            if (_faintCountdownCoroutine != null)
            {
                StopCoroutine(_faintCountdownCoroutine);
                _faintCountdownCoroutine = null;
            }
        }

        private IEnumerator FaintCountdownCoroutine()
        {
            while (_faintTimeRemaining > 0)
            {
                yield return new WaitForSeconds(1f);
                FaintTimeRemaining -= 1f;

                if (EnableDebugLogs)
                {
                    Debug.Log(
                        $"[HealthBarViewModel] 기절 카운트다운: {_faintTimeRemaining:F0}초 남음"
                    );
                }
            }

            if (_isFainted && _faintTimeRemaining <= 0)
            {
                PlayerDied?.Invoke();
                HUDEvents.TriggerPlayerDeath();

                if (EnableDebugLogs)
                {
                    Debug.Log("[HealthBarViewModel] 부활 시간 종료 - 사망 처리");
                }
            }

            _faintCountdownCoroutine = null;
        }

        #endregion

        #region BaseViewModel Override

        protected override void InitializeViewModel()
        {
            base.InitializeViewModel();

            _currentHP = 100;
            _maxHP = 100;
            _isFainted = false;
            _faintTimeRemaining = 0f;
            _faintReviveTime = 5f;
            _enableFaintSystem = true;

            if (EnableDebugLogs)
            {
                Debug.Log("[HealthBarViewModel] 초기화 완료");
            }
        }

        protected override void CleanupViewModel()
        {
            base.CleanupViewModel();

            StopFaintCountdown();

            HealthChanged = null;
            FaintStateChanged = null;
            FaintTimeUpdated = null;
            PlayerDied = null;
            PlayerRevived = null;

            if (EnableDebugLogs)
            {
                Debug.Log("[HealthBarViewModel] 정리 완료");
            }
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            Debug.Log(
                $"[HealthBarViewModel] State:\n"
                    + $"  CurrentHP: {CurrentHP}/{MaxHP} ({HealthPercentage:P1})\n"
                    + $"  IsFainted: {IsFainted}\n"
                    + $"  FaintTimeRemaining: {FaintTimeRemaining:F1}s\n"
                    + $"  IsLowHealth: {IsLowHealth}\n"
                    + $"  IsCriticalHealth: {IsCriticalHealth}\n"
                    + $"  IsAlive: {IsAlive}"
            );
        }

        [ContextMenu("Debug Damage (10)")]
        private void DebugDamage() => DamageHealth(10);

        [ContextMenu("Debug Heal (10)")]
        private void DebugHeal() => HealHealth(10);

        [ContextMenu("Debug Toggle Faint")]
        private void DebugToggleFaint() => SetFaintState(!_isFainted);

        #endregion
    }
}
