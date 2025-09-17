// using Models;
// using R3;
// using UnityEngine;
// using VContainer;
// using VContainer.Unity;
// using ViewModels;
// using R3DisposableBag = R3.DisposableBag;

// namespace Views
// {
//     /// <summary>
//     /// 꿈틀이 GameObject의 비주얼 처리를 담당하는 View
//     /// 애니메이션, 이펙트, 3D 모델 등 GameObject 관련 표현을 처리
//     /// </summary>
//     public class GgumtleGameObjectView : MonoBehaviour
//     {
//         [Header("Visual Components")]
//         [SerializeField]
//         private Animator animator;

//         [SerializeField]
//         private ParticleSystem diggingEffect;

//         [SerializeField]
//         private ParticleSystem purificationEffect;

//         [SerializeField]
//         private ParticleSystem feedingEffect;

//         [Header("Animation Triggers")]
//         [SerializeField]
//         private string diggingTrigger = "Dig";

//         [SerializeField]
//         private string emergingTrigger = "Emerge";

//         [SerializeField]
//         private string feedingTrigger = "Feed";

//         [SerializeField]
//         private string purifyTrigger = "Purify";

//         [Header("Settings")]
//         [SerializeField]
//         private bool enableDebugLogs = false;

//         private GgumtleViewModel _viewModel;
//         private R3DisposableBag _disposables = new();

//         private void Start()
//         {
//             InitializeComponents();
//             ResolveDependencies();
//             SubscribeToViewModel();
//         }

//         private void ResolveDependencies()
//         {
//             try
//             {
//                 // VContainer에서 직접 해결 (Self-Resolving 패턴)
//                 var lifetimeScope = Object.FindFirstObjectByType<DI.GameLifetimeScope>();
//                 if (lifetimeScope != null)
//                 {
//                     _viewModel = lifetimeScope.Container.Resolve<GgumtleViewModel>();
//                     if (enableDebugLogs)
//                         Debug.Log(
//                             $"[GgumtleGameObjectView] ViewModel 자동 해결 성공: {gameObject.name}"
//                         );
//                 }
//                 else
//                 {
//                     Debug.LogError(
//                         $"[GgumtleGameObjectView] GameLifetimeScope를 찾을 수 없음: {gameObject.name}"
//                     );
//                 }
//             }
//             catch (System.Exception ex)
//             {
//                 Debug.LogError(
//                     $"[GgumtleGameObjectView] ViewModel 해결 실패: {gameObject.name}, 오류: {ex.Message}"
//                 );
//             }
//         }

//         private void InitializeComponents()
//         {
//             if (animator == null)
//                 animator = GetComponent<Animator>();

//             if (animator == null)
//                 Debug.LogWarning(
//                     $"[GgumtleGameObjectView] Animator를 찾을 수 없음: {gameObject.name}"
//                 );

//             if (enableDebugLogs)
//                 Debug.Log($"[GgumtleGameObjectView] 초기화 완료: {gameObject.name}");
//         }

//         private void SubscribeToViewModel()
//         {
//             if (_viewModel == null)
//             {
//                 Debug.LogError(
//                     $"[GgumtleGameObjectView] GgumtleViewModel을 해결할 수 없어 구독을 건너뛀: {gameObject.name}"
//                 );
//                 return;
//             }

//             // 상태 변경에 따른 비주얼 처리
//             _viewModel
//                 .State.Subscribe(newState => OnStateChanged(newState))
//                 .AddTo(ref _disposables);

//             // 홀드 진행률에 따른 이펙트 처리
//             _viewModel
//                 .HoldProgress.Where(_ => _viewModel.State.Value == GgumtleState.Digging)
//                 .Subscribe(progress => OnDiggingProgress(progress))
//                 .AddTo(ref _disposables);

//             // 먹이 진행률에 따른 이펙트 처리
//             _viewModel
//                 .FoodProgress.Where(_ => _viewModel.State.Value == GgumtleState.Feeding)
//                 .Subscribe(progress => OnFeedingProgress(progress))
//                 .AddTo(ref _disposables);

//             if (enableDebugLogs)
//                 Debug.Log("[GgumtleGameObjectView] ViewModel 구독 완료");
//         }

