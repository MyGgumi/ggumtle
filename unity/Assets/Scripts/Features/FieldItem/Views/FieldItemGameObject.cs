using Features.FieldItem.Messages;
using Features.FieldItem.Models;
using Features.FieldItem.Services;
using MessagePipe;
using R3;
using UnityEngine;
using VContainer;

namespace Features.FieldItem.Views
{
    /// <summary>
    /// 필드 아이템 게임 오브젝트 (힐팩, 스피드팩 통합)
    /// InteractionTriggerDetector가 감지를 담당하고, 이 컴포넌트는 시각적 표현과 상태 관리만 담당
    /// </summary>
    public class FieldItemGameObject : MonoBehaviour
    {
        #region Inspector 설정

        [Header("필드 아이템 기본 설정")]
        [SerializeField] private int itemId = -1;
        [SerializeField] private FieldItemType itemType = FieldItemType.HealPack;
        [SerializeField] private string itemName = "FieldItem";

        [Header("비주얼 컴포넌트")]
        [SerializeField] private GameObject itemVisual;
        [SerializeField] private Collider itemCollider;
        [SerializeField] private ParticleSystem useEffect;

        [Header("디버그 설정")]
        [SerializeField] private bool enableDebugLogs = true;

        #endregion

        #region Public Properties

        public int ItemId => itemId;
        public FieldItemType ItemType => itemType;
        public string ItemName => itemName;
        public bool IsActive { get; private set; } = true;

        #endregion

        #region 의존성 주입 & 데이터

        private IFieldItemService _fieldItemService;
        private ISubscriber<FieldItemGlobalUsedMessage> _globalUsedSubscriber;
        private IPublisher<FieldItemRemoveRequestMessage> _removeRequestPublisher;
        private CompositeDisposable _disposables = new();
        private bool _isInjected = false;

        /// <summary>
        /// VContainer 의존성 주입
        /// </summary>
        [Inject]
        public void Construct(
            IFieldItemService fieldItemService,
            ISubscriber<FieldItemGlobalUsedMessage> globalUsedSubscriber,
            IPublisher<FieldItemRemoveRequestMessage> removeRequestPublisher)
        {
            _fieldItemService = fieldItemService;
            _globalUsedSubscriber = globalUsedSubscriber;
            _removeRequestPublisher = removeRequestPublisher;
            _isInjected = true;

            if (enableDebugLogs)
            {
                Debug.Log($"[FieldItemGameObject] VContainer 의존성 주입 완료: {gameObject.name}");
            }
        }

        /// <summary>
        /// 동적 생성 시 ID와 타입을 설정하는 메서드
        /// MapSpawnService에서 호출
        /// </summary>
        public void SetFieldItem(int id, FieldItemType type, string name = null)
        {
            itemId = id;
            itemType = type;
            if (!string.IsNullOrEmpty(name))
                itemName = name;

            // Interaction 레이어로 설정 (Layer 7)
            gameObject.layer = 7;

            if (enableDebugLogs)
                Debug.Log($"[FieldItemGameObject] 필드 아이템 설정: ID={id}, Type={type}, Name={itemName}, Layer={gameObject.layer}");
        }

        /// <summary>
        /// ID 설정 (MapSpawnService 호환용)
        /// </summary>
        public void SetId(int id)
        {
            itemId = id;

            // Interaction 레이어로 설정 (Layer 7)
            gameObject.layer = 7;

            if (enableDebugLogs)
                Debug.Log($"[FieldItemGameObject] ID 및 레이어 설정: ID={id}, Layer={gameObject.layer}");
        }

        /// <summary>
        /// 타입 설정 (MapSpawnService 호환용)
        /// </summary>
        public void SetType(FieldItemType type)
        {
            itemType = type;
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // VContainer 의존성 주입 확인
            if (!_isInjected)
            {
                Debug.LogError($"[FieldItemGameObject] VContainer 의존성 주입 실패: {gameObject.name}");
                return;
            }

            // 글로벌 사용 메시지 구독
            SubscribeToGlobalUsedMessage();

            if (enableDebugLogs)
            {
                Debug.Log($"[FieldItemGameObject] 초기화 완료: {gameObject.name}, ID: {itemId}, Type: {itemType}");
            }
        }

        private void OnDestroy()
        {
            _disposables?.Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 필드 아이템 사용 요청 (InteractionTriggerDetector에서 호출)
        /// </summary>
        public void RequestUse()
        {
            if (!IsActive)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[FieldItemGameObject] 이미 사용된 아이템: ID={itemId}");
                }
                return;
            }

            if (enableDebugLogs)
            {
                Debug.Log($"[FieldItemGameObject] 필드 아이템 사용 요청: ID={itemId}, Type={itemType}");
            }

            _fieldItemService?.UseFieldItem(itemId);
        }

        #endregion

        #region Private Methods

        private void SubscribeToGlobalUsedMessage()
        {
            _globalUsedSubscriber
                .Subscribe(OnGlobalItemUsed)
                .AddTo(_disposables);

            if (enableDebugLogs)
            {
                Debug.Log($"[FieldItemGameObject] 글로벌 사용 메시지 구독 완료: ID={itemId}");
            }
        }

        private void OnGlobalItemUsed(FieldItemGlobalUsedMessage message)
        {
            // 내 아이템이 사용된 경우에만 반응
            if (message.fieldItemId != itemId)
                return;

            if (enableDebugLogs)
            {
                Debug.Log($"[FieldItemGameObject] 글로벌 사용 메시지 수신: ID={itemId}, Success={message.success}");
            }

            // 성공/실패 관계없이 아이템을 비활성화하고 제거 요청 (서버에서 사용됨을 의미)
            DeactivateItem();
            RequestRemove();
        }

        private void DeactivateItem()
        {
            IsActive = false;

            // 모든 Renderer 컴포넌트 비활성화 (시각적으로 사라짐)
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
            }

            // 충돌체 비활성화 (중복 사용 방지)
            if (itemCollider != null)
                itemCollider.enabled = false;

            // 사용 이펙트 재생
            if (useEffect != null)
                useEffect.Play();

            if (enableDebugLogs)
            {
                Debug.Log($"[FieldItemGameObject] 필드 아이템 비활성화 (Renderer+Collider): ID={itemId}, Renderers={renderers.Length}개 비활성화");
            }
        }

        /// <summary>
        /// 필드 아이템 제거 요청 (MapSpawnService로 전달)
        /// </summary>
        private void RequestRemove()
        {
            if (_removeRequestPublisher != null)
            {
                _removeRequestPublisher.Publish(new FieldItemRemoveRequestMessage(itemId));

                if (enableDebugLogs)
                {
                    Debug.Log($"[FieldItemGameObject] 제거 요청 메시지 발행: ID={itemId}");
                }
            }
            else
            {
                Debug.LogError($"[FieldItemGameObject] RemoveRequestPublisher가 null이어서 제거 요청 실패: ID={itemId}");
            }
        }

        #endregion
    }
}