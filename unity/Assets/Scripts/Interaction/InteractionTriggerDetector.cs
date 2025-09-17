using System;
using UnityEngine;
using Config;

namespace Interaction
{
    /// <summary>
    /// 플레이어에 부착하여 상호작용 객체를 실시간으로 감지하는 컴포넌트
    /// Trigger 방식으로 폴링 없이 즉시 반응
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractionTriggerDetector : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private float detectionRadius = InteractionConfig.PlayerDetectionRadius;
        [SerializeField] private LayerMask interactionLayerMask = 128; // Layer 7만 감지 (Interaction layer)

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;
        [SerializeField] private bool showGizmosInEditor = true;

        // 상호작용 객체 감지 이벤트
        public event Action<IInteractable> OnInteractableEntered;
        public event Action<IInteractable> OnInteractableExited;

        private SphereCollider _triggerCollider;

        void Awake()
        {
            // Config에서 최신 범위 값 적용
            detectionRadius = InteractionConfig.PlayerDetectionRadius;
            SetupTriggerCollider();
        }

        private void SetupTriggerCollider()
        {
            // 기존 Collider 찾기 또는 새로 생성
            _triggerCollider = GetComponent<SphereCollider>();
            if (_triggerCollider == null)
            {
                _triggerCollider = gameObject.AddComponent<SphereCollider>();
                Debug.Log("[InteractionTriggerDetector] SphereCollider 자동 생성");
            }

            // Trigger 설정
            _triggerCollider.isTrigger = true;
            _triggerCollider.radius = detectionRadius;

            // Physics Layer 설정 (나중에 최적화용)
            if (gameObject.layer == 0) // Default layer
            {
                Debug.LogWarning("[InteractionTriggerDetector] 성능 최적화를 위해 전용 레이어 설정을 권장합니다.");
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[InteractionTriggerDetector] Trigger 초기화 완료 - 반지름: {detectionRadius}");
            }
        }

        void OnTriggerEnter(Collider other)
        {
            Debug.Log($"[InteractionTriggerDetector] OnTriggerEnter 호출됨: {other.gameObject.name}, Layer: {other.gameObject.layer}");

            // 레이어 마스크 체크 (상호작용 객체는 Layer 7, 다른 것들은 다른 레이어이므로 필터링)
            if (!IsInLayerMask(other.gameObject.layer, interactionLayerMask))
            {
                Debug.Log($"[InteractionTriggerDetector] 레이어 필터링됨: {other.gameObject.name}, Layer: {other.gameObject.layer}");
                return;
            }

            var interactable = other.GetComponent<IInteractable>();
            Debug.Log($"[InteractionTriggerDetector] IInteractable 컴포넌트: {interactable != null}");

            if (interactable != null)
            {
                bool canInteract = interactable.CanInteract();
                Debug.Log($"[InteractionTriggerDetector] CanInteract: {canInteract}");

                if (canInteract)
                {
                    OnInteractableEntered?.Invoke(interactable);
                    Debug.Log($"[InteractionTriggerDetector] 상호작용 객체 감지 성공: {other.gameObject.name}");
                }
            }
        }

        void OnTriggerExit(Collider other)
        {
            Debug.Log($"[InteractionTriggerDetector] OnTriggerExit 호출됨: {other.gameObject.name}");

            // 레이어 마스크 체크
            if (!IsInLayerMask(other.gameObject.layer, interactionLayerMask))
                return;

            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null)
            {
                OnInteractableExited?.Invoke(interactable);
                Debug.Log($"[InteractionTriggerDetector] 상호작용 객체 벗어남 성공: {other.gameObject.name}");
            }
        }

        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        /// <summary>
        /// 감지 반지름 동적 변경
        /// </summary>
        public void SetDetectionRadius(float radius)
        {
            detectionRadius = radius;
            if (_triggerCollider != null)
            {
                _triggerCollider.radius = radius;
            }
        }

        /// <summary>
        /// 레이어 마스크 동적 변경
        /// </summary>
        public void SetLayerMask(LayerMask layerMask)
        {
            interactionLayerMask = layerMask;
        }

        /// <summary>
        /// 현재 감지 중인 상호작용 객체들 가져오기 (필요시)
        /// </summary>
        public IInteractable[] GetCurrentInteractables()
        {
            var colliders = Physics.OverlapSphere(transform.position, detectionRadius, interactionLayerMask);
            var interactables = new System.Collections.Generic.List<IInteractable>();

            foreach (var collider in colliders)
            {
                var interactable = collider.GetComponent<IInteractable>();
                if (interactable != null && interactable.CanInteract())
                {
                    interactables.Add(interactable);
                }
            }

            return interactables.ToArray();
        }

        void OnDrawGizmosSelected()
        {
            if (showGizmosInEditor)
            {
                // 감지 범위 시각화
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, detectionRadius);

                Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
                Gizmos.DrawSphere(transform.position, detectionRadius);
            }
        }

        #region Public Properties

        public float DetectionRadius => detectionRadius;
        public LayerMask InteractionLayerMask => interactionLayerMask;
        public bool IsDebugEnabled => enableDebugLogs;

        #endregion
    }
}