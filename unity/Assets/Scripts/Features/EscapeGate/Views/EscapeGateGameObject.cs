using Features.EscapeGate.Models;
using Features.EscapeGate.Services;
using Features.Player.Services;
using Features.PlayerList.Models;
using R3;
using UnityEngine;
using VContainer;

namespace Features.EscapeGate.Views
{
    /// <summary>
    /// 탈출 게이트 순수 View 컴포넌트 - 애니메이션과 시각적 이펙트만 처리
    /// 모든 비즈니스 로직은 EscapeGateService가 담당하며, 이 컴포넌트는 상태에 따른 시각적 표현만 담당
    /// InteractionTriggerDetector가 플레이어 감지를 담당하고, 이 컴포넌트는 상호작용 로직을 포함하지 않음
    /// </summary>
    public class EscapeGateGameObject : MonoBehaviour
    {
        #region Inspector 설정

        [Header("탈출 게이트 기본 설정")]
        [SerializeField]
        private string gateName = "탈출구";

        [SerializeField]
        private int gateId = -1; // 고유 ID (Inspector에서 설정)

        // InteractionTriggerDetector에서 접근하기 위한 public property
        public int GateId => gateId;
        public string GateName => gateName;

        /// <summary>
        /// 게이트 ID 설정 (동적 생성 시 사용)
        /// </summary>
        public void SetGateId(int id)
        {
            gateId = id;
            if (enableDebugLogs)
                Debug.Log($"[EscapeGateGameObject] GateId 설정 완료: {id}");
        }

        [Header("비주얼 컴포넌트")]
        [SerializeField]
        private Animator gateAnimator;

        [SerializeField]
        private ParticleSystem activationEffect;

        [SerializeField]
        private ParticleSystem escapeEffect;

        [SerializeField]
        private GameObject interactionIcon;

        [SerializeField]
        private GameObject gateActiveVisual; // 활성화 시 보여줄 오브젝트

        [Header("애니메이션 트리거")]
        [SerializeField]
        private string activateAnimationTrigger = "Activate";

        [SerializeField]
        private string deactivateAnimationTrigger = "Deactivate";

        [Header("디버그 설정")]
        [SerializeField]
        private bool enableDebugLogs = false;

        #endregion

        #region 의존성 주입 & 데이터

        private IEscapeGateService _escapeGateService;
        private PlayerManagerService _playerManagerService;
        private bool _isInitialized = false;
        private CompositeDisposable _disposables = new();

        /// <summary>
        /// VContainer 의존성 주입
        /// </summary>
        [Inject]
        public void Construct(IEscapeGateService escapeGateService, PlayerManagerService playerManagerService)
        {
            _escapeGateService = escapeGateService;
            _playerManagerService = playerManagerService;

            if (enableDebugLogs)
            {
                Debug.Log($"[EscapeGateGameObject] VContainer 의존성 주입 완료: {gameObject.name}");
            }
        }

        #endregion


        #region Unity Lifecycle

        void Start()
        {
            if (enableDebugLogs) Debug.Log(
                $"[EscapeGateGameObject] Start() 호출됨 - GameObject: {gameObject.name}, Layer: {gameObject.layer}"
            );

            InitializeEscapeGateGameObject();
        }


        private void InitializeEscapeGateGameObject()
        {
            if (_isInitialized)
            {
                if (enableDebugLogs) Debug.Log($"[EscapeGateGameObject] 이미 초기화됨, 건너뛰기: {gameObject.name}");
                return;
            }

            if (enableDebugLogs) Debug.Log($"[EscapeGateGameObject] 초기화 시작: {gameObject.name}");

            InitializeBasicComponents();
            SetupInteractionLayer();
            RegisterToService();
            SubscribeToService();

            _isInitialized = true;
            if (enableDebugLogs) Debug.Log($"[EscapeGateGameObject] 초기화 완료: {gameObject.name}");

            // 최종 상태 확인
            var collider = GetComponent<Collider>();
            if (enableDebugLogs) Debug.Log(
                $"[EscapeGateGameObject] 초기화 완료: {gameObject.name}, ID: {gateId}, Layer: {gameObject.layer}, Collider: {collider != null}, IsTrigger: {collider?.isTrigger}"
            );
        }

