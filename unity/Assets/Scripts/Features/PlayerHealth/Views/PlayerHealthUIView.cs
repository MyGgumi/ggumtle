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

        // 체력 변화 추적용 필드
        private int _lastKnownHp = 100;
        private bool _isAnimatingHealthChange = false;

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

            if (enableDebugLogs)
                Debug.Log("[PlayerHealthUIView] 초기화 시작");

            CacheUIElements();
            SubscribeToViewModel();
            SetupHealthBars();
            InitializeSprites();

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthUIView] 초기화 완료 - 초기 체력: {_lastKnownHp}, ViewModel 연결: {viewModel != null}");
            }
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
                .Subscribe(hp => OnHealthChanged(hp.current, hp.max))
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
                _lastKnownHp = viewModel.CurrentHp.CurrentValue;
                UpdateAllUI();

                if (enableDebugLogs)
                    Debug.Log($"[PlayerHealthUIView] 초기 체력 설정: 100/100, LastKnownHP: {_lastKnownHp}");
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

        private void OnHealthChanged(int currentHP, int maxHP)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthUIView] OnHealthChanged 호출: HP={currentHP}/{maxHP}, LastKnown={_lastKnownHp}, IsAnimating={_isAnimatingHealthChange}");
            }

            // 애니메이션 진행 중이면 완전히 무시
            if (_isAnimatingHealthChange)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[PlayerHealthUIView] 애니메이션 진행 중 - 호출 무시");
                }
                return;
            }

            // 체력 변화가 있으면 애니메이션 시작
            if (currentHP != _lastKnownHp)
            {
                bool isDecrease = currentHP < _lastKnownHp;
                int difference = Mathf.Abs(currentHP - _lastKnownHp);

                if (enableDebugLogs)
                {
                    string direction = isDecrease ? "감소" : "증가";
                    Debug.Log($"[PlayerHealthUIView] 체력 {direction}: {_lastKnownHp} → {currentHP} (차이: {difference})");
                }

                // 큰 변화량의 경우 애니메이션 지속시간 조정
                float animDuration = difference > 20 ? healthChangeAnimationDuration * 1.5f : healthChangeAnimationDuration;

                // 애니메이션 실행
                StartCoroutine(AnimateHealthChangeCoroutine(currentHP, animDuration));
                _lastKnownHp = currentHP;
            }
            // 체력 변화가 없으면 즉시 업데이트 (초기화용)
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[PlayerHealthUIView] 체력 변화 없음 - 즉시 UI 업데이트: {currentHP}");
                }
                UpdateHealthBarUI(currentHP, maxHP);
            }
        }

        private void UpdateHealthBarUI(int currentHP, int maxHP)
        {
            // 체력바는 뒤에서부터 순서대로 줄어듦: hp3(81-100) → hp2(41-80) → hp1(1-40)
            int hp1, hp2, hp3;

            if (currentHP > 80)
            {
                // 81-100HP: 1,2번째 바는 가득 참, 3번째 바만 변동
                hp1 = 40;
                hp2 = 40;
                hp3 = currentHP - 80; // 1-20
            }
            else if (currentHP > 40)
            {
                // 41-80HP: 1번째 바는 가득 참, 2번째 바만 변동, 3번째 바는 빔
                hp1 = 40;
                hp2 = currentHP - 40; // 1-40
                hp3 = 0;
            }
            else if (currentHP > 0)
            {
                // 1-40HP: 1번째 바만 변동, 2,3번째 바는 빔
                hp1 = currentHP; // 1-40
                hp2 = 0;
                hp3 = 0;
            }
            else
            {
                // 0HP: 모든 바 빔 (크리티컬 상태 표시)
                hp1 = 0;
                hp2 = 0;
                hp3 = 0;
            }

            // 첫 번째 체력바 (0-40HP)
            if (_bar1Fill != null)
            {
                if (currentHP <= 0)
                {
                    // 체력이 0일 때: 12% 고정 폭으로 표시하되 강렬한 빨간색으로 유지
                    _bar1Fill.style.width = new Length(12f, LengthUnit.Percent);
                    _bar1Fill.style.backgroundColor = viewModel.CriticalBarColor.CurrentValue;

                    // 기절/사망 상태의 추가 시각적 피드백
                    if (_bar1Container != null)
                    {
                        _bar1Container.AddToClassList("critical-state");
                        // 체력바 자체도 보이도록 유지
                        _bar1Container.style.display = DisplayStyle.Flex;
                        _bar1Container.style.opacity = 1f;
                    }

                    // 체력바 Fill도 확실히 보이도록 설정
                    _bar1Fill.style.display = DisplayStyle.Flex;
                    _bar1Fill.style.opacity = 1f;

                    if (enableDebugLogs)
                    {
                        Debug.Log("[PlayerHealthUIView] 체력 0 - 크리티컬 상태 시각화 적용 (12% 고정 표시)");
                    }
                }
                else
                {
                    // 정상 상태일 때는 실제 체력 비례하고 원래 색상
                    float percent1 = (float)hp1 / 40f * 100f;
                    _bar1Fill.style.width = new Length(percent1, LengthUnit.Percent);
                    _bar1Fill.style.backgroundColor = viewModel.NormalBarColor.CurrentValue;

                    // 위험 상태 클래스 제거
                    if (_bar1Container != null)
                    {
                        _bar1Container.RemoveFromClassList("critical-state");
                    }
                }
            }

            // 두 번째 체력바 (41-80HP)
            if (_bar2Fill != null)
            {
                float percent2 = (float)hp2 / 40f * 100f;
                _bar2Fill.style.width = new Length(percent2, LengthUnit.Percent);

                // 체력이 낮을 때 색상 변경
                if (currentHP <= 20 && currentHP > 0)
                {
                    // 체력이 20 이하일 때 경고 색상
                    _bar2Fill.style.backgroundColor = Color.Lerp(
                        viewModel.CriticalBarColor.CurrentValue,
                        viewModel.NormalBarColor.CurrentValue,
                        currentHP / 20f
                    );
                }
                else
                {
                    _bar2Fill.style.backgroundColor = viewModel.NormalBarColor.CurrentValue;
                }
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
                float bar1Percent = _bar1Fill?.style.width.value.value ?? 0;
                float bar2Percent = _bar2Fill?.style.width.value.value ?? 0;
                float bar3Percent = _bar3Fill?.style.width.value.value ?? 0;

                Debug.Log(
                    $"[PlayerHealthUIView] 체력바 UI 업데이트: {currentHP}/{maxHP}\n" +
                    $"  구간별 HP: 바1={hp1}/40, 바2={hp2}/40, 바3={hp3}/20\n" +
                    $"  바 퍼센트: 바1={bar1Percent:F1}%, 바2={bar2Percent:F1}%, 바3={bar3Percent:F1}%\n" +
                    $"  크리티컬 상태: {(currentHP <= 0 ? "적용" : "해제")}"
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
                    _healthBarContainer.style.opacity = 1f;
                    _healthBarContainer.AddToClassList("ui-visible");

                    if (enableDebugLogs)
                    {
                        Debug.Log("[PlayerHealthUIView] 체력바 표시 설정");
                    }
                }
                else
                {
                    // 체력이 0이어도 완전히 숨기지 않고 약간 투명하게 처리
                    if (viewModel?.CurrentHp.CurrentValue <= 0)
                    {
                        _healthBarContainer.style.display = DisplayStyle.Flex;
                        _healthBarContainer.style.opacity = 0.8f; // 완전히 사라지지 않게

                        if (enableDebugLogs)
                        {
                            Debug.Log("[PlayerHealthUIView] 체력 0 - 체력바 반투명 처리");
                        }
                    }
                    else
                    {
                        _healthBarContainer.style.display = DisplayStyle.None;
                        _healthBarContainer.AddToClassList("ui-hidden");
                    }
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
            _isAnimatingHealthChange = true;
            int startHP = _lastKnownHp;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthUIView] 순차 체력 애니메이션 시작: {startHP} → {targetHP} ({duration}초)");
            }

            // 순차 애니메이션을 위해 체력 구간별로 처리
            yield return StartCoroutine(AnimateHealthSequentially(startHP, targetHP, duration));

            // 최종 값으로 UI 업데이트
            UpdateHealthBarUI(targetHP, viewModel?.MaxHp.CurrentValue ?? 100);
            _isAnimatingHealthChange = false;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthUIView] 순차 체력 애니메이션 완료: {targetHP}");
            }
        }

        private IEnumerator AnimateHealthSequentially(int startHP, int targetHP, float totalDuration)
        {
            if (startHP == targetHP) yield break;

            // 각 체력바별 애니메이션 단계 계산
            var steps = CalculateAnimationSteps(startHP, targetHP);

            if (steps.Count == 0) yield break;

            // 단계별 시간 분배 (마지막 단계는 조금 더 길게)
            float stepDuration = totalDuration / (steps.Count + 0.5f);

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthUIView] 애니메이션 단계: {steps.Count}개, 단계별 시간: {stepDuration:F2}초");
            }

            for (int i = 0; i < steps.Count; i++)
            {
                var step = steps[i];

                // 각 단계별 애니메이션 실행
                yield return StartCoroutine(AnimateSingleBarStep(step.Item1, step.Item2, stepDuration));

                // 단계 사이에 아주 짧은 대기 (깜빡거림 방지)
                if (i < steps.Count - 1)
                {
                    yield return new WaitForSeconds(0.05f);
                }
            }
        }

        private System.Collections.Generic.List<(int startHP, int endHP)> CalculateAnimationSteps(int startHP, int targetHP)
        {
            var steps = new System.Collections.Generic.List<(int, int)>();

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthUIView] 단계 계산: {startHP} → {targetHP}");
            }

            if (startHP > targetHP)
            {
                // 체력 감소: hp3 → hp2 → hp1 순서로 하나씩
                int currentHP = startHP;

                // 3번째 바 (81-100) 처리
                if (currentHP > 80)
                {
                    int nextHP = Mathf.Max(targetHP, 80);
                    if (nextHP < currentHP)
                    {
                        steps.Add((currentHP, nextHP));
                        currentHP = nextHP;
                        if (enableDebugLogs)
                            Debug.Log($"[PlayerHealthUIView] HP3 단계: {steps[steps.Count-1].Item1} → {steps[steps.Count-1].Item2}");
                    }
                }

                // 2번째 바 (41-80) 처리
                if (currentHP > 40 && targetHP < currentHP)
                {
                    int nextHP = Mathf.Max(targetHP, 40);
                    if (nextHP < currentHP)
                    {
                        steps.Add((currentHP, nextHP));
                        currentHP = nextHP;
                        if (enableDebugLogs)
                            Debug.Log($"[PlayerHealthUIView] HP2 단계: {steps[steps.Count-1].Item1} → {steps[steps.Count-1].Item2}");
                    }
                }

                // 1번째 바 (1-40) 처리
                if (currentHP > 0 && targetHP < currentHP)
                {
                    steps.Add((currentHP, targetHP));
                    if (enableDebugLogs)
                        Debug.Log($"[PlayerHealthUIView] HP1 단계: {steps[steps.Count-1].Item1} → {steps[steps.Count-1].Item2}");
                }
            }
            else if (startHP < targetHP)
            {
                // 체력 회복: hp1 → hp2 → hp3 순서로 하나씩
                int currentHP = startHP;

                // 1번째 바 (1-40) 처리
                if (currentHP <= 40 && targetHP > currentHP)
                {
                    int nextHP = Mathf.Min(targetHP, 40);
                    if (nextHP > currentHP)
                    {
                        steps.Add((currentHP, nextHP));
                        currentHP = nextHP;
                        if (enableDebugLogs)
                            Debug.Log($"[PlayerHealthUIView] HP1 회복: {steps[steps.Count-1].Item1} → {steps[steps.Count-1].Item2}");
                    }
                }

                // 2번째 바 (41-80) 처리
                if (currentHP <= 80 && targetHP > currentHP)
                {
                    int nextHP = Mathf.Min(targetHP, 80);
                    if (nextHP > currentHP)
                    {
                        steps.Add((currentHP, nextHP));
                        currentHP = nextHP;
                        if (enableDebugLogs)
                            Debug.Log($"[PlayerHealthUIView] HP2 회복: {steps[steps.Count-1].Item1} → {steps[steps.Count-1].Item2}");
                    }
                }

                // 3번째 바 (81-100) 처리
                if (currentHP <= 100 && targetHP > currentHP)
                {
                    steps.Add((currentHP, targetHP));
                    if (enableDebugLogs)
                        Debug.Log($"[PlayerHealthUIView] HP3 회복: {steps[steps.Count-1].Item1} → {steps[steps.Count-1].Item2}");
                }
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthUIView] 총 {steps.Count}개 단계 계산 완료");
            }

            return steps;
        }

        private IEnumerator AnimateSingleBarStep(int startHP, int endHP, float duration)
        {
            float elapsed = 0f;

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthUIView] 바 단계 애니메이션 시작: {startHP} → {endHP} ({duration:F2}초)");
            }

            // 애니메이션 시작 전 현재 상태 고정
            UpdateHealthBarUI(startHP, viewModel?.MaxHp.CurrentValue ?? 100);
            yield return new WaitForSeconds(0.02f); // 짧은 대기로 시작 상태 안정화

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float smoothProgress = healthChangeAnimationCurve.Evaluate(progress);

                int interpolatedHP = Mathf.RoundToInt(Mathf.Lerp(startHP, endHP, smoothProgress));

                // 직접 UI만 업데이트 (ViewModel 이벤트 발생 방지)
                UpdateHealthBarUI(interpolatedHP, viewModel?.MaxHp.CurrentValue ?? 100);

                yield return null;
            }

            // 단계 완료 - 최종 값으로 확실히 설정
            UpdateHealthBarUI(endHP, viewModel?.MaxHp.CurrentValue ?? 100);

            if (enableDebugLogs)
            {
                Debug.Log($"[PlayerHealthUIView] 바 단계 애니메이션 완료: {endHP}");
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
