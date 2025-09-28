using System;
using Features.Mongdung.ViewModels;
using Features.Mongdung.Services;
using Features.Mongdung.Models;
using Features.Mongdung.Messages;
using MessagePipe;
using R3;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Features.Mongdung.Views
{
    /// <summary>
    /// 몽둥이 UI View - 스킬 버튼과 함정 슬롯을 관리
    /// MongdungViewModel과 R3를 통해 반응형 UI 구현
    /// </summary>
    public class MongdungUIView : MonoBehaviour, IDisposable
    {
        #region Private Fields

        [Inject] private MongdungViewModel _mongdungViewModel;
        [Inject] private IMongdungService _mongdungService;
        [Inject] private IPublisher<MongdungActionRequestMessage> _actionRequestPublisher;

        private VisualElement _root;
        private VisualElement _attackButton;
        private VisualElement _frightenButton;
        private VisualElement _trapSlot;
        private Label _trapCountLabel;


        private readonly CompositeDisposable _disposables = new();
        private readonly bool _enableDebugLogs = true;

        #endregion

        #region Initialization

        /// <summary>
        /// HUDInitializer에서 호출되는 초기화 메서드
        /// </summary>
        public void Initialize(VisualElement root)
        {
            _root = root;

            // VContainer 의존성 주입 확인
            if (_mongdungViewModel == null)
            {
                Debug.LogError("[MongdungUIView] MongdungViewModel이 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            if (_mongdungService == null)
            {
                Debug.LogError("[MongdungUIView] MongdungService가 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            if (_actionRequestPublisher == null)
            {
                Debug.LogError("[MongdungUIView] ActionRequestPublisher가 주입되지 않았습니다! VContainer 설정을 확인하세요.");
                return;
            }

            InitializeUI();
            BindToViewModel();

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungUIView] 초기화 완료");
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        #endregion

        #region UI Initialization

        /// <summary>
        /// UI 요소들 초기화
        /// </summary>
        private void InitializeUI()
        {

            // MongdungUI 인스턴스에서 UI 요소 참조
            var mongdungUIInstance = _root.Q<VisualElement>("mongdungUI");
            if (mongdungUIInstance == null)
            {
                Debug.LogError("[MongdungUIView] mongdungUI 인스턴스를 찾을 수 없습니다");
                return;
            }

            _attackButton = mongdungUIInstance.Q<VisualElement>("attackButton");
            _frightenButton = mongdungUIInstance.Q<VisualElement>("frightenButton");
            _trapSlot = mongdungUIInstance.Q<VisualElement>("trapSlot");
            _trapCountLabel = mongdungUIInstance.Q<Label>("trapCount");

            if (_attackButton == null || _frightenButton == null || _trapSlot == null || _trapCountLabel == null)
            {
                Debug.LogError("[MongdungUIView] 필수 UI 요소를 찾을 수 없습니다");
                return;
            }

            // 버튼 이벤트 등록
            RegisterButtonEvents();

            // ViewModel 구독 설정
            SubscribeToViewModel();

            // 초기 함정 개수는 ViewModel에서 자동으로 설정됨
        }

        /// <summary>
        /// 버튼 이벤트 등록
        /// </summary>
        private void RegisterButtonEvents()
        {
            _attackButton.RegisterCallback<ClickEvent>(evt => OnAttackButtonClicked());
            _frightenButton.RegisterCallback<ClickEvent>(evt => OnFrightenButtonClicked());
            _trapSlot.RegisterCallback<ClickEvent>(evt => OnTrapSlotClicked());
        }

        /// <summary>
        /// ViewModel 구독 설정
        /// </summary>
        private void SubscribeToViewModel()
        {
            if (_mongdungViewModel == null)
            {
                Debug.LogError("[MongdungUIView] MongdungViewModel이 주입되지 않아 구독 실패");
                return;
            }

            // 함정 개수 구독
            _mongdungViewModel.TrapCount
                .Subscribe(OnTrapCountChanged)
                .AddTo(_disposables);

            Debug.Log("[MongdungUIView] ViewModel 구독 설정 완료");
        }

        /// <summary>
        /// 함정 개수 변경 시 UI 업데이트
        /// </summary>
        private void OnTrapCountChanged(int trapCount)
        {
            if (_trapCountLabel != null)
            {
                _trapCountLabel.text = trapCount.ToString();
                Debug.Log($"[MongdungUIView] 함정 개수 UI 업데이트: {trapCount}");
            }

            // 함정 개수가 0개일 때 버튼 비활성화
            UpdateTrapSlotState(trapCount > 0);
        }

        /// <summary>
        /// 함정 슬롯 활성화/비활성화 상태 업데이트
        /// </summary>
        private void UpdateTrapSlotState(bool isEnabled)
        {
            if (_trapSlot != null)
            {
                // 버튼 활성화/비활성화
                _trapSlot.SetEnabled(isEnabled);

                // 시각적 피드백 (투명도 조절)
                _trapSlot.style.opacity = isEnabled ? 1.0f : 0.5f;

                if (!isEnabled)
                {
                    // 비활성화 상태일 때 추가 스타일 적용
                    _trapSlot.AddToClassList("disabled");
                }
                else
                {
                    // 활성화 상태일 때 비활성화 스타일 제거
                    _trapSlot.RemoveFromClassList("disabled");
                }

                Debug.Log($"[MongdungUIView] 함정 슬롯 상태 업데이트: {(isEnabled ? "활성화" : "비활성화")}");
            }
        }

        #endregion

        #region ViewModel Binding

        /// <summary>
        /// ViewModel과 바인딩
        /// </summary>
        private void BindToViewModel()
        {
            if (_mongdungViewModel == null)
            {
                Debug.LogError("[MongdungUIView] MongdungViewModel이 주입되지 않았습니다");
                return;
            }

            // 공격 버튼 상태 바인딩
            BindActionButton(_attackButton, MongdungActionType.Attack);

            // 위협 버튼 상태 바인딩
            BindActionButton(_frightenButton, MongdungActionType.Frighten);

            // 함정 슬롯 상태 바인딩 (TrapSetting 액션과 연동)
            BindTrapSlot();

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungUIView] ViewModel 바인딩 완료");
            }
        }

        /// <summary>
        /// 액션 버튼과 ViewModel 상태 바인딩
        /// </summary>
        private void BindActionButton(VisualElement button, MongdungActionType actionType)
        {
            // 실행 상태 바인딩
            _mongdungViewModel.GetActionExecuting(actionType)
                .Subscribe(isExecuting =>
                {
                    if (isExecuting)
                    {
                        button.AddToClassList("active");
                        button.RemoveFromClassList("cooldown");
                    }
                    else
                    {
                        button.RemoveFromClassList("active");
                    }
                })
                .AddTo(_disposables);

            // 쿨다운 상태 바인딩
            _mongdungViewModel.GetActionCooldown(actionType)
                .Subscribe(isCooldown =>
                {
                    if (isCooldown)
                    {
                        button.AddToClassList("cooldown");
                        button.RemoveFromClassList("active");
                    }
                    else
                    {
                        button.RemoveFromClassList("cooldown");
                    }
                })
                .AddTo(_disposables);

            // 남은 시간 바인딩 (필요시 UI에 표시)
            _mongdungViewModel.GetActionRemainingTime(actionType)
                .Subscribe(remainingTime =>
                {
                    // 남은 시간이 0보다 크면 버튼 비활성화, 0이면 활성화
                    bool isDisabled = remainingTime > 0;
                    button.SetEnabled(!isDisabled);

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[MongdungUIView] {actionType} 버튼 상태 업데이트: remainingTime={remainingTime}, enabled={!isDisabled}");
                    }
                })
                .AddTo(_disposables);
        }

        /// <summary>
        /// 함정 슬롯 바인딩
        /// </summary>
        private void BindTrapSlot()
        {
            // 함정 설치 쿨다운 상태에 따라 슬롯 투명도 조절
            _mongdungViewModel.GetActionCooldown(MongdungActionType.TrapSetting)
                .Subscribe(isCooldown =>
                {
                    _trapSlot.style.opacity = isCooldown ? 0.5f : 1.0f;
                })
                .AddTo(_disposables);

            // 함정 개수는 현재 고정값 사용 (추후 서비스에서 동적으로 가져올 수 있음)
            // TODO: 실제 함정 개수를 MongdungService에서 가져오도록 확장
        }



        #endregion

        #region Button Event Handlers

        /// <summary>
        /// 공격 버튼 클릭 처리
        /// </summary>
        private void OnAttackButtonClicked()
        {
            if (!_mongdungViewModel.CanExecuteAction(MongdungActionType.Attack))
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[MongdungUIView] 공격 액션을 실행할 수 없습니다");
                }
                return;
            }

            var message = new MongdungActionRequestMessage(
                _mongdungViewModel.PlayerId,
                MongdungActionType.Attack,
                transform.position,
                transform.forward,
                -1, // targetId는 나중에 AttackHitDetector에서 처리
                "UIClick"
            );

            _actionRequestPublisher.Publish(message);

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungUIView] 공격 액션 요청 메시지 발행");
            }
        }

        /// <summary>
        /// 위협 버튼 클릭 처리
        /// </summary>
        private void OnFrightenButtonClicked()
        {
            if (!_mongdungViewModel.CanExecuteAction(MongdungActionType.Frighten))
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[MongdungUIView] 위협 액션을 실행할 수 없습니다");
                }
                return;
            }

            var message = new MongdungActionRequestMessage(
                _mongdungViewModel.PlayerId,
                MongdungActionType.Frighten,
                transform.position,
                transform.forward,
                -1,
                "UIClick"
            );

            _actionRequestPublisher.Publish(message);

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungUIView] 위협 액션 요청 메시지 발행");
            }
        }

        /// <summary>
        /// 함정 슬롯 클릭 처리
        /// </summary>
        private void OnTrapSlotClicked()
        {
            // 함정 개수 확인
            if (_mongdungViewModel.TrapCount.CurrentValue <= 0)
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[MongdungUIView] 함정 개수가 0개입니다. 스킬을 사용할 수 없습니다.");
                }
                return;
            }

            if (!_mongdungViewModel.CanExecuteAction(MongdungActionType.TrapSetting))
            {
                if (_enableDebugLogs)
                {
                    Debug.Log("[MongdungUIView] 함정 설치 액션을 실행할 수 없습니다");
                }
                return;
            }

            var message = new MongdungActionRequestMessage(
                _mongdungViewModel.PlayerId,
                MongdungActionType.TrapSetting,
                transform.position,
                transform.forward,
                -1,
                "UIClick"
            );

            _actionRequestPublisher.Publish(message);

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungUIView] 함정 설치 액션 요청 메시지 발행");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// UI 표시/숨김
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_enableDebugLogs)
            {
                Debug.Log($"[MongdungUIView] SetVisible 호출됨: {visible}");
                Debug.Log($"[MongdungUIView] _root 존재 여부: {_root != null}");
            }

            if (_root != null)
            {
                var mongdungUIInstance = _root.Q<VisualElement>("mongdungUI");
                if (_enableDebugLogs)
                {
                    Debug.Log($"[MongdungUIView] mongdungUI 인스턴스 찾기 결과: {mongdungUIInstance != null}");
                }

                if (mongdungUIInstance != null)
                {
                    mongdungUIInstance.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

                    if (_enableDebugLogs)
                    {
                        Debug.Log($"[MongdungUIView] mongdungUI display 설정 완료: {(visible ? "Flex" : "None")}");
                    }
                }
                else
                {
                    Debug.LogError("[MongdungUIView] mongdungUI 인스턴스를 찾을 수 없습니다!");
                }
            }
            else
            {
                Debug.LogError("[MongdungUIView] _root가 null입니다!");
            }
        }

        /// <summary>
        /// 함정 슬롯 배경 스프라이트 설정 (UIAssetService에서 호출)
        /// </summary>
        public void SetSlotBackgroundSprite(Sprite backgroundSprite)
        {
            if (_enableDebugLogs)
                Debug.Log($"[MongdungUIView] SetSlotBackgroundSprite 호출됨. Sprite: {(backgroundSprite != null ? backgroundSprite.name : "NULL")}");

            if (backgroundSprite == null)
            {
                if (_enableDebugLogs)
                    Debug.LogWarning("[MongdungUIView] 배경 스프라이트가 null입니다.");
                return;
            }

            if (_trapSlot != null)
            {
                _trapSlot.style.backgroundImage = new StyleBackground(backgroundSprite);
                _trapSlot.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);

                if (_enableDebugLogs)
                    Debug.Log($"[MongdungUIView] 함정 슬롯에 배경 스프라이트 적용: {backgroundSprite.name}");
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            _disposables?.Dispose();

            if (_enableDebugLogs)
            {
                Debug.Log("[MongdungUIView] Dispose 완료");
            }
        }

        #endregion
    }
}