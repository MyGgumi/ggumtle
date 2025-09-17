using System;
using Config;
using Features.Ggumtle.Messages;
using Features.Ggumtle.Models;
using Features.Ggumtle.Views;
using MessagePipe;
using UnityEngine;
using VContainer;

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
        [SerializeField]
        [Range(1f, 20f)]
        private float detectionRadius = 6f; // Inspector에서 조절 가능

        [SerializeField]
        private LayerMask interactionLayerMask = 128; // Layer 7만 감지 (Interaction layer)

        [Header("Debug")]
        [SerializeField]
        private bool enableDebugLogs = false;

        [SerializeField]
        private bool showGizmosInEditor = true;

        // MessagePipe Publishers (VContainer로 주입)
        [Inject]
        private IPublisher<Features.Ggumtle.Messages.GgumtleDetectedMessage> _ggumtleDetectedPublisher;

        [Inject]
        private IPublisher<Features.Ggumtle.Messages.GgumtleLeftMessage> _ggumtleLeftPublisher;

        // 기존 C# Event도 호환성을 위해 유지 (다른 상호작용 객체용)
        public event Action<IInteractable> OnInteractableEntered;
        public event Action<IInteractable> OnInteractableExited;

        private Collider _triggerCollider; // SphereCollider 또는 CapsuleCollider 모두 지원

        void Awake()
        {
            Debug.Log(
                $"[InteractionTriggerDetector] Awake 호출 - GameObject: {gameObject.name}, Layer: {gameObject.layer}"
            );

            SetupTriggerCollider();

            // Character Controller와 함께 사용 중인지 확인
            var characterController = GetComponentInParent<CharacterController>();
            if (characterController != null)
            {
                Debug.Log($"[InteractionTriggerDetector] Parent에 CharacterController 있음");
            }
        }

        void Start()
        {
            // 지연된 DI 확인 (VContainer 초기화가 완료될 때까지 대기)
            StartCoroutine(CheckDependenciesAfterFrame());
        }

        private System.Collections.IEnumerator CheckDependenciesAfterFrame()
        {
            yield return null; // 한 프레임 대기

            // VContainer 주입 확인 및 수동 해결
            if (_ggumtleDetectedPublisher == null || _ggumtleLeftPublisher == null)
            {
                try
                {
                    // GlobalMessagePipe로 수동 해결 시도
                    _ggumtleDetectedPublisher ??=
                        GlobalMessagePipe.GetPublisher<Features.Ggumtle.Messages.GgumtleDetectedMessage>();
                    _ggumtleLeftPublisher ??=
                        GlobalMessagePipe.GetPublisher<Features.Ggumtle.Messages.GgumtleLeftMessage>();

                    Debug.Log(
                        $"[InteractionTriggerDetector] GlobalMessagePipe로 수동 주입 완료 - {gameObject.name}"
                    );
                }
                catch (System.Exception ex)
                {
                    Debug.LogError(
                        $"[InteractionTriggerDetector] 수동 주입 실패: {ex.Message} - {gameObject.name}"
                    );
                }
            }

            // 최종 확인
            if (_ggumtleDetectedPublisher == null)
            {
                Debug.LogError(
                    $"[InteractionTriggerDetector] GgumtleDetectedPublisher가 주입되지 않음! - {gameObject.name}"
                );
            }
            if (_ggumtleLeftPublisher == null)
            {
                Debug.LogError(
                    $"[InteractionTriggerDetector] GgumtleLeftPublisher가 주입되지 않음! - {gameObject.name}"
                );
            }

            // Collider 타입별 정보 표시
            string colliderInfo = _triggerCollider switch
            {
                CapsuleCollider c => $"CapsuleCollider(R:{c.radius}, H:{c.height})",
                SphereCollider s => $"SphereCollider(R:{s.radius})",
                BoxCollider b => $"BoxCollider(S:{b.size})",
                null => "없음",
                _ => _triggerCollider.GetType().Name,
            };

            Debug.Log(
                $"[InteractionTriggerDetector] Start 완료 - {colliderInfo}, IsTrigger: {_triggerCollider?.isTrigger}"
            );
        }

        private void SetupTriggerCollider()
        {
            // 기존 Collider 찾기 (어떤 타입이든)
            _triggerCollider = GetComponent<Collider>();

            if (_triggerCollider == null)
            {
                // 기본적으로 SphereCollider 생성
                _triggerCollider = gameObject.AddComponent<SphereCollider>();
                ((SphereCollider)_triggerCollider).radius = detectionRadius;
                Debug.Log(
                    $"[InteractionTriggerDetector] SphereCollider 자동 생성 - {gameObject.name}"
                );
            }
            else
            {
                // 기존 Collider 타입 확인 및 설정
                if (_triggerCollider is CapsuleCollider capsule)
                {
                    // CapsuleCollider는 radius와 height로 범위 설정
                    capsule.radius = detectionRadius;
                    capsule.height = detectionRadius * 2f; // 높이는 반경의 2배 정도로
                    Debug.Log(
                        $"[InteractionTriggerDetector] CapsuleCollider 사용 - {gameObject.name}, Radius: {capsule.radius}, Height: {capsule.height}"
                    );
                }
                else if (_triggerCollider is SphereCollider sphere)
                {
                    sphere.radius = detectionRadius;
                    Debug.Log(
                        $"[InteractionTriggerDetector] SphereCollider 사용 - {gameObject.name}, Radius: {sphere.radius}"
                    );
                }
                else if (_triggerCollider is BoxCollider box)
                {
                    // BoxCollider도 지원
                    float size = detectionRadius * 2f;
                    box.size = new Vector3(size, size, size);
                    Debug.Log(
                        $"[InteractionTriggerDetector] BoxCollider 사용 - {gameObject.name}, Size: {box.size}"
                    );
                }
                else
                {
                    Debug.Log(
                        $"[InteractionTriggerDetector] {_triggerCollider.GetType().Name} 사용 - {gameObject.name}"
                    );
                }
            }

            // Trigger 설정
            _triggerCollider.isTrigger = true;

            // Physics Layer 설정 (나중에 최적화용)
            if (gameObject.layer == 0) // Default layer
            {
                Debug.LogWarning(
                    "[InteractionTriggerDetector] 성능 최적화를 위해 전용 레이어 설정을 권장합니다."
                );
            }

            if (enableDebugLogs)
            {
                Debug.Log(
                    $"[InteractionTriggerDetector] Trigger 초기화 완료 - 반지름: {detectionRadius}"
                );
            }
        }

        void OnTriggerEnter(Collider other)
        {
            // 레이어 마스크 체크 (상호작용 객체는 Layer 7, 다른 것들은 다른 레이어이므로 필터링)
            if (!IsInLayerMask(other.gameObject.layer, interactionLayerMask))
            {
                return;
            }

            if (enableDebugLogs)
                Debug.Log(
                    $"[InteractionTriggerDetector] 상호작용 객체 감지: {other.gameObject.name}"
                );

            // 꿈틀이인지 확인하고 MessagePipe로 처리
            var ggumtleGameObject = other.GetComponent<GgumtleGameObject>();
            if (enableDebugLogs)
            {
                Debug.Log($"[InteractionTriggerDetector] GgumtleGameObject 컴포넌트 찾기: {other.gameObject.name}, 결과: {ggumtleGameObject != null}");
                if (ggumtleGameObject == null)
                {
                    // 어떤 컴포넌트들이 있는지 확인
                    var components = other.GetComponents<Component>();
                    var componentNames = new string[components.Length];
                    for (int i = 0; i < components.Length; i++)
                    {
                        componentNames[i] = components[i] != null ? components[i].GetType().Name : "NULL";
                    }
                    Debug.Log($"[InteractionTriggerDetector] {other.gameObject.name}의 컴포넌트들: {string.Join(", ", componentNames)}");
                }
            }

            if (ggumtleGameObject != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 꿈틀이 감지: {other.gameObject.name}");
                HandleGgumtleDetection(ggumtleGameObject, other);
                return;
            }

            // 다른 상호작용 객체는 기존 방식 유지
            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null)
            {
                bool canInteract = interactable.CanInteract();
                if (canInteract)
                {
                    OnInteractableEntered?.Invoke(interactable);
                    if (enableDebugLogs)
                        Debug.Log(
                            $"[InteractionTriggerDetector] 상호작용 객체 감지: {other.gameObject.name}"
                        );
                }
            }
        }

        private void HandleGgumtleDetection(GgumtleGameObject ggumtle, Collider other)
        {
            if (_ggumtleDetectedPublisher == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning(
                        "[InteractionTriggerDetector] GgumtleDetectedPublisher가 주입되지 않음"
                    );
                return;
            }

            // MessagePipe로 감지 이벤트 발행 (상태 정보는 ViewModel에서 관리)
            var distance = Vector3.Distance(transform.position, other.transform.position);
            var message = new GgumtleDetectedMessage(
                ggumtle.GgumtleId,
                other.transform,
                distance,
                GgumtleState.Buried // ViewModel이 실제 상태를 관리하므로 기본값 전달
            );

            _ggumtleDetectedPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log(
                    $"[InteractionTriggerDetector] 꿈틀이 감지 메시지 발행: {ggumtle.GgumtleId}"
                );
        }

        void OnTriggerExit(Collider other)
        {
            // 레이어 마스크 체크
            if (!IsInLayerMask(other.gameObject.layer, interactionLayerMask))
                return;

            // 꿈틀이인지 확인하고 MessagePipe로 처리
            var ggumtleGameObject = other.GetComponent<GgumtleGameObject>();
            if (ggumtleGameObject != null)
            {
                if (enableDebugLogs)
                    Debug.Log(
                        $"[InteractionTriggerDetector] 꿈틀이 벗어남: {other.gameObject.name}"
                    );
                HandleGgumtleLeft(ggumtleGameObject);
                return;
            }

            // 다른 상호작용 객체는 기존 방식 유지
            var interactable = other.GetComponent<IInteractable>();
            if (interactable != null)
            {
                OnInteractableExited?.Invoke(interactable);
                if (enableDebugLogs)
                    Debug.Log(
                        $"[InteractionTriggerDetector] 상호작용 객체 벗어남: {other.gameObject.name}"
                    );
            }
        }

        private void HandleGgumtleLeft(GgumtleGameObject ggumtle)
        {
            if (_ggumtleLeftPublisher == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning(
                        "[InteractionTriggerDetector] GgumtleLeftPublisher가 주입되지 않음"
                    );
                return;
            }

            // MessagePipe로 벗어남 이벤트 발행
            var message = new GgumtleLeftMessage(ggumtle.GgumtleId);
            _ggumtleLeftPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log(
                    $"[InteractionTriggerDetector] 꿈틀이 벗어남 메시지 발행: {ggumtle.GgumtleId}"
                );
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
                if (_triggerCollider is CapsuleCollider capsule)
                {
                    capsule.radius = radius;
                    capsule.height = radius * 2f;
                }
                else if (_triggerCollider is SphereCollider sphere)
                {
                    sphere.radius = radius;
                }
                else if (_triggerCollider is BoxCollider box)
                {
                    float size = radius * 2f;
                    box.size = new Vector3(size, size, size);
                }
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
            var colliders = Physics.OverlapSphere(
                transform.position,
                detectionRadius,
                interactionLayerMask
            );
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
