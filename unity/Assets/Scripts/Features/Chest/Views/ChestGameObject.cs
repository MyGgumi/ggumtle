using Features.Chest.Models;
using Features.Chest.Services;
using Features.Chest.ViewModels;
using R3;
using UnityEngine;
using VContainer;

namespace Features.Chest.Views
{
    /// <summary>
    /// 상자 순수 View 컴포넌트 - 애니메이션과 시각적 이펙트만 처리
    /// 모든 비즈니스 로직은 ChestViewModel이 담당하며, 이 컴포넌트는 ViewModel 상태에 따른 시각적 표현만 담당
    /// InteractionTriggerDetector가 플레이어 감지를 담당하고, 이 컴포넌트는 상호작용 로직을 포함하지 않음
    /// </summary>
    public class ChestGameObject : MonoBehaviour
    {
        #region Inspector 설정

        [Header("상자 기본 설정")]
        [SerializeField]
        private string chestName = "상자";

        [SerializeField]
        private string chestId; // 고유 ID (Inspector에서 설정 또는 자동 생성)

        // InteractionTriggerDetector에서 접근하기 위한 public property
        public string ChestId => chestId;
        public string ChestName => chestName;

        [Header("비주얼 컴포넌트")]
        [SerializeField]
        private Animator chestAnimator;

        [SerializeField]
        private ParticleSystem openEffect;

        [SerializeField]
        private ParticleSystem closeEffect;

        [SerializeField]
        private GameObject interactionIcon;

        [Header("애니메이션 트리거")]
        [SerializeField]
        private string openAnimationTrigger = "Open";

        [SerializeField]
        private string closeAnimationTrigger = "Close";

        [Header("디버그 설정")]
        [SerializeField]
        private bool enableDebugLogs = false;

        #endregion

        #region 의존성 주입 & 데이터

        private ChestViewModel _viewModel;
        private IChestService _chestService;
        private bool _isInitialized = false; // 초기화 중복 방지
        private CompositeDisposable _disposables = new();

        /// <summary>
        /// VContainer 의존성 주입
        /// </summary>
        [Inject]
        public void Construct(ChestViewModel viewModel, IChestService chestService)
        {
            _viewModel = viewModel;
            _chestService = chestService;

            if (enableDebugLogs)
            {
                Debug.Log($"[ChestGameObject] VContainer 의존성 주입 완료: {gameObject.name}");
            }
        }

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            Debug.Log(
                $"[ChestGameObject] Start() 호출됨 - GameObject: {gameObject.name}, Layer: {gameObject.layer}"
            );

            // VContainer 의존성 주입이 아직 안 된 경우 Coroutine으로 대기
            if (_viewModel == null || _chestService == null)
            {
                Debug.Log(
                    $"[ChestGameObject] 의존성 주입 대기 중: {gameObject.name}. ViewModel: {_viewModel != null}, Service: {_chestService != null}"
                );
                StartCoroutine(WaitForDependencyInjection());
                return;
            }

            InitializeChestGameObject();
        }

