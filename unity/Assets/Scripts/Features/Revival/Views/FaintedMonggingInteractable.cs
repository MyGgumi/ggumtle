using Cysharp.Threading.Tasks;
using Features.Revival.ViewModels;
using UnityEngine;
using VContainer;

namespace Features.Revival.Views
{
    /// <summary>
    /// 기절한 몽깅이와 상호작용할 수 있는 컴포넌트
    /// IInteractable을 구현하여 InteractionTriggerDetector에서 감지됨
    /// </summary>
    public class FaintedMonggingInteractable : MonoBehaviour, IInteractable
    {
        [Header("Interaction Settings")]
        [SerializeField]
        private float interactionRange = 2f;

        [SerializeField]
        private float holdDuration = 3f;

        [Header("Player Info")]
        [SerializeField]
        private long faintedPlayerId = -1;

        [SerializeField]
        private string playerName = "Unknown";

        [Header("Debug")]
        [SerializeField]
        private bool enableDebugLogs = true;

        #region Private Fields

        private bool _isInteracting = false;
        private bool _canInteract = true;
        private Coroutine _holdCoroutine;

        #endregion

        #region Public Properties

        /// <summary>
        /// 기절한 플레이어의 ID
        /// </summary>
        public long PlayerId => faintedPlayerId;

        /// <summary>
        /// 기절한 플레이어의 이름
        /// </summary>
        public string PlayerName => playerName;

        #endregion

        #region Dependencies

        private RevivalViewModel _revivalViewModel;

        #endregion

        #region Initialization

        [Inject]
        public void Construct(RevivalViewModel revivalViewModel)
        {
            _revivalViewModel = revivalViewModel;

            if (enableDebugLogs)
            {
                Debug.Log($"[FaintedMonggingInteractable] VContainer 의존성 주입 완료: {_revivalViewModel != null}");
            }
        }

        /// <summary>
        /// 기절한 플레이어 정보 설정
        /// </summary>
        public void SetFaintedPlayer(long playerId, string name)
        {
            faintedPlayerId = playerId;
            playerName = name;

            if (enableDebugLogs)
            {
                Debug.Log($"[FaintedMonggingInteractable] 기절한 플레이어 설정: ID={playerId}, Name={name}");
            }
        }

        #endregion

        #region IInteractable Implementation

        public bool CanInteract()
        {
            // 기절한 플레이어 ID가 설정되어 있고, 상호작용 가능한 상태인지 확인
            bool canInteract = _canInteract &&
                              faintedPlayerId > 0 &&
                              !_isInteracting &&
                              (_revivalViewModel == null || !_revivalViewModel.IsAnyRevivalInProgress());

            if (enableDebugLogs && !canInteract)
            {
                Debug.Log($"[FaintedMonggingInteractable] 상호작용 불가: CanInteract={_canInteract}, PlayerId={faintedPlayerId}, IsInteracting={_isInteracting}");
            }

            return canInteract;
        }

        public void Interact()
        {
            if (!CanInteract())
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[FaintedMonggingInteractable] 상호작용 실행 불가");
                }
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[FaintedMonggingInteractable] 상호작용 시작: 플레이어 {playerName} 부활 시도");
            }

            // RevivalViewModel을 통해 직접 부활 시작
            StartDirectRevival();
        }

        public void OnInteractionEnd()
        {
            if (_isInteracting)
            {
                if (enableDebugLogs)
                {
                    Debug.Log("[FaintedMonggingInteractable] 상호작용 종료");
                }

                CancelDirectRevival();
            }
        }

        public float GetInteractionRange()
        {
            return interactionRange;
        }

        public Transform GetTransform()
        {
            return transform;
        }

        public string GetInteractableName()
        {
            return $"기절한 {playerName}";
        }

        public void OnHoldStart()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[FaintedMonggingInteractable] 홀드 시작");
            }

            StartDirectRevival();
        }

        public void OnHoldProgress(float progress)
        {
            // 진행률은 RevivalViewModel에서 관리됨
            if (enableDebugLogs)
            {
                Debug.Log($"[FaintedMonggingInteractable] 홀드 진행: {progress:P1}");
            }
        }

        public void OnHoldComplete()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[FaintedMonggingInteractable] 홀드 완료");
            }

            // 홀드 완료는 서버에서 MonggingRevivalComplete로 처리됨
        }

        public void OnHoldCancelled()
        {
            if (enableDebugLogs)
            {
                Debug.Log("[FaintedMonggingInteractable] 홀드 취소");
            }

            CancelDirectRevival();
        }

        public float GetHoldDuration()
        {
            return holdDuration;
        }

        #endregion

        #region Revival Methods

        private async void StartDirectRevival()
        {
            if (_revivalViewModel == null)
            {
                Debug.LogError("[FaintedMonggingInteractable] RevivalViewModel이 null입니다!");
                return;
            }

            if (_isInteracting)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[FaintedMonggingInteractable] 이미 상호작용 중입니다");
                }
                return;
            }

            _isInteracting = true;

            try
            {
                // 상호작용 상태 업데이트
                _revivalViewModel.UpdateInteractionState(
                    faintedPlayerId,
                    true,
                    true,
                    Vector3.Distance(transform.position, Camera.main?.transform.position ?? Vector3.zero),
                    transform.position
                );

                // 직접 부활 시작
                bool success = await _revivalViewModel.StartDirectRevivalAsync();

                if (!success)
                {
                    if (enableDebugLogs)
                    {
                        Debug.LogWarning("[FaintedMonggingInteractable] 직접 부활 시작 실패");
                    }

                    _isInteracting = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FaintedMonggingInteractable] 직접 부활 시작 중 예외 발생: {e.Message}");
                _isInteracting = false;
            }
        }

        private async void CancelDirectRevival()
        {
            if (_revivalViewModel == null || !_isInteracting)
            {
                return;
            }

            try
            {
                await _revivalViewModel.CancelDirectRevivalAsync();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[FaintedMonggingInteractable] 직접 부활 취소 중 예외 발생: {e.Message}");
            }
            finally
            {
                _isInteracting = false;
            }
        }

        #endregion

        #region Unity Lifecycle

        private void OnDisable()
        {
            // 비활성화 시 상호작용 종료
            if (_isInteracting)
            {
                OnInteractionEnd();
            }
        }

        private void OnDestroy()
        {
            // 오브젝트 파괴 시 상호작용 정리
            if (_revivalViewModel != null)
            {
                _revivalViewModel.ClearInteractionState();
            }

            if (enableDebugLogs)
            {
                Debug.Log("[FaintedMonggingInteractable] OnDestroy");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 상호작용 활성화/비활성화
        /// </summary>
        public void SetInteractionEnabled(bool enabled)
        {
            _canInteract = enabled;

            if (!enabled && _isInteracting)
            {
                OnInteractionEnd();
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[FaintedMonggingInteractable] 상호작용 {(enabled ? "활성화" : "비활성화")}");
            }
        }

        /// <summary>
        /// 현재 상호작용 중인지 확인
        /// </summary>
        public bool IsInteracting()
        {
            return _isInteracting;
        }

        #endregion

        #region Debug

        private void OnDrawGizmosSelected()
        {
            // 상호작용 범위 시각화
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionRange);

            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawSphere(transform.position, interactionRange);
        }

        [ContextMenu("Test Interaction")]
        private void TestInteraction()
        {
            if (Application.isPlaying)
            {
                Interact();
            }
        }

        [ContextMenu("Test Cancel Interaction")]
        private void TestCancelInteraction()
        {
            if (Application.isPlaying)
            {
                OnInteractionEnd();
            }
        }

        #endregion
    }
}