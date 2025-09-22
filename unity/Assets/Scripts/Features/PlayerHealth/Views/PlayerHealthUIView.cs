using System;
using System.Collections;
using Features.PlayerHealth.Models;
using Features.PlayerHealth.ViewModels;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using R3DisposableBag = R3.CompositeDisposable;

namespace Features.PlayerHealth.Views
{
    public class PlayerHealthUIView : MonoBehaviour
    {
        [Header("ViewModel Reference")]
        [SerializeField]
        private PlayerHealthViewModel viewModel;

        [Header("UI References")]
        private VisualElement _root;
        private VisualElement _healthBarContainer;
        private VisualElement _bar1Container;
        private VisualElement _bar2Container;
        private VisualElement _bar3Container;
        private VisualElement _bar1Fill;
        private VisualElement _bar2Fill;
        private VisualElement _bar3Fill;
        private Label _healthText;
        private Label _stateText;
        private VisualElement _faintTimerContainer;
        private Label _faintTimerText;
        private VisualElement _warningIndicator;
        private VisualElement _criticalIndicator;

        [Header("Sprites")]
        [SerializeField]
        private Sprite hpIconSprite;

        [Header("Settings")]
        [SerializeField]
        private bool enableDebugLogs = true;

        private CompositeDisposable _disposables = new();

        [Inject]
        public void Construct(PlayerHealthViewModel playerHealthViewModel)
        {
            viewModel = playerHealthViewModel;
            if (enableDebugLogs)
                Debug.Log($"[PlayerHealthUIView] VContainer 의존성 주입 완료: {viewModel != null}");
        }

        public void Initialize(VisualElement root)
        {
            _root = root;

            if (viewModel == null)
            {
                Debug.LogError(
                    "[PlayerHealthUIView] ViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요."
                );
                return;
            }

            CacheUIElements();
            SubscribeToViewModel();
            SetupHealthBars();
            InitializeSprites();

            if (enableDebugLogs)
                Debug.Log("[PlayerHealthUIView] 초기화 완료");
        }

