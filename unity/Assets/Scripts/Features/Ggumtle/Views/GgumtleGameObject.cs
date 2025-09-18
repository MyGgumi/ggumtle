using Features.Ggumtle.Models;
using Features.Ggumtle.ViewModels;
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
        private R3DisposableBag _disposables = new();

        #endregion

        #region Unity Lifecycle

        void Start()
        {
            Debug.Log(
                $"[GgumtleGameObject] Start() 호출됨 - GameObject: {gameObject.name}, Layer: {gameObject.layer}"
            );

            InitializeBasicComponents();
            SetupInteractionLayer(); // Layer 설정 추가
            ResolveDependencies();
            RegisterToService(); // GgumtleService에 등록
            SubscribeToViewModel();

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
            // ID 자동 생성 (0이면)
            if (ggumtleId == 0)
            {
                // 랜덤한 양수 int 생성 (1~999999)
                ggumtleId = UnityEngine.Random.Range(1, 1000000);
                if (enableDebugLogs)
                    Debug.Log($"[GgumtleGameObject] ID 자동 생성: {ggumtleId}");
            }

            // 컴포넌트 자동 찾기
            if (ggumtleAnimator == null)
                ggumtleAnimator = GetComponent<Animator>();

            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 컴포넌트 초기화 완료: {gameObject.name}");
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

        private void ResolveDependencies()
        {
            try
            {
                // VContainer에서 직접 해결 (Self-Resolving 패턴)
                var lifetimeScope = Object.FindFirstObjectByType<DI.GameLifetimeScope>();
                if (lifetimeScope != null)
                {
                    _viewModel = lifetimeScope.Container.Resolve<GgumtleViewModel>();
                    if (enableDebugLogs)
                        Debug.Log(
                            $"[GgumtleGameObject] ViewModel 자동 해결 성공: {gameObject.name}"
                        );
                }
                else
                {
                    Debug.LogError(
                        $"[GgumtleGameObject] GameLifetimeScope를 찾을 수 없음: {gameObject.name}"
                    );
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError(
                    $"[GgumtleGameObject] ViewModel 해결 실패: {gameObject.name}, 오류: {ex.Message}"
                );
            }
        }

        #endregion

        #region Service Registration

        /// <summary>
        /// GgumtleService에 자신을 등록
        /// </summary>
        private void RegisterToService()
        {
            try
            {
                // VContainer에서 직접 IGgumtleService 해결
                var scope = FindObjectOfType<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    var ggumtleService = scope.Container.Resolve<Features.Ggumtle.Services.IGgumtleService>();

                    // GgumtleService에 등록 (ID, 이름, 위치)
                    ggumtleService.RegisterGgumtle(ggumtleId.ToString(), gameObject.name, transform.position);
                    Debug.Log($"[GgumtleGameObject] GgumtleService 등록 완료: {gameObject.name}, ID: {ggumtleId}");
                }
                else
                {
                    Debug.LogError($"[GgumtleGameObject] GameLifetimeScope를 찾을 수 없어 등록 실패: {gameObject.name}");
                }
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
            try
            {
                // VContainer에서 직접 IGgumtleService 해결
                var scope = FindObjectOfType<GameLifetimeScope>();
                if (scope != null && scope.Container != null)
                {
                    var ggumtleService = scope.Container.Resolve<Features.Ggumtle.Services.IGgumtleService>();

                    // GgumtleService에서 해제
                    ggumtleService.UnregisterGgumtle(ggumtleId.ToString());
                    Debug.Log($"[GgumtleGameObject] GgumtleService 해제 완료: {gameObject.name}, ID: {ggumtleId}");
                }
                else
                {
                    Debug.LogError($"[GgumtleGameObject] GameLifetimeScope를 찾을 수 없어 해제 실패: {gameObject.name}");
                }
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
            _viewModel.State.Subscribe(OnViewModelStateChanged).AddTo(ref _disposables);

            // 홀드 진행률에 따른 이펙트 처리
            _viewModel
                .HoldProgress.Where(_ => _viewModel.State.Value == GgumtleState.Digging)
                .Subscribe(OnDiggingProgress)
                .AddTo(ref _disposables);

            // 먹이 진행률에 따른 이펙트 처리
            _viewModel
                .FoodProgress.Where(_ => _viewModel.State.Value == GgumtleState.Feeding)
                .Subscribe(OnFeedingProgress)
                .AddTo(ref _disposables);

            // 범위 감지에 따른 처리
            _viewModel.IsInRange.Subscribe(OnRangeChanged).AddTo(ref _disposables);

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
                case GgumtleState.Feeding:
                    OnStateFeeding();
                    break;
                case GgumtleState.Purified:
                    OnStatePurified();
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
            if (ggumtleAnimator != null && !string.IsNullOrEmpty(diggingAnimationTrigger))
            {
                ggumtleAnimator.SetTrigger(diggingAnimationTrigger);
                ggumtleAnimator.SetBool("IsDigging", true);
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
                .AddTo(ref _disposables);
        }

        private void StopAllEffects()
        {
            if (diggingEffect != null)
                diggingEffect.Stop();
            if (feedingEffect != null)
                feedingEffect.Stop();
        }

        private void DestroyGameObject()
        {
            if (enableDebugLogs)
                Debug.Log($"[GgumtleGameObject] 정화 완료 - 오브젝트 제거: {gameObject.name}");

            Destroy(gameObject);
        }

        #endregion


        #region 디버그 및 유틸리티


        [ContextMenu("Log Current State")]
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
