using System;
using Features.Mongdung.Models;
using Features.Mongdung.Services;
using Features.Mongdung.ViewModels;
using Features.Mongdung.Messages;
using Networks.Rooms.Domains;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Mongdung.Views
{
    /// <summary>
    /// 몽둥이 GameObject 컴포넌트
    /// 기존 MongdungSystem을 대체하는 새로운 Feature 기반 컴포넌트
    /// 3가지 액션(Attack, TrapSetting, Frighten) 처리 및 애니메이션 제어
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class MongdungGameObject : MonoBehaviour
    {
        [Header("몽둥이 액션 설정")]
        [SerializeField] private MongdungData mongdungData = new MongdungData();

        [Header("입력 설정")]
        [SerializeField] private KeyCode attackKey = KeyCode.Q;
        [SerializeField] private KeyCode trapKey = KeyCode.E;
        [SerializeField] private KeyCode frightenKey = KeyCode.R;

        [Header("Debug 설정")]
        [SerializeField] private bool enableDebugLogs = true;

        // 의존성 주입
        private IMongdungService _mongdungService;
        private MongdungViewModel _viewModel;
        private IPublisher<MongdungMovementBlockedMessage> _movementBlockedPublisher;

        // 컴포넌트
        private Animator _animator;
        private CompositeDisposable _disposables = new CompositeDisposable();

        // 플레이어 정보
        public long PlayerId { get; private set; }
        public string PlayerName { get; private set; }

        // 애니메이터 파라미터 해시
        private int _attackHash;
        private int _trapSettingHash;
        private int _frightenHash;

        // 현재 실행 중인 액션
        private MongdungActionType? _currentExecutingAction = null;

        [Inject]
        public void Construct(
            IMongdungService mongdungService,
            MongdungViewModel viewModel,
            IPublisher<MongdungMovementBlockedMessage> movementBlockedPublisher)
        {
            _mongdungService = mongdungService ?? throw new ArgumentNullException(nameof(mongdungService));
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _movementBlockedPublisher = movementBlockedPublisher;

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungGameObject] VContainer 의존성 주입 완료: {gameObject.name}");
            }
        }

        private void Start()
        {
            InitializeComponents();
            InitializeAnimatorParameters();
            SubscribeToViewModel();

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungGameObject] 초기화 완료: {gameObject.name}");
            }
        }

        private void Update()
        {
            // 로컬 플레이어만 입력 처리
            if (IsLocalPlayer())
            {
                HandleInput();
            }
            // Remote 플레이어는 네트워크 메시지만 수신해서 애니메이션 처리
        }

        /// <summary>
        /// PlayerPacket 데이터로 몽둥이 초기화
        /// </summary>
        public void InitializeFromPacket(PlayerPacket packet)
        {
            if (packet.IsMongging)
            {
                Debug.LogWarning("[MongdungGameObject] 몽깅이 플레이어에게 MongdungGameObject가 적용되었습니다!");
                return;
            }

            PlayerId = packet.Id;
            PlayerName = packet.NickName ?? $"Player_{packet.Id}";

            // 몽둥이 데이터 업데이트
            mongdungData.playerId = packet.Id;
            mongdungData.playerName = PlayerName;

            // ViewModel 초기화
            if (_viewModel != null)
            {
                _viewModel.Initialize(PlayerId, PlayerName);
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungGameObject] 패킷으로 초기화: PlayerId={PlayerId}, Name={PlayerName}");
            }
        }

        private void InitializeComponents()
        {
            // 하위 오브젝트들에서 Controller가 있는 Animator 찾기
            var animators = GetComponentsInChildren<Animator>();

            foreach (var animator in animators)
            {
                if (animator.runtimeAnimatorController != null)
                {
                    _animator = animator;
                    break;
                }
            }

            if (_animator == null)
            {
                Debug.LogError($"[MongdungGameObject] Controller가 할당된 Animator를 찾을 수 없습니다: {gameObject.name}");
                if (enableDebugLogs)
                {
                    Debug.Log($"[MongdungGameObject] 발견된 Animator 목록:");
                    for (int i = 0; i < animators.Length; i++)
                    {
                        Debug.Log($"  [{i}] {animators[i].gameObject.name} - Controller: {animators[i].runtimeAnimatorController?.name ?? "None"}");
                    }
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[MongdungGameObject] Controller가 있는 Animator 발견: {_animator.gameObject.name} (Controller: {_animator.runtimeAnimatorController.name})");
                }
            }
        }

        private void InitializeAnimatorParameters()
        {
            if (_animator != null)
            {
                _attackHash = Animator.StringToHash("Attack");
                _trapSettingHash = Animator.StringToHash("TrapSetting");
                _frightenHash = Animator.StringToHash("Frighten");

                if (enableDebugLogs)
                {
                    Debug.Log($"[MongdungGameObject] 애니메이터 파라미터 초기화 완료");
                }
            }
        }

        private void SubscribeToViewModel()
        {
            if (_viewModel == null)
            {
                Debug.LogError($"[MongdungGameObject] ViewModel이 null입니다: {gameObject.name}");
                return;
            }

            // 상태 변경 구독
            _viewModel.CurrentState.Subscribe(OnStateChanged).AddTo(_disposables);

            // 이동 제한 구독
            _viewModel.IsMovementBlocked.Subscribe(OnMovementBlockedChanged).AddTo(_disposables);

            // 각 액션 실행 상태 구독
            _viewModel.GetActionExecuting(MongdungActionType.Attack)
                .Subscribe(isExecuting => OnActionExecutingChanged(MongdungActionType.Attack, isExecuting))
                .AddTo(_disposables);

            _viewModel.GetActionExecuting(MongdungActionType.TrapSetting)
                .Subscribe(isExecuting => OnActionExecutingChanged(MongdungActionType.TrapSetting, isExecuting))
                .AddTo(_disposables);

            _viewModel.GetActionExecuting(MongdungActionType.Frighten)
                .Subscribe(isExecuting => OnActionExecutingChanged(MongdungActionType.Frighten, isExecuting))
                .AddTo(_disposables);

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungGameObject] ViewModel 구독 완료");
            }
        }

        private void HandleInput()
        {
            // Attack 액션
            if (Input.GetKeyDown(attackKey))
            {
                TryExecuteAction(MongdungActionType.Attack);
            }

            // TrapSetting 액션
            if (Input.GetKeyDown(trapKey))
            {
                TryExecuteAction(MongdungActionType.TrapSetting);
            }

            // Frighten 액션
            if (Input.GetKeyDown(frightenKey))
            {
                TryExecuteAction(MongdungActionType.Frighten);
            }
        }

        /// <summary>
        /// 액션 실행 시도
        /// </summary>
        public async void TryExecuteAction(MongdungActionType actionType)
        {
            if (!_viewModel.CanExecuteAction(actionType))
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[MongdungGameObject] 액션 실행 불가: {actionType}");
                }
                return;
            }

            Vector3 position = transform.position;
            Vector3 direction = transform.forward;

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungGameObject] 액션 실행 시도: {actionType} at {position}");
            }

            bool success = await _mongdungService.ExecuteActionAsync(PlayerId, actionType, position, direction);

            if (!success && enableDebugLogs)
            {
                Debug.LogWarning($"[MongdungGameObject] 액션 실행 실패: {actionType}");
            }
        }

        /// <summary>
        /// 액션 취소
        /// </summary>
        public async void CancelAction(MongdungActionType actionType)
        {
            if (_currentExecutingAction == actionType)
            {
                bool success = await _mongdungService.CancelActionAsync(PlayerId, actionType);

                if (enableDebugLogs)
                {
                    Debug.Log($"[MongdungGameObject] 액션 취소 {(success ? "성공" : "실패")}: {actionType}");
                }
            }
        }

        private void OnStateChanged(MongdungState newState)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungGameObject] 상태 변경: {newState}");
            }

            // 상태에 따른 추가 처리가 필요한 경우 여기에 구현
            switch (newState)
            {
                case MongdungState.Idle:
                    _currentExecutingAction = null;
                    break;
                case MongdungState.ExecutingAction:
                    // 실행 중 상태는 OnActionExecutingChanged에서 처리
                    break;
                case MongdungState.Cooldown:
                    // 쿨다운 상태 처리
                    break;
                case MongdungState.GameEnd:
                    // 게임 종료 상태 처리
                    break;
            }
        }

        private void OnMovementBlockedChanged(bool isBlocked)
        {
            // PlayerGameObject에 이동 제한 전달
            var playerGameObject = GetComponent<Features.Player.Views.PlayerGameObject>();
            if (playerGameObject != null)
            {
                playerGameObject.canMove = !isBlocked;

                if (enableDebugLogs)
                {
                    Debug.Log($"[MongdungGameObject] 이동 제한 변경: {isBlocked} -> PlayerGameObject.canMove = {!isBlocked}");
                }
            }
        }

        private void OnActionExecutingChanged(MongdungActionType actionType, bool isExecuting)
        {
            if (isExecuting)
            {
                _currentExecutingAction = actionType;
                TriggerActionAnimation(actionType);

                if (enableDebugLogs)
                {
                    Debug.Log($"[MongdungGameObject] 액션 실행 시작: {actionType}");
                }
            }
            else if (_currentExecutingAction == actionType)
            {
                _currentExecutingAction = null;
                StopActionAnimation(actionType);

                if (enableDebugLogs)
                {
                    Debug.Log($"[MongdungGameObject] 액션 실행 완료: {actionType}");
                }
            }
        }

        private void TriggerActionAnimation(MongdungActionType actionType)
        {
            if (_animator == null)
            {
                Debug.LogError($"[MongdungGameObject] Animator가 null입니다! 애니메이션 트리거 실패: {actionType}");
                return;
            }

            if (_animator.runtimeAnimatorController == null)
            {
                Debug.LogError($"[MongdungGameObject] AnimatorController가 null입니다! 애니메이션 트리거 실패: {actionType}");
                return;
            }

            switch (actionType)
            {
                case MongdungActionType.Attack:
                    _animator.SetTrigger(_attackHash);
                    if (enableDebugLogs)
                        Debug.Log($"[MongdungGameObject] Attack 트리거 설정 (Hash: {_attackHash})");
                    break;
                case MongdungActionType.TrapSetting:
                    _animator.SetTrigger(_trapSettingHash);
                    if (enableDebugLogs)
                        Debug.Log($"[MongdungGameObject] TrapSetting 트리거 설정 (Hash: {_trapSettingHash})");
                    break;
                case MongdungActionType.Frighten:
                    _animator.SetTrigger(_frightenHash);
                    if (enableDebugLogs)
                        Debug.Log($"[MongdungGameObject] Frighten 트리거 설정 (Hash: {_frightenHash})");
                    break;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungGameObject] 애니메이션 트리거 완료: {actionType}, Controller: {_animator.runtimeAnimatorController.name}");
            }
        }

        private void StopActionAnimation(MongdungActionType actionType)
        {
            // 필요한 경우 애니메이션 정리 로직 구현
            // 현재는 Trigger 방식이므로 별도 정리 불필요
        }

        private bool IsLocalPlayer()
        {
            // GameObject 이름으로 로컬 플레이어 판단
            return gameObject.name.Contains("Local");
        }

        /// <summary>
        /// 액션 실행 가능 여부 확인
        /// </summary>
        public bool CanExecuteAction(MongdungActionType actionType)
        {
            return _viewModel?.CanExecuteAction(actionType) ?? false;
        }

        /// <summary>
        /// 현재 실행 중인 액션 가져오기
        /// </summary>
        public MongdungActionType? GetCurrentExecutingAction()
        {
            return _currentExecutingAction;
        }

        /// <summary>
        /// 특정 액션의 남은 쿨다운 시간 가져오기
        /// </summary>
        public float GetRemainingCooldown(MongdungActionType actionType)
        {
            return _viewModel?.GetActionRemainingTime(actionType).CurrentValue ?? 0f;
        }

        private void OnDestroy()
        {
            // R3 구독 해제
            _disposables?.Dispose();

            // ViewModel 해제
            _viewModel?.Dispose();

            if (enableDebugLogs)
            {
                Debug.Log($"[MongdungGameObject] 리소스 정리 완료: {gameObject.name}");
            }
        }

        /// <summary>
        /// 디버그용 현재 상태 출력
        /// </summary>
        [ContextMenu("Log Current Status")]
        public void LogCurrentStatus()
        {
            if (_viewModel == null)
            {
                Debug.Log("[MongdungGameObject] ViewModel이 null입니다.");
                return;
            }

            Debug.Log(
                $"[MongdungGameObject] 상태 리포트:\n" +
                $"  PlayerId: {PlayerId}\n" +
                $"  PlayerName: {PlayerName}\n" +
                $"  CurrentState: {_viewModel.CurrentState.CurrentValue}\n" +
                $"  IsMovementBlocked: {_viewModel.IsMovementBlocked.CurrentValue}\n" +
                $"  CurrentExecutingAction: {_currentExecutingAction}\n" +
                $"  Attack Cooldown: {GetRemainingCooldown(MongdungActionType.Attack):F1}s\n" +
                $"  TrapSetting Cooldown: {GetRemainingCooldown(MongdungActionType.TrapSetting):F1}s\n" +
                $"  Frighten Cooldown: {GetRemainingCooldown(MongdungActionType.Frighten):F1}s"
            );
        }

        private void OnDrawGizmosSelected()
        {
            // 각 액션의 범위 표시
            if (mongdungData?.actionDataArray != null)
            {
                Gizmos.color = Color.red;
                foreach (var actionData in mongdungData.actionDataArray)
                {
                    if (actionData.actionType == MongdungActionType.Attack)
                        Gizmos.color = Color.red;
                    else if (actionData.actionType == MongdungActionType.TrapSetting)
                        Gizmos.color = Color.yellow;
                    else if (actionData.actionType == MongdungActionType.Frighten)
                        Gizmos.color = Color.blue;

                    Gizmos.DrawWireSphere(transform.position, actionData.range);
                }
            }
        }
    }
}