//         private void OnStateChanged(GgumtleState newState)
//         {
//             if (enableDebugLogs)
//                 Debug.Log($"[GgumtleGameObjectView] 상태 변경: {newState}");

//             switch (newState)
//             {
//                 case GgumtleState.Buried:
//                     OnStateBuried();
//                     break;
//                 case GgumtleState.Digging:
//                     OnStateDigging();
//                     break;
//                 case GgumtleState.Emerging:
//                     OnStateEmerging();
//                     break;
//                 case GgumtleState.Feeding:
//                     OnStateFeeding();
//                     break;
//                 case GgumtleState.Purified:
//                     OnStatePurified();
//                     break;
//             }
//         }

//         private void OnStateBuried()
//         {
//             // 땅에 묻힌 상태 - 모든 이펙트 중지
//             StopAllEffects();

//             if (animator != null)
//             {
//                 // 묻힌 상태 애니메이션 (있다면)
//                 animator.SetBool("IsDigging", false);
//                 animator.SetBool("IsFeeding", false);
//             }
//         }

//         private void OnStateDigging()
//         {
//             // 파내는 중 상태
//             if (animator != null && !string.IsNullOrEmpty(diggingTrigger))
//             {
//                 animator.SetTrigger(diggingTrigger);
//                 animator.SetBool("IsDigging", true);
//             }

//             if (diggingEffect != null)
//             {
//                 diggingEffect.Play();
//             }
//         }

//         private void OnStateEmerging()
//         {
//             // 나오는 중 상태 - 파내기 이펙트 중지
//             if (diggingEffect != null)
//             {
//                 diggingEffect.Stop();
//             }

//             if (animator != null && !string.IsNullOrEmpty(emergingTrigger))
//             {
//                 animator.SetTrigger(emergingTrigger);
//                 animator.SetBool("IsDigging", false);
//             }
//         }

//         private void OnStateFeeding()
//         {
//             // 먹이주기 가능 상태
//             if (animator != null && !string.IsNullOrEmpty(feedingTrigger))
//             {
//                 animator.SetTrigger(feedingTrigger);
//                 animator.SetBool("IsFeeding", true);
//             }

//             if (feedingEffect != null)
//             {
//                 feedingEffect.Play();
//             }
//         }

//         private void OnStatePurified()
//         {
//             // 정화 완료 상태
//             StopAllEffects();

//             if (animator != null && !string.IsNullOrEmpty(purifyTrigger))
//             {
//                 animator.SetTrigger(purifyTrigger);
//                 animator.SetBool("IsFeeding", false);
//             }

//             if (purificationEffect != null)
//             {
//                 purificationEffect.Play();
//             }

//             // 3초 후 오브젝트 제거
//             Observable
//                 .Timer(System.TimeSpan.FromSeconds(3f))
//                 .Subscribe(_ => DestroyGameObject())
//                 .AddTo(ref _disposables);
//         }

//         private void OnDiggingProgress(float progress)
//         {
//             // 파내기 진행률에 따른 이펙트 조절
//             if (diggingEffect != null)
//             {
//                 var main = diggingEffect.main;
//                 main.startLifetime = progress * 2f; // 진행률에 따라 파티클 생존시간 조절
//             }
//         }

//         private void OnFeedingProgress(float progress)
//         {
//             // 먹이주기 진행률에 따른 이펙트 조절
//             if (feedingEffect != null)
//             {
//                 var emission = feedingEffect.emission;
//                 emission.rateOverTime = progress * 20f; // 진행률에 따라 파티클 발생률 조절
//             }
//         }

//         private void StopAllEffects()
//         {
//             if (diggingEffect != null)
//                 diggingEffect.Stop();
//             if (feedingEffect != null)
//                 feedingEffect.Stop();
//         }

//         private void DestroyGameObject()
//         {
//             if (enableDebugLogs)
//                 Debug.Log($"[GgumtleGameObjectView] 정화 완료 - 오브젝트 제거: {gameObject.name}");

//             Destroy(gameObject);
//         }

//         private void OnDestroy()
//         {
//             // R3 구독 해제
//             _disposables.Dispose();

//             if (enableDebugLogs)
//                 Debug.Log($"[GgumtleGameObjectView] OnDestroy: {gameObject.name}");
//         }
//     }
// }
