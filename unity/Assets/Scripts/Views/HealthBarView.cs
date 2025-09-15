using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using MVVM.UI;

namespace Views
{
    public class HealthBarView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField] private HealthBarViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _healthBar;
        private VisualElement _faintOverlay;

        [Header("Health Bar Elements")]
        private VisualElement _bar1;
        private VisualElement _bar1Fill;
        private VisualElement _bar2;
        private VisualElement _bar2Fill;
        private VisualElement _bar3;
        private VisualElement _bar3Fill;

        [Header("Animation Settings")]
        [SerializeField] private float healthChangeAnimationDuration = 0.5f;
        [SerializeField] private AnimationCurve healthChangeAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        public void Initialize(VisualElement root, HealthBarViewModel viewModel)
        {
            _root = root;
            this.viewModel = viewModel;

            CacheUIElements();
            SubscribeToViewModel();
            UpdateAllUI();

            Debug.Log("[HealthBarView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            _healthBar = _root.Q<VisualElement>("healthBar");
            _faintOverlay = _root.Q<VisualElement>("faintOverlay");

            _bar1 = _root.Q<VisualElement>("bar1");
            _bar1Fill = _root.Q<VisualElement>("bar1Fill");
            _bar2 = _root.Q<VisualElement>("bar2");
            _bar2Fill = _root.Q<VisualElement>("bar2Fill");
            _bar3 = _root.Q<VisualElement>("bar3");
            _bar3Fill = _root.Q<VisualElement>("bar3Fill");

            Debug.Log($"[HealthBarView] UI 요소 캐싱 완료: " +
                     $"체력바={(_healthBar != null ? "OK" : "NULL")}, " +
                     $"기절오버레이={(_faintOverlay != null ? "OK" : "NULL")}, " +
                     $"체력바들={(_bar1Fill != null && _bar2Fill != null && _bar3Fill != null ? "OK" : "NULL")}");
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null) return;

            viewModel.HealthChanged += OnHealthChanged;
            viewModel.FaintStateChanged += OnFaintStateChanged;
            viewModel.FaintTimeUpdated += OnFaintTimeUpdated;

            Debug.Log("[HealthBarView] ViewModel 이벤트 구독 완료");
        }

        private void UnsubscribeFromViewModel()
        {
            if (viewModel == null) return;

            viewModel.HealthChanged -= OnHealthChanged;
            viewModel.FaintStateChanged -= OnFaintStateChanged;
            viewModel.FaintTimeUpdated -= OnFaintTimeUpdated;

            Debug.Log("[HealthBarView] ViewModel 이벤트 구독 해제 완료");
        }

        #region ViewModel Event Handlers

        private void OnHealthChanged(int currentHP, int maxHP)
        {
            UpdateHealthBarUI(currentHP, maxHP);
            HUDEvents.TriggerHealthChange(currentHP, maxHP);
        }

        private void OnFaintStateChanged(bool isFainted, float reviveTime)
        {
            UpdateFaintOverlay(isFainted, reviveTime);
        }

        private void OnFaintTimeUpdated(float timeRemaining)
        {
            // 추가적인 UI 업데이트가 필요하면 여기서 처리
        }

        #endregion

        #region UI Update Methods

        private void UpdateHealthBarUI(int currentHP, int maxHP)
        {
            // 체력바는 40HP씩 3개 구간으로 나뉨 (총 120HP 표시 가능, 실제 최대는 100HP)
            int hp1 = Mathf.Clamp(currentHP, 0, 40);
            int hp2 = Mathf.Clamp(currentHP - 40, 0, 40);
            int hp3 = Mathf.Clamp(currentHP - 80, 0, 20); // 3번째는 20HP만 (최대 100HP)

            // 첫 번째 체력바 (0-40HP)
            if (_bar1Fill != null)
            {
                if (currentHP <= 0)
                {
                    // 기절 상태일 때는 12%로 고정하고 빨간색
                    _bar1Fill.style.width = new Length(12f, LengthUnit.Percent);
                    _bar1Fill.style.backgroundColor = viewModel.CriticalBarColor;
                }
                else
                {
                    // 정상 상태일 때는 실제 체력 비례하고 원래 색상
                    float percent1 = (float)hp1 / 40f * 100f;
                    _bar1Fill.style.width = new Length(percent1, LengthUnit.Percent);
                    _bar1Fill.style.backgroundColor = viewModel.NormalBarColor;
                }
            }

            // 두 번째 체력바 (41-80HP)
            if (_bar2Fill != null)
            {
                float percent2 = (float)hp2 / 40f * 100f;
                _bar2Fill.style.width = new Length(percent2, LengthUnit.Percent);
                _bar2Fill.style.backgroundColor = viewModel.NormalBarColor;
            }

            // 세 번째 체력바 (81-100HP)
            if (_bar3Fill != null && _bar3 != null)
            {
                if (hp3 > 0)
                {
                    _bar3.style.display = DisplayStyle.Flex;
                    float percent3 = (float)hp3 / 20f * 100f;
                    _bar3Fill.style.width = new Length(percent3, LengthUnit.Percent);
                    _bar3Fill.style.backgroundColor = viewModel.ThirdBarColor;
                }
                else
                {
                    _bar3.style.display = DisplayStyle.None;
                }
            }

            Debug.Log($"[HealthBarView] 체력바 UI 업데이트: {currentHP}/{maxHP} (바1: {hp1}/40, 바2: {hp2}/40, 바3: {hp3}/20)");
        }

        private void UpdateFaintOverlay(bool isFainted, float reviveTime)
        {
            if (_faintOverlay == null) return;

            if (isFainted)
            {
                // 기절 상태 진입
                _faintOverlay.RemoveFromClassList("ui-hidden");
                _faintOverlay.AddToClassList("ui-visible");
                _faintOverlay.style.opacity = 1f;

                Debug.Log($"[HealthBarView] 기절 오버레이 표시 ({reviveTime}초)");
            }
            else
            {
                // 기절 상태 해제
                _faintOverlay.AddToClassList("ui-hidden");
                _faintOverlay.RemoveFromClassList("ui-visible");
                _faintOverlay.style.opacity = 0f;

                Debug.Log("[HealthBarView] 기절 오버레이 숨김");
            }
        }

        private void UpdateAllUI()
        {
            if (viewModel == null) return;

            UpdateHealthBarUI(viewModel.CurrentHP, viewModel.MaxHP);
            UpdateFaintOverlay(viewModel.IsFainted, viewModel.FaintTimeRemaining);
        }

        #endregion

        #region Public API

        public void SetHealthBarVisibility(bool visible)
        {
            if (_healthBar != null)
            {
                _healthBar.RemoveFromClassList("hidden");
                _healthBar.RemoveFromClassList("ui-hidden");

                if (visible)
                {
                    _healthBar.style.display = DisplayStyle.Flex;
                    _healthBar.AddToClassList("ui-visible");
                }
                else
                {
                    _healthBar.style.display = DisplayStyle.None;
                    _healthBar.AddToClassList("ui-hidden");
                }

                Debug.Log($"[HealthBarView] 체력바 가시성 설정: {visible}");
            }
        }

        public void SetFaintOverlayVisibility(bool visible)
        {
            if (_faintOverlay != null)
            {
                _faintOverlay.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void AnimateHealthChange(int targetHP, float duration = -1f)
        {
            if (duration < 0) duration = healthChangeAnimationDuration;
            StartCoroutine(AnimateHealthChangeCoroutine(targetHP, duration));
        }

        private IEnumerator AnimateHealthChangeCoroutine(int targetHP, float duration)
        {
            int startHP = viewModel.CurrentHP;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = healthChangeAnimationCurve.Evaluate(elapsed / duration);

                int interpolatedHP = Mathf.RoundToInt(Mathf.Lerp(startHP, targetHP, progress));

                // 직접 UI만 업데이트 (ViewModel 이벤트 발생 방지)
                UpdateHealthBarUI(interpolatedHP, viewModel.MaxHP);

                yield return null;
            }

            // 최종 값으로 ViewModel 업데이트
            if (viewModel != null)
            {
                viewModel.SetHealth(targetHP);
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            SubscribeToViewModel();
        }

        private void OnDisable()
        {
            UnsubscribeFromViewModel();
        }

        private void OnDestroy()
        {
            UnsubscribeFromViewModel();
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            Debug.Log($"[HealthBarView] UI State:\n" +
                     $"  HealthBar: {(_healthBar?.style.display.value == DisplayStyle.Flex ? "Visible" : "Hidden")}\n" +
                     $"  FaintOverlay: {(_faintOverlay?.style.display.value == DisplayStyle.Flex ? "Visible" : "Hidden")}\n" +
                     $"  Bar1Fill Width: {_bar1Fill?.style.width.value.value ?? 0}%\n" +
                     $"  Bar2Fill Width: {_bar2Fill?.style.width.value.value ?? 0}%\n" +
                     $"  Bar3Fill Width: {_bar3Fill?.style.width.value.value ?? 0}%\n" +
                     $"  ViewModel: {(viewModel != null ? "Connected" : "NULL")}");
        }

        #endregion
    }
}