        private System.Collections.IEnumerator WaitForDependencyInjection()
        {
            float timeout = 5f; // 5초 타임아웃
            float elapsed = 0f;

            while ((_viewModel == null || _chestService == null) && elapsed < timeout)
            {
                yield return new UnityEngine.WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            if (_viewModel != null && _chestService != null)
            {
                Debug.Log($"[ChestGameObject] 의존성 주입 완료 대기 성공: {gameObject.name}");
                InitializeChestGameObject();
            }
            else
            {
                Debug.LogError(
                    $"[ChestGameObject] 의존성 주입 타임아웃: {gameObject.name}. ViewModel: {_viewModel != null}, Service: {_chestService != null}"
                );
            }
        }

        private void InitializeChestGameObject()
        {
            if (_isInitialized)
            {
                Debug.Log($"[ChestGameObject] 이미 초기화됨, 건너뛰기: {gameObject.name}");
                return;
            }

            Debug.Log($"[ChestGameObject] 초기화 시작: {gameObject.name}");

            InitializeBasicComponents();
            SetupInteractionLayer();
            RegisterToService();
            SubscribeToViewModel();

            _isInitialized = true;
            Debug.Log($"[ChestGameObject] 초기화 완료: {gameObject.name}");

            // 최종 상태 확인
            var collider = GetComponent<Collider>();
            Debug.Log(
                $"[ChestGameObject] 초기화 완료: {gameObject.name}, ID: {chestId}, Layer: {gameObject.layer}, Collider: {collider != null}, IsTrigger: {collider?.isTrigger}"
            );
        }

        void OnDestroy()
        {
            // ChestService에서 해제
            UnregisterFromService();

            // R3 구독 해제
            _disposables.Dispose();

            if (enableDebugLogs)
                Debug.Log($"[ChestGameObject] OnDestroy: {gameObject.name}");
        }

        #endregion

        #region 초기화

        private void InitializeBasicComponents()
        {
            // ID 자동 생성 (Inspector에서 설정하지 않은 경우)
            if (string.IsNullOrEmpty(chestId))
            {
                // GameObject 이름에서 ID 추출 (예: "Chest_123" -> "123")
                if (gameObject.name.StartsWith("Chest_"))
                {
                    chestId = gameObject.name.Substring(6); // "Chest_" 다음 부분
                    Debug.Log($"[ChestGameObject] GameObject 이름에서 추출된 chestId: {chestId}");
                }
                else
                {
                    // 백업: 위치 기반 ID 생성
                    chestId =
                        $"Chest_{transform.position.x}_{transform.position.z}_{GetInstanceID()}";
                    Debug.Log($"[ChestGameObject] 자동 생성된 chestId: {chestId}");
                }
            }

            // 컴포넌트 자동 찾기
            if (chestAnimator == null)
                chestAnimator = GetComponent<Animator>();

            if (enableDebugLogs)
                Debug.Log(
                    $"[ChestGameObject] 컴포넌트 초기화 완료: {gameObject.name}, ID: {chestId}"
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
                        $"[ChestGameObject] 레이어를 Interaction(7)으로 설정: {gameObject.name}"
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

                Debug.Log($"[ChestGameObject] SphereCollider 자동 생성: {gameObject.name}");
            }
            else
            {
                if (!collider.isTrigger)
                {
                    collider.isTrigger = true;
                    Debug.Log(
                        $"[ChestGameObject] Collider를 Trigger로 자동 설정: {gameObject.name}"
                    );
                }
            }
        }

        #endregion

        #region Service Registration

        /// <summary>
        /// ChestService에 자신을 등록
        /// </summary>
        private void RegisterToService()
        {
            if (_chestService == null)
            {
                Debug.LogError(
                    $"[ChestGameObject] ChestService가 주입되지 않아 등록 실패: {gameObject.name}"
                );
                return;
            }

            try
            {
                _chestService.RegisterChest(chestId, chestName, transform.position, gameObject);
                Debug.Log(
                    $"[ChestGameObject] ChestService 등록 완료: {gameObject.name}, ID: {chestId}"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[ChestGameObject] ChestService 등록 실패: {gameObject.name}, 오류: {ex.Message}"
                );
            }
        }

        /// <summary>
        /// ChestService에서 자신을 해제
        /// </summary>
        private void UnregisterFromService()
        {
            if (_chestService == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning(
                        $"[ChestGameObject] ChestService가 null이어서 해제 생략: {gameObject.name}"
                    );
                return;
            }

            try
            {
                _chestService.UnregisterChest(chestId);
                Debug.Log(
                    $"[ChestGameObject] ChestService 해제 완료: {gameObject.name}, ID: {chestId}"
                );
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[ChestGameObject] ChestService 해제 실패: {gameObject.name}, 오류: {ex.Message}"
                );
            }
        }

        #endregion

        #region R3 ViewModel 구독 (뷰 로직)

        private void SubscribeToViewModel()
        {
            if (_viewModel == null)
            {
                Debug.LogError(
                    $"[ChestGameObject] ChestViewModel을 해결할 수 없어 구독을 건너뜀: {gameObject.name}"
                );
                return;
            }

            // 상태 변경에 따른 비주얼 처리
            _viewModel.State.Subscribe(OnViewModelStateChanged).AddTo(_disposables);

            // UI 가시성에 따른 처리
            _viewModel.IsUIVisible.Subscribe(OnUIVisibilityChanged).AddTo(_disposables);

            // 범위 감지에 따른 처리
            _viewModel.IsInRange.Subscribe(OnRangeChanged).AddTo(_disposables);

            // 현재 상자 ID 변경에 따른 처리
            _viewModel.CurrentChestId.Subscribe(OnCurrentChestChanged).AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[ChestGameObject] ViewModel R3 구독 완료");
        }

        private void OnViewModelStateChanged(ChestState newState)
        {
            if (enableDebugLogs)
                Debug.Log($"[ChestGameObject] ViewModel 상태 변경: {newState}");

            HandleStateVisuals(newState);
        }

        private void OnUIVisibilityChanged(bool isVisible)
        {
            if (enableDebugLogs)
                Debug.Log($"[ChestGameObject] UI 가시성 변경: {isVisible}");

            // UI 가시성에 따른 추가 처리가 필요하면 여기에 구현
        }