        private void CacheUIElements()
        {
            if (_root == null)
            {
                Debug.LogError("[PlayerHealthUIView] Root VisualElement가 null입니다.");
                return;
            }

            _healthBarContainer = _root.Q<VisualElement>("healthBar");
            _faintTimerContainer = _root.Q<VisualElement>("faintOverlay");

            _bar1Container = _root.Q<VisualElement>("bar1");
            _bar1Fill = _root.Q<VisualElement>("bar1Fill");
            _bar2Container = _root.Q<VisualElement>("bar2");
            _bar2Fill = _root.Q<VisualElement>("bar2Fill");
            _bar3Container = _root.Q<VisualElement>("bar3");
            _bar3Fill = _root.Q<VisualElement>("bar3Fill");

            _healthText = _root.Q<Label>("healthText");
            _stateText = _root.Q<Label>("stateText");
            _faintTimerText = _root.Q<Label>("faintTimerText");
            _warningIndicator = _root.Q<VisualElement>("warningIndicator");
            _criticalIndicator = _root.Q<VisualElement>("criticalIndicator");

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerHealthUIView] UI 요소 캐싱 완료: "
                        + $"HealthBar={(_healthBarContainer != null ? "OK" : "NULL")}, "
                        + $"Bar1={(_bar1Container != null ? "OK" : "NULL")}, "
                        + $"Bar2={(_bar2Container != null ? "OK" : "NULL")}, "
                        + $"Bar3={(_bar3Container != null ? "OK" : "NULL")}"
                );
            }
        }

        private void SubscribeToViewModel()
        {
            if (viewModel == null)
                return;

            viewModel.HealthText.Subscribe(text => UpdateHealthText(text)).AddTo(_disposables);

            viewModel.StateText.Subscribe(text => UpdateStateText(text)).AddTo(_disposables);

            viewModel.CurrentHp
                .CombineLatest(viewModel.MaxHp, (current, max) => new { current, max })
                .Subscribe(hp => UpdateHealthBarUI(hp.current, hp.max))
                .AddTo(_disposables);

            viewModel
                .ShouldShowFaintTimer.Subscribe(shouldShow => SetFaintTimerVisibility(shouldShow))
                .AddTo(_disposables);

            viewModel
                .FaintTimerText.Subscribe(text => UpdateFaintTimerText(text))
                .AddTo(_disposables);

            viewModel
                .ShouldShowHealthWarning.Subscribe(shouldShow =>
                    SetWarningIndicatorVisibility(shouldShow)
                )
                .AddTo(_disposables);

            viewModel
                .ShouldShowCriticalState.Subscribe(shouldShow =>
                    SetCriticalIndicatorVisibility(shouldShow)
                )
                .AddTo(_disposables);

            viewModel
                .IsAlive.Subscribe(isAlive => SetHealthBarVisibility(isAlive))
                .AddTo(_disposables);

            viewModel
                .CurrentState.Subscribe(state => UpdateHealthBarColors(state))
                .AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[PlayerHealthUIView] ViewModel 구독 완료");
        }

        private void SetupHealthBars()
        {
            // 초기 체력 설정 (100HP)
            if (viewModel != null)
            {
                viewModel.SetHealth(100, 100);
                UpdateAllUI();

                if (enableDebugLogs)
                    Debug.Log("[PlayerHealthUIView] 초기 체력 설정: 100/100");
            }
        }

        #region Animation and Timing

        [Header("Animation Settings")]
        [SerializeField]
        private float healthChangeAnimationDuration = 0.5f;

        [SerializeField]
        private AnimationCurve healthChangeAnimationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #endregion

        #region UI Updates

        private void UpdateHealthBarUI(int currentHP, int maxHP)
        {
            // 체력바는 40HP씩 3개 구간으로 나눠짐 (총 120HP 표시 가능, 실제 최대는 100HP)
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
                    _bar1Fill.style.backgroundColor = viewModel.CriticalBarColor.CurrentValue;
                }
                else
                {
                    // 정상 상태일 때는 실제 체력 비례하고 원래 색상
                    float percent1 = (float)hp1 / 40f * 100f;
                    _bar1Fill.style.width = new Length(percent1, LengthUnit.Percent);
                    _bar1Fill.style.backgroundColor = viewModel.NormalBarColor.CurrentValue;
                }
            }

            // 두 번째 체력바 (41-80HP)
            if (_bar2Fill != null)
            {
                float percent2 = (float)hp2 / 40f * 100f;
                _bar2Fill.style.width = new Length(percent2, LengthUnit.Percent);
                _bar2Fill.style.backgroundColor = viewModel.NormalBarColor.CurrentValue;
            }

            // 세 번째 체력바 (81-100HP)
            if (_bar3Fill != null && _bar3Container != null)
            {
                if (hp3 > 0)
                {
                    _bar3Container.style.display = DisplayStyle.Flex;
                    float percent3 = (float)hp3 / 20f * 100f;
                    _bar3Fill.style.width = new Length(percent3, LengthUnit.Percent);
                    _bar3Fill.style.backgroundColor = viewModel.ThirdBarColor.CurrentValue;
                }
                else
                {
                    _bar3Container.style.display = DisplayStyle.None;
                }
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[PlayerHealthUIView] 체력바 UI 업데이트: {currentHP}/{maxHP} (바1: {hp1}/40, 바2: {hp2}/40, 바3: {hp3}/20)"
                );
            }
        }

        private void UpdateHealthBarColors(PlayerState state)
        {
            // 체력바 색상은 UpdateHealthBarUI에서 처리하므로 여기서는 비워둔
        }

        private void UpdateHealthText(string text)
        {
            if (_healthText != null)
                _healthText.text = text;
        }

        private void UpdateStateText(string text)
        {
            if (_stateText != null)
                _stateText.text = text;
        }

        private void UpdateFaintTimerText(string text)
        {
            if (_faintTimerText != null)
                _faintTimerText.text = text;
        }

        private void UpdateFaintOverlay(bool isFainted)
        {
            if (_faintTimerContainer == null)
                return;

            if (isFainted)
            {
                _faintTimerContainer.RemoveFromClassList("ui-hidden");
                _faintTimerContainer.AddToClassList("ui-visible");
                _faintTimerContainer.style.opacity = 1f;
            }
            else
            {
                _faintTimerContainer.AddToClassList("ui-hidden");
                _faintTimerContainer.RemoveFromClassList("ui-visible");
                _faintTimerContainer.style.opacity = 0f;
            }
        }

        private void SetHealthBarVisibility(bool visible)
        {
            if (_healthBarContainer != null)
            {
                _healthBarContainer.RemoveFromClassList("hidden");
                _healthBarContainer.RemoveFromClassList("ui-hidden");

                if (visible)
                {
                    _healthBarContainer.style.display = DisplayStyle.Flex;
                    _healthBarContainer.AddToClassList("ui-visible");
                }
                else
                {
                    _healthBarContainer.style.display = DisplayStyle.None;
                    _healthBarContainer.AddToClassList("ui-hidden");
                }
            }
        }

        private void SetFaintTimerVisibility(bool visible)
        {
            UpdateFaintOverlay(visible);
        }

        private void SetWarningIndicatorVisibility(bool visible)
        {
            if (_warningIndicator != null)
                _warningIndicator.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void SetCriticalIndicatorVisibility(bool visible)
        {
            if (_criticalIndicator != null)
                _criticalIndicator.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

            if (_warningIndicator != null)
                _warningIndicator.style.display = visible
                    ? DisplayStyle.None
                    : _warningIndicator.style.display;
        }

        private void UpdateAllUI()
        {
            if (viewModel == null)
                return;

            UpdateHealthBarUI(viewModel.CurrentHp.CurrentValue, viewModel.MaxHp.CurrentValue);
            UpdateFaintOverlay(viewModel.IsFainted.CurrentValue);
        }

        #endregion

        #region Sprite Initialization

        private void InitializeSprites()
        {
            if (hpIconSprite != null)
            {
                var hpIconElement = _root?.Q<VisualElement>("hpIcon");
                if (hpIconElement != null)
                {
                    hpIconElement.style.backgroundImage = new StyleBackground(hpIconSprite);
                    hpIconElement.style.backgroundSize = new BackgroundSize(
                        BackgroundSizeType.Cover
                    );

                    if (enableDebugLogs)
                        Debug.Log("[PlayerHealthUIView] HP 아이콘 스프라이트 설정 완료");
                }
            }
        }

        public void SetHpIconSprite(Sprite sprite)
        {
            hpIconSprite = sprite;
            InitializeSprites();
        }

        #endregion

        #region Animation

        public void AnimateHealthChange(int targetHP, float duration = -1f)
        {
            if (duration < 0)
                duration = healthChangeAnimationDuration;
            StartCoroutine(AnimateHealthChangeCoroutine(targetHP, duration));
        }

        private IEnumerator AnimateHealthChangeCoroutine(int targetHP, float duration)
        {
            int startHP = viewModel?.CurrentHp.CurrentValue ?? 0;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = healthChangeAnimationCurve.Evaluate(elapsed / duration);

                int interpolatedHP = Mathf.RoundToInt(Mathf.Lerp(startHP, targetHP, progress));

                // 직접 UI만 업데이트 (ViewModel 이벤트 발생 방지)
                UpdateHealthBarUI(interpolatedHP, viewModel?.MaxHp.CurrentValue ?? 100);

                yield return null;
            }

            // 최종 값으로 ViewModel 업데이트
            if (viewModel != null)
            {
                viewModel.SetHealth(targetHP);
            }
        }

        #endregion

        #region Public API

        public void SetHealthBarContainerVisibility(bool visible)
        {
            SetHealthBarVisibility(visible);
        }

        public void SetFaintOverlayVisibility(bool visible)
        {
            if (_faintTimerContainer != null)
            {
                _faintTimerContainer.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void DamageHealth(int damage)
        {
            if (viewModel != null)
                viewModel.DamageHealth(damage);
        }

        public void HealHealth(int healAmount)
        {
            if (viewModel != null)
                viewModel.HealHealth(healAmount);
        }

        public void SetHealth(int newHp, int newMaxHp = -1)
        {
            if (viewModel != null)
                viewModel.SetHealth(newHp, newMaxHp);
        }

        public void RevivePlayer(int reviveHp = 30)
        {
            if (viewModel != null)
                viewModel.RevivePlayer(reviveHp);
        }

        public int GetCurrentHp()
        {
            return viewModel?.CurrentHp.CurrentValue ?? 0;
        }

        public int GetMaxHp()
        {
            return viewModel?.MaxHp.CurrentValue ?? 0;
        }

        public PlayerState GetCurrentState()
        {
            return viewModel?.CurrentState.CurrentValue ?? PlayerState.Normal;
        }

        public bool IsAlive()
        {
            return viewModel?.IsAlive.CurrentValue ?? false;
        }

        public bool IsFainted()
        {
            return viewModel?.IsFainted.CurrentValue ?? false;
        }

        #endregion

        #region Unity Lifecycle

        private void OnEnable()
        {
            if (viewModel != null)
                SubscribeToViewModel();
        }

        private void OnDisable()
        {
            _disposables?.Clear();
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();

            if (enableDebugLogs)
                Debug.Log("[PlayerHealthUIView] OnDestroy - Dispose 완료");
        }

        #endregion

        #region Debug Methods

        [ContextMenu("Log Current UI State")]
        public void LogCurrentUIState()
        {
            Debug.Log(
                $"[PlayerHealthUIView] UI State:\n"
                    + $"  HealthBar: {(_healthBarContainer?.style.display.value == DisplayStyle.Flex ? "Visible" : "Hidden")}\n"
                    + $"  FaintOverlay: {(_faintTimerContainer?.style.display.value == DisplayStyle.Flex ? "Visible" : "Hidden")}\n"
                    + $"  Bar1Fill Width: {_bar1Fill?.style.width.value.value ?? 0}%\n"
                    + $"  Bar2Fill Width: {_bar2Fill?.style.width.value.value ?? 0}%\n"
                    + $"  Bar3Fill Width: {_bar3Fill?.style.width.value.value ?? 0}%\n"
                    + $"  ViewModel: {(viewModel != null ? "Connected" : "NULL")}"
            );
        }

        #endregion
    }
}
