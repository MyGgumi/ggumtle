using Features.Ggumtle.Messages;
using Features.Ggumtle.Models;
using Features.Ggumtle.ViewModels;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using DI;
using R3DisposableBag = R3.DisposableBag;

namespace Features.Ggumtle.Views
{
    /// <summary>
    /// 꿈틀이 순수 View 컴포넌트 - 애니메이션과 시각적 이펙트만 처리
    /// 모든 비즈니스 로직은 GgumtleViewModel이 담당하며, 이 컴포넌트는 ViewModel 상태에 따른 시각적 표현만 담당
    /// InteractionTriggerDetector가 플레이어 감지를 담당하고, 이 컴포넌트는 상호작용 로직을 포함하지 않음
    /// </summary>
    public class GgumtleGameObject : MonoBehaviour
    {
        #region Inspector 설정

        [Header("꿈틀이 기본 설정")]
        [SerializeField]
        private string ggumtleName = "꿈틀이";

        [SerializeField]
        private int ggumtleId; // 고유 ID (Inspector에서 설정 또는 자동 생성)

        // InteractionTriggerDetector에서 접근하기 위한 public property
        public int GgumtleId => ggumtleId;

        [Header("비주얼 컴포넌트")]
        [SerializeField]
        private Animator ggumtleAnimator;

        // 하위 애니메이터 (실제 애니메이션 제어용)
        private Animator childAnimator;

        [SerializeField]
        private ParticleSystem diggingEffect;

        [SerializeField]
        private ParticleSystem purificationEffect;

        [SerializeField]
        private ParticleSystem feedingEffect;

        [Header("애니메이션 트리거")]
        [SerializeField]
        private string diggingAnimationTrigger = "Dig";

        [SerializeField]
        private string emergingAnimationTrigger = "Emerge";

        [SerializeField]
        private string feedingAnimationTrigger = "Feed";

        [SerializeField]
        private string purifiedAnimationTrigger = "Purify";

        [Header("디버그 설정")]
        [SerializeField]
        private bool enableDebugLogs = false;

        #endregion

        #region 의존성 주입 & 데이터

        private GgumtleViewModel _viewModel;
        private Features.Ggumtle.Services.IGgumtleService _ggumtleService;
        private ISubscriber<GgumtleStateBroadcastMessage> _stateBroadcastSubscriber;
        private CompositeDisposable _disposables = new();

        /// <summary>
        /// VContainer 의존성 주입
        /// </summary>
        [Inject]
        public void Construct(
            GgumtleViewModel viewModel,
            Features.Ggumtle.Services.IGgumtleService ggumtleService,
            ISubscriber<GgumtleStateBroadcastMessage> stateBroadcastSubscriber)
        {
            _viewModel = viewModel;
            _ggumtleService = ggumtleService;
            _stateBroadcastSubscriber = stateBroadcastSubscriber;

            if (enableDebugLogs)
            {
                Debug.Log($"[GgumtleGameObject] VContainer 의존성 주입 완료: {gameObject.name}");
            }
        }

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            Debug.Log(
                $"[GgumtleGameObject] Start() 호출됨 - GameObject: {gameObject.name}, Layer: {gameObject.layer}"
            );

            // VContainer 의존성 주입 확인
            if (_viewModel == null || _ggumtleService == null || _stateBroadcastSubscriber == null)
            {
                Debug.LogError($"[GgumtleGameObject] VContainer 의존성 주입 실패: {gameObject.name}. ViewModel: {_viewModel != null}, Service: {_ggumtleService != null}, Subscriber: {_stateBroadcastSubscriber != null}");
                return;
            }

            InitializeBasicComponents();
            SetupInteractionLayer(); // Layer 설정 추가
            RegisterToService(); // GgumtleService에 등록
            SubscribeToViewModel();
            SubscribeToNetworkEvents(); // 네트워크 이벤트 구독

            // 최종 상태 확인
            var collider = GetComponent<Collider>();
            Debug.Log(
                $"[GgumtleGameObject] 초기화 완료: {gameObject.name}, ID: {ggumtleId}, Layer: {gameObject.layer}, Collider: {collider != null}, IsTrigger: {collider?.isTrigger}"
            );
        }

