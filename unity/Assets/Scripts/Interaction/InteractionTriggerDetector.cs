using System;
using System.Collections.Generic;
using System.Linq;
using Config;
using Features.Ggumtle.Messages;
using Features.Ggumtle.Models;
using Features.Ggumtle.Views;
using Features.Chest.Messages;
using Features.Chest.Models;
using Features.Chest.Views;
using Features.Revival.Messages;
using Features.Revival.Views;
using Features.EscapeGate.Messages;
using Features.EscapeGate.Views;
using Features.Player.Services;
using Features.PlayerList.Models;
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
        private IPublisher<Features.Ggumtle.Messages.GgumtleDetectedMessage> _ggumtleDetectedPublisher;
        private IPublisher<Features.Ggumtle.Messages.GgumtleLeftMessage> _ggumtleLeftPublisher;
        private IPublisher<Features.Chest.Messages.ChestDetectedMessage> _chestDetectedPublisher;
        private IPublisher<Features.Chest.Messages.ChestLeftMessage> _chestLeftPublisher;
        private IPublisher<Features.Revival.Messages.FaintedMonggingDetectedMessage> _faintedMonggingDetectedPublisher;
        private IPublisher<Features.Revival.Messages.FaintedMonggingLeftMessage> _faintedMonggingLeftPublisher;
        private IPublisher<Features.EscapeGate.Messages.EscapeGateDetectedMessage> _escapeGateDetectedPublisher;
        private IPublisher<Features.EscapeGate.Messages.EscapeGateLeftMessage> _escapeGateLeftPublisher;

        // 플레이어 관리 서비스
        private PlayerManagerService _playerManagerService;

        [Inject]
        public void Construct(
            PlayerManagerService playerManagerService,
            IPublisher<GgumtleDetectedMessage> ggumtleDetectedPublisher,
            IPublisher<GgumtleLeftMessage> ggumtleLeftPublisher,
            IPublisher<ChestDetectedMessage> chestDetectedPublisher,
            IPublisher<ChestLeftMessage> chestLeftPublisher,
            IPublisher<FaintedMonggingDetectedMessage> faintedMonggingDetectedPublisher,
            IPublisher<FaintedMonggingLeftMessage> faintedMonggingLeftPublisher,
            IPublisher<EscapeGateDetectedMessage> escapeGateDetectedPublisher,
            IPublisher<EscapeGateLeftMessage> escapeGateLeftPublisher
        )
        {
            _playerManagerService = playerManagerService;
            _ggumtleDetectedPublisher = ggumtleDetectedPublisher;
            _ggumtleLeftPublisher = ggumtleLeftPublisher;
            _chestDetectedPublisher = chestDetectedPublisher;
            _chestLeftPublisher = chestLeftPublisher;
            _faintedMonggingDetectedPublisher = faintedMonggingDetectedPublisher;
            _faintedMonggingLeftPublisher = faintedMonggingLeftPublisher;
            _escapeGateDetectedPublisher = escapeGateDetectedPublisher;
            _escapeGateLeftPublisher = escapeGateLeftPublisher;
            Debug.Log($"[InteractionTriggerDetector] VContainer 의존성 주입 완료 - {gameObject.name}");
        }

        // 기존 C# Event도 호환성을 위해 유지 (다른 상호작용 객체용)
        public event Action<IInteractable> OnInteractableEntered;
        public event Action<IInteractable> OnInteractableExited;

        private Collider _triggerCollider; // SphereCollider 또는 CapsuleCollider 모두 지원

        // 현재 감지된 꿈틀이들을 거리순으로 관리
        private readonly List<(GgumtleGameObject ggumtle, Collider collider, float distance)> _detectedGgumtles = new();
        private GgumtleGameObject _currentClosestGgumtle = null;

        // 현재 감지된 상자들을 거리순으로 관리
        private readonly List<(ChestGameObject chest, Collider collider, float distance)> _detectedChests = new();
        private ChestGameObject _currentClosestChest = null;

        // 현재 감지된 기절한 몽깅이들을 거리순으로 관리
        private readonly List<(FaintedMonggingInteractable faintedMongging, Collider collider, float distance)> _detectedFaintedMonggings = new();
        private FaintedMonggingInteractable _currentClosestFaintedMongging = null;

        // 현재 감지된 탈출 게이트들을 거리순으로 관리
        private readonly List<(EscapeGateGameObject escapeGate, Collider collider, float distance)> _detectedEscapeGates = new();
        private EscapeGateGameObject _currentClosestEscapeGate = null;

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

            // 거리 업데이트를 위한 주기적 검사 시작
            InvokeRepeating(nameof(UpdateDetectedGgumtleDistances), 0.1f, 0.1f);
        }

        private System.Collections.IEnumerator CheckDependenciesAfterFrame()
        {
            yield return null; // 한 프레임 대기

            // VContainer 주입 확인
            bool ggumtlePublishersOK = _ggumtleDetectedPublisher != null && _ggumtleLeftPublisher != null;
            bool chestPublishersOK = _chestDetectedPublisher != null && _chestLeftPublisher != null;
            bool faintedMonggingPublishersOK = _faintedMonggingDetectedPublisher != null && _faintedMonggingLeftPublisher != null;
            bool escapeGatePublishersOK = _escapeGateDetectedPublisher != null && _escapeGateLeftPublisher != null;

            if (ggumtlePublishersOK && chestPublishersOK && faintedMonggingPublishersOK && escapeGatePublishersOK)
            {
                Debug.Log(
                    $"[InteractionTriggerDetector] VContainer MessagePipe 주입 성공 - {gameObject.name}"
                );
            }
            else
            {
                Debug.LogError(
                    $"[InteractionTriggerDetector] VContainer MessagePipe 주입 실패! " +
                    $"GgumtleDetected: {_ggumtleDetectedPublisher != null}, GgumtleLeft: {_ggumtleLeftPublisher != null}, " +
                    $"ChestDetected: {_chestDetectedPublisher != null}, ChestLeft: {_chestLeftPublisher != null}, " +
                    $"FaintedMonggingDetected: {_faintedMonggingDetectedPublisher != null}, FaintedMonggingLeft: {_faintedMonggingLeftPublisher != null}, " +
                    $"EscapeGateDetected: {_escapeGateDetectedPublisher != null}, EscapeGateLeft: {_escapeGateLeftPublisher != null} - {gameObject.name}"
                );
            }

            // 최종 확인
            if (_ggumtleDetectedPublisher == null)
            {
                Debug.LogError(
                    $"[InteractionTriggerDetector] GgumtleDetectedPublisher가 주입되지 않음! VContainer 등록을 확인하세요. - {gameObject.name}"
                );
            }
            if (_ggumtleLeftPublisher == null)
            {
                Debug.LogError(
                    $"[InteractionTriggerDetector] GgumtleLeftPublisher가 주입되지 않음! VContainer 등록을 확인하세요. - {gameObject.name}"
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
            // 로컬 몽깅이 플레이어만 상호작용 감지 활성화
            if (!IsLocalMonggingPlayer())
            {
                return;
            }

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
            if (ggumtleGameObject != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 꿈틀이 감지: {other.gameObject.name}");
                AddGgumtleToDetectionList(ggumtleGameObject, other);
                return;
            }

            // 상자인지 확인하고 MessagePipe로 처리
            var chestGameObject = other.GetComponent<ChestGameObject>();
            if (chestGameObject != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 상자 감지: {other.gameObject.name}");
                AddChestToDetectionList(chestGameObject, other);
                return;
            }

            // 기절한 몽깅이인지 확인하고 MessagePipe로 처리
            var faintedMonggingInteractable = other.GetComponent<FaintedMonggingInteractable>();
            if (faintedMonggingInteractable != null && faintedMonggingInteractable.enabled)
            {
                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 기절한 몽깅이 감지: {other.gameObject.name}");
                AddFaintedMonggingToDetectionList(faintedMonggingInteractable, other);
                return;
            }

            // 탈출 게이트인지 확인하고 MessagePipe로 처리
            var escapeGateGameObject = other.GetComponent<EscapeGateGameObject>();
            if (escapeGateGameObject != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 탈출 게이트 감지: {other.gameObject.name}");
                AddEscapeGateToDetectionList(escapeGateGameObject, other);
                return;
            }

            if (enableDebugLogs)
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

        /// <summary>
        /// 꿈틀이를 감지 리스트에 추가하고 가장 가까운 꿈틀이 업데이트
        /// </summary>
        private void AddGgumtleToDetectionList(GgumtleGameObject ggumtle, Collider other)
        {
            var distance = Vector3.Distance(transform.position, other.transform.position);

            // 이미 리스트에 있는지 확인
            var existingIndex = _detectedGgumtles.FindIndex(g => g.ggumtle == ggumtle);
            if (existingIndex >= 0)
            {
                // 거리만 업데이트
                _detectedGgumtles[existingIndex] = (ggumtle, other, distance);
            }
            else
            {
                // 새로 추가
                _detectedGgumtles.Add((ggumtle, other, distance));
            }

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 꿈틀이 감지 리스트에 추가: {ggumtle.GgumtleId}, 거리: {distance:F2}");

            UpdateClosestGgumtle();
        }

        void OnTriggerExit(Collider other)
        {
            // 로컬 몽깅이 플레이어만 상호작용 감지 활성화
            if (!IsLocalMonggingPlayer())
            {
                return;
            }

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
                RemoveGgumtleFromDetectionList(ggumtleGameObject);
                return;
            }

            // 상자인지 확인하고 MessagePipe로 처리
            var chestGameObject = other.GetComponent<ChestGameObject>();
            if (chestGameObject != null)
            {
                if (enableDebugLogs)
                    Debug.Log(
                        $"[InteractionTriggerDetector] 상자 벗어남: {other.gameObject.name}"
                    );
                RemoveChestFromDetectionList(chestGameObject);
                return;
            }

            // 기절한 몽깅이인지 확인하고 MessagePipe로 처리
            var faintedMonggingInteractable = other.GetComponent<FaintedMonggingInteractable>();
            if (faintedMonggingInteractable != null)
            {
                if (enableDebugLogs)
                    Debug.Log(
                        $"[InteractionTriggerDetector] 기절한 몽깅이 벗어남: {other.gameObject.name}"
                    );
                RemoveFaintedMonggingFromDetectionList(faintedMonggingInteractable);
                return;
            }

            // 탈출 게이트인지 확인하고 MessagePipe로 처리
            var escapeGateGameObject = other.GetComponent<EscapeGateGameObject>();
            if (escapeGateGameObject != null)
            {
                if (enableDebugLogs)
                    Debug.Log(
                        $"[InteractionTriggerDetector] 탈출 게이트 벗어남: {other.gameObject.name}"
                    );
                RemoveEscapeGateFromDetectionList(escapeGateGameObject);
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

        /// <summary>
        /// 꿈틀이를 감지 리스트에서 제거하고 가장 가까운 꿈틀이 업데이트
        /// </summary>
        private void RemoveGgumtleFromDetectionList(GgumtleGameObject ggumtle)
        {
            var removed = _detectedGgumtles.RemoveAll(g => g.ggumtle == ggumtle);

            if (removed > 0)
            {
                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 꿈틀이 감지 리스트에서 제거: {ggumtle.GgumtleId}");

                UpdateClosestGgumtle();
            }
        }

        /// <summary>
        /// 가장 가까운 꿈틀이 업데이트 및 메시지 발행
        /// </summary>
        private void UpdateClosestGgumtle()
        {
            GgumtleGameObject newClosest = null;

            if (_detectedGgumtles.Count > 0)
            {
                // 거리순으로 정렬하여 가장 가까운 꿈틀이 찾기
                var closest = _detectedGgumtles.OrderBy(g => g.distance).First();
                newClosest = closest.ggumtle;

                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 가장 가까운 꿈틀이: {newClosest.GgumtleId}, 거리: {closest.distance:F2}");
            }

            // 가장 가까운 꿈틀이가 변경되었는지 확인
            if (_currentClosestGgumtle != newClosest)
            {
                // 이전 꿈틀이가 있었다면 Left 메시지 발행
                if (_currentClosestGgumtle != null)
                {
                    PublishGgumtleLeftMessage(_currentClosestGgumtle);
                }

                // 새로운 꿈틀이가 있다면 Detected 메시지 발행
                if (newClosest != null)
                {
                    var closestInfo = _detectedGgumtles.First(g => g.ggumtle == newClosest);
                    PublishGgumtleDetectedMessage(newClosest, closestInfo.collider, closestInfo.distance);
                }

                _currentClosestGgumtle = newClosest;
            }
        }

        /// <summary>
        /// 꿈틀이 감지 메시지 발행
        /// </summary>
        private void PublishGgumtleDetectedMessage(GgumtleGameObject ggumtle, Collider collider, float distance)
        {
            if (_ggumtleDetectedPublisher == null)
            {
                Debug.LogError("[InteractionTriggerDetector] GgumtleDetectedPublisher가 주입되지 않아 메시지를 발행할 수 없습니다!");
                return;
            }

            // 꿈틀이의 현재 상태를 가져옴
            var currentState = ggumtle.GetCurrentState();

            var message = new GgumtleDetectedMessage(
                ggumtle.GgumtleId.ToString(),
                collider.transform,
                distance,
                currentState // 실제 꿈틀이 상태 전달
            );

            _ggumtleDetectedPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 꿈틀이 감지 메시지 발행: {ggumtle.GgumtleId}");
        }

        /// <summary>
        /// 꿈틀이 벗어남 메시지 발행
        /// </summary>
        private void PublishGgumtleLeftMessage(GgumtleGameObject ggumtle)
        {
            if (_ggumtleLeftPublisher == null)
            {
                Debug.LogError("[InteractionTriggerDetector] GgumtleLeftPublisher가 주입되지 않아 메시지를 발행할 수 없습니다!");
                return;
            }

            var message = new GgumtleLeftMessage(ggumtle.GgumtleId.ToString());
            _ggumtleLeftPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 꿈틀이 벗어남 메시지 발행: {ggumtle.GgumtleId}");
        }

        /// <summary>
        /// 상자를 감지 리스트에 추가하고 가장 가까운 상자 업데이트
        /// </summary>
        private void AddChestToDetectionList(ChestGameObject chest, Collider other)
        {
            var distance = Vector3.Distance(transform.position, other.transform.position);

            // 이미 리스트에 있는지 확인
            var existingIndex = _detectedChests.FindIndex(c => c.chest == chest);
            if (existingIndex >= 0)
            {
                // 거리만 업데이트
                _detectedChests[existingIndex] = (chest, other, distance);
            }
            else
            {
                // 새로 추가
                _detectedChests.Add((chest, other, distance));
            }

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 상자 감지 리스트에 추가: {chest.ChestId}, 거리: {distance:F2}");

            UpdateClosestChest();
        }

        /// <summary>
        /// 상자를 감지 리스트에서 제거하고 가장 가까운 상자 업데이트
        /// </summary>
        private void RemoveChestFromDetectionList(ChestGameObject chest)
        {
            var removed = _detectedChests.RemoveAll(c => c.chest == chest);

            if (removed > 0)
            {
                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 상자 감지 리스트에서 제거: {chest.ChestId}");

                UpdateClosestChest();
            }
        }

        /// <summary>
        /// 가장 가까운 상자 업데이트 및 메시지 발행
        /// </summary>
        private void UpdateClosestChest()
        {
            ChestGameObject newClosest = null;

            if (_detectedChests.Count > 0)
            {
                // 거리순으로 정렬하여 가장 가까운 상자 찾기
                var closest = _detectedChests.OrderBy(c => c.distance).First();
                newClosest = closest.chest;

                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 가장 가까운 상자: {newClosest.ChestId}, 거리: {closest.distance:F2}");
            }

            // 가장 가까운 상자가 변경되었는지 확인
            if (_currentClosestChest != newClosest)
            {
                // 이전 상자가 있었다면 Left 메시지 발행
                if (_currentClosestChest != null)
                {
                    PublishChestLeftMessage(_currentClosestChest);
                }

                // 새로운 상자가 있다면 Detected 메시지 발행
                if (newClosest != null)
                {
                    var closestInfo = _detectedChests.First(c => c.chest == newClosest);
                    PublishChestDetectedMessage(newClosest, closestInfo.collider, closestInfo.distance);
                }

                _currentClosestChest = newClosest;
            }
        }

        /// <summary>
        /// 상자 감지 메시지 발행
        /// </summary>
        private void PublishChestDetectedMessage(ChestGameObject chest, Collider collider, float distance)
        {
            if (_chestDetectedPublisher == null)
            {
                Debug.LogError("[InteractionTriggerDetector] ChestDetectedPublisher가 주입되지 않아 메시지를 발행할 수 없습니다!");
                return;
            }

            var message = new ChestDetectedMessage(
                chest.ChestId,
                collider.transform,
                distance,
                ChestState.Closed // ViewModel이 실제 상태를 관리하므로 기본값 전달
            );

            _chestDetectedPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 상자 감지 메시지 발행: {chest.ChestId}");
        }

        /// <summary>
        /// 상자 벗어남 메시지 발행
        /// </summary>
        private void PublishChestLeftMessage(ChestGameObject chest)
        {
            if (_chestLeftPublisher == null)
            {
                Debug.LogError("[InteractionTriggerDetector] ChestLeftPublisher가 주입되지 않아 메시지를 발행할 수 없습니다!");
                return;
            }

            var message = new ChestLeftMessage(chest.ChestId);
            _chestLeftPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 상자 벗어남 메시지 발행: {chest.ChestId}");
        }

        /// <summary>
        /// 감지된 꿈틀이들, 상자들, 기절한 몽깅이들의 거리를 주기적으로 업데이트
        /// </summary>
        private void UpdateDetectedGgumtleDistances()
        {
            bool distanceChanged = false;

            // 꿈틀이들의 거리 업데이트
            if (_detectedGgumtles.Count > 0)
            {
                for (int i = 0; i < _detectedGgumtles.Count; i++)
                {
                    var (ggumtle, collider, oldDistance) = _detectedGgumtles[i];

                    if (ggumtle == null || collider == null)
                    {
                        // 무효한 참조는 제거
                        _detectedGgumtles.RemoveAt(i);
                        i--;
                        distanceChanged = true;
                        continue;
                    }

                    var newDistance = Vector3.Distance(transform.position, collider.transform.position);

                    // 거리 변화가 0.1f 이상일 때만 업데이트 (노이즈 방지)
                    if (Mathf.Abs(newDistance - oldDistance) > 0.1f)
                    {
                        _detectedGgumtles[i] = (ggumtle, collider, newDistance);
                        distanceChanged = true;
                    }
                }

                // 거리가 변경되었다면 가장 가까운 꿈틀이 재평가
                if (distanceChanged)
                {
                    UpdateClosestGgumtle();
                }
            }

            // 상자들의 거리 업데이트
            distanceChanged = false;
            if (_detectedChests.Count > 0)
            {
                for (int i = 0; i < _detectedChests.Count; i++)
                {
                    var (chest, collider, oldDistance) = _detectedChests[i];

                    if (chest == null || collider == null)
                    {
                        // 무효한 참조는 제거
                        _detectedChests.RemoveAt(i);
                        i--;
                        distanceChanged = true;
                        continue;
                    }

                    var newDistance = Vector3.Distance(transform.position, collider.transform.position);

                    // 거리 변화가 0.1f 이상일 때만 업데이트 (노이즈 방지)
                    if (Mathf.Abs(newDistance - oldDistance) > 0.1f)
                    {
                        _detectedChests[i] = (chest, collider, newDistance);
                        distanceChanged = true;
                    }
                }

                // 거리가 변경되었다면 가장 가까운 상자 재평가
                if (distanceChanged)
                {
                    UpdateClosestChest();
                }
            }

            // 기절한 몽깅이들의 거리 업데이트
            distanceChanged = false;
            if (_detectedFaintedMonggings.Count > 0)
            {
                for (int i = 0; i < _detectedFaintedMonggings.Count; i++)
                {
                    var (faintedMongging, collider, oldDistance) = _detectedFaintedMonggings[i];

                    if (faintedMongging == null || collider == null)
                    {
                        // 무효한 참조는 제거
                        _detectedFaintedMonggings.RemoveAt(i);
                        i--;
                        distanceChanged = true;
                        continue;
                    }

                    var newDistance = Vector3.Distance(transform.position, collider.transform.position);

                    // 거리 변화가 0.1f 이상일 때만 업데이트 (노이즈 방지)
                    if (Mathf.Abs(newDistance - oldDistance) > 0.1f)
                    {
                        _detectedFaintedMonggings[i] = (faintedMongging, collider, newDistance);
                        distanceChanged = true;
                    }
                }

                // 거리가 변경되었다면 가장 가까운 기절한 몽깅이 재평가
                if (distanceChanged)
                {
                    UpdateClosestFaintedMongging();
                }
            }

            // 탈출 게이트들의 거리 업데이트
            distanceChanged = false;
            if (_detectedEscapeGates.Count > 0)
            {
                for (int i = 0; i < _detectedEscapeGates.Count; i++)
                {
                    var (escapeGate, collider, oldDistance) = _detectedEscapeGates[i];

                    if (escapeGate == null || collider == null)
                    {
                        // 무효한 참조는 제거
                        _detectedEscapeGates.RemoveAt(i);
                        i--;
                        distanceChanged = true;
                        continue;
                    }

                    var newDistance = Vector3.Distance(transform.position, collider.transform.position);

                    // 거리 변화가 0.1f 이상일 때만 업데이트 (노이즈 방지)
                    if (Mathf.Abs(newDistance - oldDistance) > 0.1f)
                    {
                        _detectedEscapeGates[i] = (escapeGate, collider, newDistance);
                        distanceChanged = true;
                    }
                }

                // 거리가 변경되었다면 가장 가까운 탈출 게이트 재평가
                if (distanceChanged)
                {
                    UpdateClosestEscapeGate();
                }
            }
        }

        private bool IsInLayerMask(int layer, LayerMask layerMask)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }

        /// <summary>
        /// 로컬 몽깅이 플레이어인지 확인
        /// </summary>
        private bool IsLocalMonggingPlayer()
        {
            if (_playerManagerService == null)
            {
                if (enableDebugLogs)
                    Debug.LogWarning("[InteractionTriggerDetector] PlayerManagerService가 주입되지 않았습니다");
                return true; // 기본값으로 활성화 (기존 동작 유지)
            }

            var localPlayerRole = _playerManagerService.GetLocalPlayerRole();
            bool isLocalMongging = localPlayerRole == PlayerRole.Mongging;

            if (enableDebugLogs && !isLocalMongging)
                Debug.Log($"[InteractionTriggerDetector] 몽둥이 플레이어이므로 상호작용 감지 비활성화");

            return isLocalMongging;
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

        /// <summary>
        /// 기절한 몽깅이를 감지 리스트에 추가하고 가장 가까운 기절한 몽깅이 업데이트
        /// </summary>
        private void AddFaintedMonggingToDetectionList(FaintedMonggingInteractable faintedMongging, Collider other)
        {
            var distance = Vector3.Distance(transform.position, other.transform.position);

            // 이미 리스트에 있는지 확인
            var existingIndex = _detectedFaintedMonggings.FindIndex(f => f.faintedMongging == faintedMongging);
            if (existingIndex >= 0)
            {
                // 거리만 업데이트
                _detectedFaintedMonggings[existingIndex] = (faintedMongging, other, distance);
            }
            else
            {
                // 새로 추가
                _detectedFaintedMonggings.Add((faintedMongging, other, distance));
            }

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 기절한 몽깅이 감지 리스트에 추가: {faintedMongging.PlayerId}, 거리: {distance:F2}");

            UpdateClosestFaintedMongging();
        }

        /// <summary>
        /// 기절한 몽깅이를 감지 리스트에서 제거하고 가장 가까운 기절한 몽깅이 업데이트
        /// </summary>
        private void RemoveFaintedMonggingFromDetectionList(FaintedMonggingInteractable faintedMongging)
        {
            var removed = _detectedFaintedMonggings.RemoveAll(f => f.faintedMongging == faintedMongging);

            if (removed > 0)
            {
                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 기절한 몽깅이 감지 리스트에서 제거: {faintedMongging.PlayerId}");

                UpdateClosestFaintedMongging();
            }
        }

        /// <summary>
        /// 가장 가까운 기절한 몽깅이 업데이트 및 메시지 발행
        /// </summary>
        private void UpdateClosestFaintedMongging()
        {
            FaintedMonggingInteractable newClosest = null;

            if (_detectedFaintedMonggings.Count > 0)
            {
                // 거리순으로 정렬하여 가장 가까운 기절한 몽깅이 찾기
                var closest = _detectedFaintedMonggings.OrderBy(f => f.distance).First();
                newClosest = closest.faintedMongging;

                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 가장 가까운 기절한 몽깅이: {newClosest.PlayerId}, 거리: {closest.distance:F2}");
            }

            // 가장 가까운 기절한 몽깅이가 변경되었는지 확인
            if (_currentClosestFaintedMongging != newClosest)
            {
                // 이전 기절한 몽깅이가 있었다면 Left 메시지 발행
                if (_currentClosestFaintedMongging != null)
                {
                    PublishFaintedMonggingLeftMessage(_currentClosestFaintedMongging);
                }

                // 새로운 기절한 몽깅이가 있다면 Detected 메시지 발행
                if (newClosest != null)
                {
                    var closestInfo = _detectedFaintedMonggings.First(f => f.faintedMongging == newClosest);
                    PublishFaintedMonggingDetectedMessage(newClosest, closestInfo.collider, closestInfo.distance);
                }

                _currentClosestFaintedMongging = newClosest;
            }
        }

        /// <summary>
        /// 기절한 몽깅이 감지 메시지 발행
        /// </summary>
        private void PublishFaintedMonggingDetectedMessage(FaintedMonggingInteractable faintedMongging, Collider collider, float distance)
        {
            if (_faintedMonggingDetectedPublisher == null)
            {
                Debug.LogError("[InteractionTriggerDetector] FaintedMonggingDetectedPublisher가 주입되지 않아 메시지를 발행할 수 없습니다!");
                return;
            }

            var message = new FaintedMonggingDetectedMessage(
                faintedMongging.PlayerId,
                collider.transform,
                distance,
                faintedMongging.PlayerName ?? "Unknown"
            );

            _faintedMonggingDetectedPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 기절한 몽깅이 감지 메시지 발행: {faintedMongging.PlayerId}");
        }

        /// <summary>
        /// 기절한 몽깅이 벗어남 메시지 발행
        /// </summary>
        private void PublishFaintedMonggingLeftMessage(FaintedMonggingInteractable faintedMongging)
        {
            if (_faintedMonggingLeftPublisher == null)
            {
                Debug.LogError("[InteractionTriggerDetector] FaintedMonggingLeftPublisher가 주입되지 않아 메시지를 발행할 수 없습니다!");
                return;
            }

            var message = new FaintedMonggingLeftMessage(faintedMongging.PlayerId);
            _faintedMonggingLeftPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 기절한 몽깅이 벗어남 메시지 발행: {faintedMongging.PlayerId}");
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

        /// <summary>
        /// 탈출 게이트를 감지 리스트에 추가하고 가장 가까운 탈출 게이트 업데이트
        /// </summary>
        private void AddEscapeGateToDetectionList(EscapeGateGameObject escapeGate, Collider other)
        {
            var distance = Vector3.Distance(transform.position, other.transform.position);

            // 이미 리스트에 있는지 확인
            var existingIndex = _detectedEscapeGates.FindIndex(e => e.escapeGate == escapeGate);
            if (existingIndex >= 0)
            {
                // 거리만 업데이트
                _detectedEscapeGates[existingIndex] = (escapeGate, other, distance);
            }
            else
            {
                // 새로 추가
                _detectedEscapeGates.Add((escapeGate, other, distance));
            }

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 탈출 게이트 감지 리스트에 추가: {escapeGate.GateId}, 거리: {distance:F2}");

            UpdateClosestEscapeGate();
        }

        /// <summary>
        /// 탈출 게이트를 감지 리스트에서 제거하고 가장 가까운 탈출 게이트 업데이트
        /// </summary>
        private void RemoveEscapeGateFromDetectionList(EscapeGateGameObject escapeGate)
        {
            var removed = _detectedEscapeGates.RemoveAll(e => e.escapeGate == escapeGate);

            if (removed > 0)
            {
                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 탈출 게이트 감지 리스트에서 제거: {escapeGate.GateId}");

                UpdateClosestEscapeGate();
            }
        }

        /// <summary>
        /// 가장 가까운 탈출 게이트 업데이트 및 메시지 발행
        /// </summary>
        private void UpdateClosestEscapeGate()
        {
            EscapeGateGameObject newClosest = null;

            if (_detectedEscapeGates.Count > 0)
            {
                // 거리순으로 정렬하여 가장 가까운 탈출 게이트 찾기
                var closest = _detectedEscapeGates.OrderBy(e => e.distance).First();
                newClosest = closest.escapeGate;

                if (enableDebugLogs)
                    Debug.Log($"[InteractionTriggerDetector] 가장 가까운 탈출 게이트: {newClosest.GateId}, 거리: {closest.distance:F2}");
            }

            // 가장 가까운 탈출 게이트가 변경되었는지 확인
            if (_currentClosestEscapeGate != newClosest)
            {
                // 이전 탈출 게이트가 있었다면 Left 메시지 발행
                if (_currentClosestEscapeGate != null)
                {
                    PublishEscapeGateLeftMessage(_currentClosestEscapeGate);
                }

                // 새로운 탈출 게이트가 있다면 Detected 메시지 발행
                if (newClosest != null)
                {
                    var closestInfo = _detectedEscapeGates.First(e => e.escapeGate == newClosest);
                    PublishEscapeGateDetectedMessage(newClosest, closestInfo.collider, closestInfo.distance);
                }

                _currentClosestEscapeGate = newClosest;
            }
        }

        /// <summary>
        /// 탈출 게이트 감지 메시지 발행
        /// </summary>
        private void PublishEscapeGateDetectedMessage(EscapeGateGameObject escapeGate, Collider collider, float distance)
        {
            if (_escapeGateDetectedPublisher == null)
            {
                Debug.LogError("[InteractionTriggerDetector] EscapeGateDetectedPublisher가 주입되지 않아 메시지를 발행할 수 없습니다!");
                return;
            }

            var message = new EscapeGateDetectedMessage(
                escapeGate.GateId,
                collider.transform,
                distance,
                Features.EscapeGate.Models.EscapeGateState.Inactive // 기본값, Service에서 실제 상태 관리
            );

            _escapeGateDetectedPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 탈출 게이트 감지 메시지 발행: {escapeGate.GateId}");
        }

        /// <summary>
        /// 탈출 게이트 벗어남 메시지 발행
        /// </summary>
        private void PublishEscapeGateLeftMessage(EscapeGateGameObject escapeGate)
        {
            if (_escapeGateLeftPublisher == null)
            {
                Debug.LogError("[InteractionTriggerDetector] EscapeGateLeftPublisher가 주입되지 않아 메시지를 발행할 수 없습니다!");
                return;
            }

            var message = new EscapeGateLeftMessage(escapeGate.GateId);
            _escapeGateLeftPublisher.Publish(message);

            if (enableDebugLogs)
                Debug.Log($"[InteractionTriggerDetector] 탈출 게이트 벗어남 메시지 발행: {escapeGate.GateId}");
        }

        #region Public Properties

        public float DetectionRadius => detectionRadius;
        public LayerMask InteractionLayerMask => interactionLayerMask;
        public bool IsDebugEnabled => enableDebugLogs;

        #endregion
    }
}