        private void OnRangeChanged(bool inRange)
        {
            if (enableDebugLogs)
                Debug.Log($"[ChestGameObject] 범위 변경: {inRange}");

            // 상호작용 아이콘 표시/숨김
            if (interactionIcon != null)
            {
                // 현재 이 상자가 감지된 상자이고, 범위 내에 있으며, 닫혀있을 때만 아이콘 표시
                bool shouldShowIcon =
                    inRange
                    && _viewModel.CurrentChestId.Value == chestId
                    && _viewModel.State.Value == ChestState.Closed;

                interactionIcon.SetActive(shouldShowIcon);
            }
        }

        private void OnCurrentChestChanged(string newChestId)
        {
            bool isCurrentChest = newChestId == chestId;

            if (enableDebugLogs)
                Debug.Log(
                    $"[ChestGameObject] 현재 상자 변경: {newChestId}, 내가 현재 상자인가: {isCurrentChest}"
                );

            // 현재 상자가 아니라면 상호작용 아이콘 숨김
            if (!isCurrentChest && interactionIcon != null)
            {
                interactionIcon.SetActive(false);
            }
        }

        #endregion

        #region 비주얼 처리 (애니메이션 & 이펙트)

        private void HandleStateVisuals(ChestState state)
        {
            // 현재 상자가 아니라면 애니메이션 재생하지 않음
            if (_viewModel.CurrentChestId.Value != chestId)
                return;

            switch (state)
            {
                case ChestState.Closed:
                    OnStateClosed();
                    break;
                case ChestState.Open:
                    OnStateOpen();
                    break;
                case ChestState.Animating:
                    OnStateAnimating();
                    break;
                case ChestState.Locked:
                    OnStateLocked();
                    break;
            }
        }

        private void OnStateClosed()
        {
            // 닫힌 상태 - 닫기 애니메이션 재생
            if (chestAnimator != null && !string.IsNullOrEmpty(closeAnimationTrigger))
            {
                chestAnimator.SetTrigger(closeAnimationTrigger);
                chestAnimator.SetBool("IsOpen", false);
            }

            if (closeEffect != null)
            {
                closeEffect.Play();
            }

            // 상호작용 아이콘 표시 (범위 내에 있다면)
            if (interactionIcon != null && _viewModel.IsInRange.Value)
            {
                interactionIcon.SetActive(true);
            }

            if (enableDebugLogs)
                Debug.Log($"[ChestGameObject] 상자 닫힘 애니메이션: {gameObject.name}");
        }

        private void OnStateOpen()
        {
            // 열린 상태 - 열기 애니메이션 재생
            if (chestAnimator != null && !string.IsNullOrEmpty(openAnimationTrigger))
            {
                chestAnimator.SetTrigger(openAnimationTrigger);
                chestAnimator.SetBool("IsOpen", true);
            }

            if (openEffect != null)
            {
                openEffect.Play();
            }

            // 상호작용 아이콘 숨김
            if (interactionIcon != null)
            {
                interactionIcon.SetActive(false);
            }

            if (enableDebugLogs)
                Debug.Log($"[ChestGameObject] 상자 열림 애니메이션: {gameObject.name}");
        }

        private void OnStateAnimating()
        {
            // 애니메이션 중 - 상호작용 아이콘 숨김
            if (interactionIcon != null)
            {
                interactionIcon.SetActive(false);
            }

            if (enableDebugLogs)
                Debug.Log($"[ChestGameObject] 상자 애니메이션 중: {gameObject.name}");
        }

        private void OnStateLocked()
        {
            // 잠긴 상태 - 특별한 이펙트나 애니메이션
            if (enableDebugLogs)
                Debug.Log($"[ChestGameObject] 상자 잠김 상태: {gameObject.name}");
        }

        #endregion

        #region 디버그 및 유틸리티

        [ContextMenu("Log Current State")]
        public void LogCurrentState()
        {
            if (_viewModel == null)
            {
                Debug.Log("[ChestGameObject] ViewModel is null");
                return;
            }

            Debug.Log(
                $"[ChestGameObject] State Report:\n"
                    + $"  GameObject: {gameObject.name}\n"
                    + $"  Chest ID: {chestId}\n"
                    + $"  Chest Name: {chestName}\n"
                    + $"  ViewModel State: {_viewModel.State.Value}\n"
                    + $"  In Range: {_viewModel.IsInRange.Value}\n"
                    + $"  Is Current Chest: {_viewModel.CurrentChestId.Value == chestId}\n"
                    + $"  UI Visible: {_viewModel.IsUIVisible.Value}"
            );
        }

        #endregion
    }
}