        void OnDestroy()
        {
            // GgumtleService에서 해제
            UnregisterFromService();

            // R3 구독 해제
            _disposables.Dispose();

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] OnDestroy: {gameObject.name}");
        }

        #endregion

        #region 초기화

        private void InitializeBasicComponents()
        {
            // ID는 외부에서 설정되므로 자동 생성하지 않음
            // MapSpawnServiceImpl에서 리플렉션으로 설정함

            // 컴포넌트 자동 찾기
            if (ggumtleAnimator == null)
                ggumtleAnimator = GetComponent<Animator>();

            // 하위 애니메이터 찾기 (실제 애니메이션 제어용)
            childAnimator = GetComponentInChildren<Animator>();
            if (childAnimator == null)
            {
                Debug.LogWarning($"[GgumtleGameObject] 하위 Animator를 찾을 수 없습니다: {gameObject.name}");
            }

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 컴포넌트 초기화 완료: {gameObject.name}, ID: {ggumtleId}, ChildAnimator: {childAnimator != null}");
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
                        $"[GgumtleGameObject] 레이어를 Interaction(7)으로 설정: {gameObject.name}"
                    );
            }

            // Collider가 있는지 확인 (Trigger 감지에 필요)
            var collider = GetComponent<Collider>();
            if (collider == null)
            {
                Debug.LogWarning(
                    $"[GgumtleGameObject] Collider가 없습니다! Trigger 감지가 작동하지 않습니다: {gameObject.name}"
                );
            }
            else
            {
                if (!collider.isTrigger)
                {
                    collider.isTrigger = true;
                    Debug.Log(
                        $"[GgumtleGameObject] Collider를 Trigger로 자동 설정: {gameObject.name}"
                    );
                }

                // Collider 타입별 정보 출력
                if (collider is CapsuleCollider capsule)
                {
                    if (enableDebugLogs)
                        Debug.Log(
                            $"[GgumtleGameObject] CapsuleCollider 감지 - Height: {capsule.height}, Radius: {capsule.radius}"
                        );
                }
                else if (collider is SphereCollider sphere)
                {
                    if (enableDebugLogs)
                        Debug.Log(
                            $"[GgumtleGameObject] SphereCollider 감지 - Radius: {sphere.radius}"
                        );
                }
                else if (collider is BoxCollider box)
                {
                    if (enableDebugLogs)
                        Debug.Log($"[GgumtleGameObject] BoxCollider 감지 - Size: {box.size}");
                }
            }
        }


        #endregion

        #region Service Registration

        /// <summary>
        /// GgumtleService에 자신을 등록
        /// </summary>
        private void RegisterToService()
        {
            if (_ggumtleService == null)
            {
                Debug.LogError($"[GgumtleGameObject] GgumtleService가 주입되지 않아 등록 실패: {gameObject.name}");
                return;
            }

            try
            {
                // GgumtleService에 등록 (ID, 이름, 위치)
                _ggumtleService.RegisterGgumtle(ggumtleId.ToString(), gameObject.name, transform.position);
                Debug.Log($"[GgumtleGameObject] GgumtleService 등록 완료: {gameObject.name}, ID: {ggumtleId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GgumtleGameObject] GgumtleService 등록 실패: {gameObject.name}, 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// GgumtleService에서 자신을 해제
        /// </summary>
        private void UnregisterFromService()
        {
            if (_ggumtleService == null)
            {
                Debug.LogError($"[GgumtleGameObject] GgumtleService가 주입되지 않아 해제 실패: {gameObject.name}");
                return;
            }

            try
            {
                // 주입된 GgumtleService 사용
                _ggumtleService.UnregisterGgumtle(ggumtleId.ToString());
                Debug.Log($"[GgumtleGameObject] GgumtleService 해제 완료: {gameObject.name}, ID: {ggumtleId}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GgumtleGameObject] GgumtleService 해제 실패: {gameObject.name}, 오류: {ex.Message}");
            }
        }

        #endregion

        #region R3 ViewModel 구독 (뷰 로직)

        private void SubscribeToViewModel()
        {
            if (_viewModel == null)
            {
                Debug.LogError(
                    $"[GgumtleGameObject] GgumtleViewModel을 해결할 수 없어 구독을 건너뜀: {gameObject.name}"
                );
                return;
            }

            // 상태 변경에 따른 비주얼 처리
            _viewModel.State.Subscribe(OnViewModelStateChanged).AddTo(_disposables);

            // 홀드 진행률에 따른 이펙트 처리
            _viewModel
                .HoldProgress.Where(_ => _viewModel.State.Value == GgumtleState.Digging)
                .Subscribe(OnDiggingProgress)
                .AddTo(_disposables);

            // 먹이 진행률에 따른 이펙트 처리
            _viewModel
                .FoodProgress.Where(_ => _viewModel.State.Value == GgumtleState.Feeding)
                .Subscribe(OnFeedingProgress)
                .AddTo(_disposables);

            // 범위 감지에 따른 처리
            _viewModel.IsInRange.Subscribe(OnRangeChanged).AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log("[GgumtleGameObject] ViewModel R3 구독 완료");
        }

        private void OnViewModelStateChanged(GgumtleState newState)
        {
            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] ViewModel 상태 변경: {newState}");

            HandleStateVisuals(newState);
        }

        private void OnDiggingProgress(float progress)
        {
            // 파내기 진행률에 따른 이펙트 조절
            if (diggingEffect != null)
            {
                var main = diggingEffect.main;
                main.startLifetime = progress * 2f; // 진행률에 따라 파티클 생존시간 조절
            }

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 파내기 진행률: {progress:P1}");
        }

        private void OnFeedingProgress(float progress)
        {
            // 먹이주기 진행률에 따른 이펙트 조절
            if (feedingEffect != null)
            {
                var emission = feedingEffect.emission;
                emission.rateOverTime = progress * 20f; // 진행률에 따라 파티클 발생률 조절
            }

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 먹이 진행률: {progress:P1}");
        }

        private void OnRangeChanged(bool inRange)
        {
            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 범위 변경: {inRange}");

            // 범위에 따른 추가 처리가 필요하면 여기에 구현
        }

        #endregion

        #region 네트워크 이벤트 구독

        private void SubscribeToNetworkEvents()
        {
            if (_stateBroadcastSubscriber == null)
            {
                Debug.LogError($"[GgumtleGameObject] StateBroadcastSubscriber가 null이어서 네트워크 이벤트 구독 실패: {gameObject.name}");
                return;
            }

            // 서버에서 오는 상태 브로드캐스트 메시지 구독
            _stateBroadcastSubscriber
                .Subscribe(OnStateBroadcastReceived)
                .AddTo(_disposables);

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 네트워크 이벤트 구독 완료: {gameObject.name}");
        }

        private void OnStateBroadcastReceived(GgumtleStateBroadcastMessage message)
        {
            // 현재 꿈틀이 ID와 메시지의 ID가 일치하는지 확인
            if (message.GgumtleId != ggumtleId)
                return;

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 상태 브로드캐스트 수신: {gameObject.name}, Status={message.Status}");

            // 서버 상태 코드에 따라 애니메이터 파라미터 설정
            UpdateAnimatorParameters(message.Status);

            // ViewModel 상태도 업데이트
            var ggumtleState = ConvertStatusToGgumtleState(message.Status);
            if (ggumtleState.HasValue)
            {
                _viewModel.State.Value = ggumtleState.Value;

                // Fake 상태일 때 프로그레스바 숨기기
                if (ggumtleState.Value == GgumtleState.Fake)
                {
                    _viewModel.HoldProgress.Value = 0f;
                    _viewModel.FoodProgress.Value = 0f;
                }
            }
        }

        /// <summary>
        /// 서버 상태 코드에 따라 애니메이터 파라미터 설정
        /// </summary>
        private void UpdateAnimatorParameters(int status)
        {
            if (childAnimator == null)
            {
                Debug.LogWarning($"[GgumtleGameObject] Child Animator가 null이어서 파라미터 설정 실패: {gameObject.name}");
                return;
            }

            switch (status)
            {
                case 1: // 묻혀 있음
                    childAnimator.SetBool("IsDigging", false);
                    break;

                case 2: // 파는 중
                    childAnimator.SetBool("IsDigging", true);
                    childAnimator.SetBool("IsFeeding", false);
                    break;

                case 10: // 나와 있음 (PullUp)
                    childAnimator.SetTrigger("Emerge");
                    childAnimator.SetBool("IsFeeding", false);  // Feeding 중단
                    break;

                case 11: // 짭꿈틀
                    childAnimator.SetBool("IsFake", true);
                    
                    break;

                case 20: // 먹는 중
                    childAnimator.SetBool("IsFeeding", true);
                    break;

                case 30: // 정화 완료
                    childAnimator.SetTrigger("Purify");
                    break;

                default:
                    Debug.LogWarning($"[GgumtleGameObject] 알 수 없는 상태 코드: {status}");
                    break;
            }

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 애니메이터 파라미터 설정 완료: Status={status}");
        }

        /// <summary>
        /// 서버 상태 코드를 GgumtleState enum으로 변환
        /// </summary>
        private GgumtleState? ConvertStatusToGgumtleState(int status)
        {
            return status switch
            {
                1 => GgumtleState.Buried,
                2 => GgumtleState.Digging,
                10 => GgumtleState.Emerged, // 나와 있음 (먹이주기 대기)
                11 => GgumtleState.Fake,
                20 => GgumtleState.Feeding, // 먹는 중
                30 => GgumtleState.Purified,
                _ => null
            };
        }

        #endregion

        #region 비주얼 처리 (애니메이션 & 이펙트)

        private void HandleStateVisuals(GgumtleState state)
        {
            switch (state)
            {
                case GgumtleState.Buried:
                    OnStateBuried();
                    break;
                case GgumtleState.Digging:
                    OnStateDigging();
                    break;
                case GgumtleState.Emerging:
                    OnStateEmerging();
                    break;
                case GgumtleState.Emerged:
                    OnStateEmerged();
                    break;
                case GgumtleState.Feeding:
                    OnStateFeeding();
                    break;
                case GgumtleState.Purified:
                    OnStatePurified();
                    break;
                case GgumtleState.Fake:
                    OnStateFake();
                    break;
            }
        }

        private void OnStateBuried()
        {
            // 땅에 묻힌 상태 - 모든 이펙트 중지
            StopAllEffects();

            if (ggumtleAnimator != null)
            {
                ggumtleAnimator.SetBool("IsDigging", false);
                ggumtleAnimator.SetBool("IsFeeding", false);
            }
        }

        private void OnStateDigging()
        {
            // 파내는 중 상태
            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] OnStateDigging 호출됨 - Animator: {ggumtleAnimator != null}, Trigger: '{diggingAnimationTrigger}'");

            if (ggumtleAnimator != null && !string.IsNullOrEmpty(diggingAnimationTrigger))
            {
                ggumtleAnimator.SetTrigger(diggingAnimationTrigger);
                ggumtleAnimator.SetBool("IsDigging", true);

                if (enableDebugLogs)
                    Debug.Log($"[GgumtleGameObject] 파기 애니메이션 실행: {diggingAnimationTrigger}");
            }
            else
            {
                Debug.LogWarning($"[GgumtleGameObject] 파기 애니메이션 실행 실패 - Animator: {ggumtleAnimator != null}, Trigger: '{diggingAnimationTrigger}'");
            }

            if (diggingEffect != null)
            {
                diggingEffect.Play();
            }
        }

        private void OnStateEmerging()
        {
            // 나오는 중 상태 - 파내기 이펙트 중지
            if (diggingEffect != null)
            {
                diggingEffect.Stop();
            }

            if (ggumtleAnimator != null && !string.IsNullOrEmpty(emergingAnimationTrigger))
            {
                ggumtleAnimator.SetTrigger(emergingAnimationTrigger);
                ggumtleAnimator.SetBool("IsDigging", false);
            }
        }

        private void OnStateEmerged()
        {
            // 나와 있는 상태 - 먹이주기 대기 상태 (기본 상태)
            if (ggumtleAnimator != null)
            {
                ggumtleAnimator.SetBool("IsDigging", false);
                ggumtleAnimator.SetBool("IsFeeding", false);
                ggumtleAnimator.SetBool("IsFake", false);
            }

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 나와 있음 상태 (먹이주기 대기): {gameObject.name}");
        }

        private void OnStateFeeding()
        {
            // 먹이주기 가능 상태
            if (ggumtleAnimator != null && !string.IsNullOrEmpty(feedingAnimationTrigger))
            {
                ggumtleAnimator.SetTrigger(feedingAnimationTrigger);
                ggumtleAnimator.SetBool("IsFeeding", true);
            }

            if (feedingEffect != null)
            {
                feedingEffect.Play();
            }
        }

        private void OnStatePurified()
        {
            // 정화 완료 상태
            StopAllEffects();

            if (ggumtleAnimator != null && !string.IsNullOrEmpty(purifiedAnimationTrigger))
            {
                ggumtleAnimator.SetTrigger(purifiedAnimationTrigger);
                ggumtleAnimator.SetBool("IsFeeding", false);
            }

            if (purificationEffect != null)
            {
                purificationEffect.Play();
            }

            // 3초 후 오브젝트 제거
            Observable
                .Timer(System.TimeSpan.FromSeconds(3f))
                .Subscribe(_ => DestroyGameObject())
                .AddTo(_disposables);
        }

        private void StopAllEffects()
        {
            if (diggingEffect != null)
                diggingEffect.Stop();
            if (feedingEffect != null)
                feedingEffect.Stop();
        }

        private void OnStateFake()
        {
            // 짭꿈틀 상태 - Fake 이펙트 재생 후 즉시 삭제
            StopAllEffects();

            if (ggumtleAnimator != null)
            {
                ggumtleAnimator.SetTrigger("Fake");
                ggumtleAnimator.SetBool("isFake", true);
            }

            // Fake 이펙트 재생하고 삭제
            PlayFakeEffectAndDestroy();

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 짭꿈틀 발견 - 이펙트 재생 후 삭제: {gameObject.name}");
        }

        private void PlayFakeEffectAndDestroy()
        {
            // Main 하위에서 Fake_effect 찾기
            Transform mainTransform = transform.Find("Main");
            if (mainTransform != null)
            {
                Transform fakeEffectTransform = mainTransform.Find("Fake_effect");
                if (fakeEffectTransform != null)
                {
                    // Fake_effect와 모든 하위 파티클 시스템 재생
                    ParticleSystem[] allParticles = fakeEffectTransform.GetComponentsInChildren<ParticleSystem>();
                    float maxDuration = 0f;

                    foreach (ParticleSystem ps in allParticles)
                    {
                        ps.gameObject.SetActive(true);
                        ps.Play();

                        // 가장 긴 파티클 duration 계산
                        var main = ps.main;
                        float duration = main.startLifetime.constantMax + main.duration;
                        if (duration > maxDuration)
                            maxDuration = duration;

                        if (enableDebugLogs)
                            Debug.Log($"[GgumtleGameObject] Fake 이펙트 파티클 재생: {ps.name}");
                    }

                    // 이펙트 재생 후 오브젝트 삭제
                    Observable
                        .Timer(System.TimeSpan.FromSeconds(maxDuration))
                        .Subscribe(_ => DestroyGameObject())
                        .AddTo(_disposables);
                }
                else
                {
                    Debug.LogWarning($"[GgumtleGameObject] Fake_effect를 찾을 수 없습니다: {gameObject.name}");
                    DestroyGameObject(); // 이펙트 없어도 즉시 삭제
                }
            }
            else
            {
                Debug.LogWarning($"[GgumtleGameObject] Main 오브젝트를 찾을 수 없습니다: {gameObject.name}");
                DestroyGameObject(); // 이펙트 없어도 즉시 삭제
            }
        }

        private void DestroyGameObject()
        {
            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 오브젝트 제거: {gameObject.name}");

            Destroy(gameObject);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 애니메이션 이벤트 핸들러에서 호출 - ViewModel 상태를 Fake로 변경
        /// </summary>
        public void SetFakeState()
        {
            if (_viewModel != null)
            {
                _viewModel.State.Value = GgumtleState.Fake;
                if (enableDebugLogs)
                    Debug.Log($"[GgumtleGameObject] ViewModel 상태를 Fake로 변경: {gameObject.name}");
            }
            else
            {
                Debug.LogError($"[GgumtleGameObject] ViewModel이 null이어서 Fake 상태 설정 실패: {gameObject.name}");
            }
        }

        #endregion

        #region 디버그 및 유틸리티


        [ContextMenu("Log Current State")]
        /// <summary>
        /// 현재 꿈틀이 상태 반환
        /// </summary>
        public GgumtleState GetCurrentState()
        {
            // GgumtleService에서 현재 상태 가져오기
            var data = _ggumtleService?.GetGgumtleData(GgumtleId.ToString());
            if (data != null)
            {
                return data.currentState;
            }

            // 기본값 반환
            return GgumtleState.Buried;
        }

        public void LogCurrentState()
        {
            if (_viewModel == null)
            {
                Debug.Log("[GgumtleGameObject] ViewModel is null");
                return;
            }

            Debug.Log(
                $"[GgumtleGameObject] State Report:\\n"
                    + $"  GameObject: {gameObject.name}\\n"
                    + $"  Ggumtle ID: {ggumtleId}\\n"
                    + $"  ViewModel State: {_viewModel.State.Value}\\n"
                    + $"  In Range: {_viewModel.IsInRange.Value}\\n"
                    + $"  Is Holding: {_viewModel.IsHolding.Value}"
            );
        }

        #endregion
    }
}