        void OnDestroy()
        {
            // EscapeGateService에서 해제
            UnregisterFromService();

            // R3 구독 해제
            _disposables.Dispose();

            if (enableDebugLogs)
                Debug.Log($"[EscapeGateGameObject] OnDestroy: {gameObject.name}");
        }

        #endregion

        #region 초기화

        private void InitializeBasicComponents()
        {
            // ID 검증
            if (gateId < 0)
            {
                Debug.LogError($"[EscapeGateGameObject] gateId가 설정되지 않음: {gameObject.name}");
                return;
            }

            // 컴포넌트 자동 찾기
            if (gateAnimator == null)
                gateAnimator = GetComponent<Animator>();

            // 초기 상태 설정 (비활성화)
            if (gateActiveVisual != null)
                gateActiveVisual.SetActive(false);

            if (interactionIcon != null)
                interactionIcon.SetActive(false);

            if (enableDebugLogs)
                Debug.Log(
                    $"[EscapeGateGameObject] 컴포넌트 초기화 완료: {gameObject.name}, ID: {gateId}"
                );
        }

        private void SetupInteractionLayer()
        {
            // 상호작용 레이어 설정 (Layer 7 = Interaction)
            // InteractionTriggerDetector가 이 레이어만 감지함
            if (gameObject.layer != 7)
            {
                gameObject.layer = 7;
                if (enableDebugLogs)
                    Debug.Log(
                        $"[EscapeGateGameObject] 레이어를 Interaction(7)으로 설정: {gameObject.name}"
                    );
            }

            // Collider가 있는지 확인 (Trigger 감지에 필요)
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                // SphereCollider 자동 생성
                var sphereCollider = gameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 2f; // 기본 상호작용 범위

                if (enableDebugLogs) Debug.Log($"[EscapeGateGameObject] SphereCollider 자동 생성: {gameObject.name}");
            }
            else
            {
                if (!collider.isTrigger)
                {
                    collider.isTrigger = true;
                    if (enableDebugLogs) Debug.Log(
                        $"[EscapeGateGameObject] Collider를 Trigger로 자동 설정: {gameObject.name}"
                    );
                }
            }
        }

        #endregion

        #region Service Registration

        /// <summary>
        /// EscapeGateService에 자신을 등록
        /// </summary>
        private void RegisterToService()
        {
            if (_escapeGateService == null)
            {
                Debug.LogError(
                    $"[EscapeGateGameObject] EscapeGateService가 주입되지 않아 등록 실패: {gameObject.name}"
                );
                return;
            }

            if (gateId < 0)
            {
                Debug.LogError(
                    $"[EscapeGateGameObject] gateId가 설정되지 않아 등록 실패: {gameObject.name}"
                );
                return;
            }

            try
            {
                _escapeGateService.RegisterGate(gateId, gateName, transform.position, gameObject);
                if (enableDebugLogs) Debug.Log(
                    $"[EscapeGateGameObject] EscapeGateService 등록 완료: {gameObject.name}, ID: {gateId}"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[EscapeGateGameObject] EscapeGateService 등록 실패: {gameObject.name}, 오류: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// EscapeGateService에서 자신을 해제
        /// </summary>
        private void UnregisterFromService()
        {
            if (_escapeGateService == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning(
                        $"[EscapeGateGameObject] EscapeGateService가 null이어서 해제 생략: {gameObject.name}"
                    );
                return;
            }

            try
            {
                _escapeGateService.UnregisterGate(gateId);
                if (enableDebugLogs) Debug.Log(
                    $"[EscapeGateGameObject] EscapeGateService 해제 완료: {gameObject.name}, ID: {gateId}"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[EscapeGateGameObject] EscapeGateService 해제 실패: {gameObject.name}, 오류: {ex.Message}"
                );
            }
        }

        #endregion

        #region R3 Service 구독 (뷰 로직)

        private void SubscribeToService()
        {
            if (_escapeGateService == null)
            {
                Debug.LogError(
                    $"[EscapeGateGameObject] EscapeGateService를 해결할 수 없어 구독을 건너뜀: {gameObject.name}"
                );
                return;
            }

            // 현재 게이트 상태 변경에 따른 비주얼 처리
            _escapeGateService.CurrentGateState.Subscribe(OnCurrentGateStateChanged).AddTo(_disposables);

            // 현재 게이트 ID 변경에 따른 처리
            _escapeGateService.CurrentGateId.Subscribe(OnCurrentGateChanged).AddTo(_disposables);

            // 범위 감지에 따른 처리
            _escapeGateService.IsInRange.Subscribe(OnRangeChanged).AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[EscapeGateGameObject] EscapeGateService R3 구독 완료");
        }

        private void OnCurrentGateStateChanged(EscapeGateState newState)
        {
            // 현재 이 게이트가 감지된 게이트인 경우에만 상태 변경 처리
            if (_escapeGateService.CurrentGateId.CurrentValue != gateId)
                return;

            if (enableDebugLogs)
                Debug.Log($"[EscapeGateGameObject] 상태 변경: {newState} (Gate: {gateId})");

            HandleStateVisuals(newState);
        }

        private void OnCurrentGateChanged(int newGateId)
        {
            bool isCurrentGate = newGateId == gateId;

            if (enableDebugLogs)
                Debug.Log(
                    $"[EscapeGateGameObject] 현재 게이트 변경: {newGateId}, 내가 현재 게이트인가: {isCurrentGate}"
                );

            // 현재 게이트가 아니라면 상호작용 아이콘 숨김
            if (!isCurrentGate && interactionIcon != null)
            {
                interactionIcon.SetActive(false);
            }
        }

        private void OnRangeChanged(bool inRange)
        {
            // 현재 이 게이트가 감지된 게이트이고, 범위 내에 있으며, 몽깅이 플레이어일 때만 아이콘 표시
            bool shouldShowIcon = inRange
                && _escapeGateService.CurrentGateId.CurrentValue == gateId
                && _escapeGateService.CurrentGateState.CurrentValue == EscapeGateState.Active
                && IsLocalMonggingPlayer();

            if (interactionIcon != null)
            {
                interactionIcon.SetActive(shouldShowIcon);
            }

            if (enableDebugLogs)
                Debug.Log($"[EscapeGateGameObject] 범위 변경: {inRange}, 아이콘 표시: {shouldShowIcon}");
        }

        #endregion

        #region 비주얼 처리 (애니메이션 & 이펙트)

        private void HandleStateVisuals(EscapeGateState state)
        {
            switch (state)
            {
                case EscapeGateState.Inactive:
                    OnStateInactive();
                    break;
                case EscapeGateState.Active:
                    OnStateActive();
                    break;
                case EscapeGateState.InUse:
                    OnStateInUse();
                    break;
                case EscapeGateState.Disabled:
                    OnStateDisabled();
                    break;
            }
        }

        private void OnStateInactive()
        {
            // 비활성화 상태
            if (gateAnimator != null && !string.IsNullOrEmpty(deactivateAnimationTrigger))
            {
                gateAnimator.SetTrigger(deactivateAnimationTrigger);
                gateAnimator.SetBool("IsActive", false);
            }

            if (gateActiveVisual != null)
            {
                gateActiveVisual.SetActive(false);
            }

            if (interactionIcon != null)
            {
                interactionIcon.SetActive(false);
            }

            if (enableDebugLogs)
                Debug.Log($"[EscapeGateGameObject] 탈출구 비활성화: {gameObject.name}");
        }

        private void OnStateActive()
        {
            // 활성화 상태 - 탈출 가능
            if (gateAnimator != null && !string.IsNullOrEmpty(activateAnimationTrigger))
            {
                gateAnimator.SetTrigger(activateAnimationTrigger);
                gateAnimator.SetBool("IsActive", true);
            }

            if (activationEffect != null)
            {
                activationEffect.Play();
            }

            if (gateActiveVisual != null)
            {
                gateActiveVisual.SetActive(true);
            }

            // 상호작용 아이콘은 범위 내에 있고 몽깅이 플레이어일 때만 표시
            if (interactionIcon != null && _escapeGateService.IsInRange.CurrentValue && IsLocalMonggingPlayer())
            {
                interactionIcon.SetActive(true);
            }

            if (enableDebugLogs)
                Debug.Log($"[EscapeGateGameObject] 탈출구 활성화: {gameObject.name}");
        }

        private void OnStateInUse()
        {
            // 사용 중 상태 - 상호작용 아이콘 숨김
            if (interactionIcon != null)
            {
                interactionIcon.SetActive(false);
            }

            if (escapeEffect != null)
            {
                escapeEffect.Play();
            }

            if (enableDebugLogs)
                Debug.Log($"[EscapeGateGameObject] 탈출구 사용 중: {gameObject.name}");
        }

        private void OnStateDisabled()
        {
            // 비활성화됨 상태
            if (gateActiveVisual != null)
            {
                gateActiveVisual.SetActive(false);
            }

            if (interactionIcon != null)
            {
                interactionIcon.SetActive(false);
            }

            if (enableDebugLogs)
                Debug.Log($"[EscapeGateGameObject] 탈출구 사용 불가: {gameObject.name}");
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// 로컬 몽깅이 플레이어인지 확인
        /// </summary>
        private bool IsLocalMonggingPlayer()
        {
            if (_playerManagerService == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[EscapeGateGameObject] PlayerManagerService가 주입되지 않았습니다");
                return false; // 몽깅이가 아니면 탈출 불가
            }

            var localPlayerRole = _playerManagerService.GetLocalPlayerRole();
            return localPlayerRole == PlayerRole.Mongging;
        }

        /// <summary>
        /// 외부에서 탈출 시도 호출 (상호작용 시)
        /// </summary>
        public void AttemptEscape()
        {
            if (!IsLocalMonggingPlayer())
            {
                if (enableDebugLogs)
                    Debug.Log("[EscapeGateGameObject] 몽둥이 플레이어는 탈출할 수 없습니다");
                return;
            }

            if (_escapeGateService.CurrentGateState.CurrentValue != EscapeGateState.Active)
            {
                if (enableDebugLogs)
                    Debug.Log("[EscapeGateGameObject] 탈출구가 활성화되지 않았습니다");
                return;
            }

            _escapeGateService.AttemptEscape(gateId);
        }

        #endregion

        #region 디버그 및 유틸리티

        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            if (_escapeGateService == null)
            {
                Debug.Log("[EscapeGateGameObject] EscapeGateService is null");
                return;
            }

            var gateData = _escapeGateService.GetGate(gateId);
            Debug.Log(
                $"[EscapeGateGameObject] State Report:\n"
                    + $"  GameObject: {gameObject.name}\n"
                    + $"  Gate ID: {gateId}\n"
                    + $"  Gate Name: {gateName}\n"
                    + $"  Gate State: {gateData?.State}\n"
                    + $"  In Range: {_escapeGateService.IsInRange.CurrentValue}\n"
                    + $"  Is Current Gate: {_escapeGateService.CurrentGateId.CurrentValue == gateId}\n"
                    + $"  Is Local Mongging: {IsLocalMonggingPlayer()}"
            );
        }

        #endregion
    }
}