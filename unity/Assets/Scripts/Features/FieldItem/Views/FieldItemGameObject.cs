using Features.FieldItem.Models;
using UnityEngine;
using VContainer;

namespace Features.FieldItem.Views
{
    /// <summary>
    /// 필드 아이템 게임 오브젝트 (힐팩, 스피드팩 통합)
    /// </summary>
    public class FieldItemGameObject : MonoBehaviour
    {
        [SerializeField] private int itemId;
        [SerializeField] private FieldItemType itemType;

        private bool _isInjected = false;

        public int ItemId => itemId;
        public FieldItemType ItemType => itemType;

        /// <summary>
        /// VContainer 의존성 주입 (테스트용 간단 구현)
        /// </summary>
        [Inject]
        public void Construct()
        {
            _isInjected = true;
            Debug.Log($"[FieldItemGameObject] VContainer 의존성 주입 완료: {gameObject.name}");
        }

        public void SetId(int id)
        {
            itemId = id;
        }

        public void SetType(FieldItemType type)
        {
            itemType = type;
        }

        private void Start()
        {
            // VContainer 의존성 주입 확인
            if (!_isInjected)
            {
                Debug.LogError($"[FieldItemGameObject] VContainer 의존성 주입 실패: {gameObject.name}");
                return;
            }

            Debug.Log($"[FieldItemGameObject] 초기화 완료: {gameObject.name}, ID: {itemId}, Type: {itemType}");
        }

        private void OnTriggerEnter(Collider other)
        {
            // 플레이어가 아이템을 먹었을 때 처리 (추후 구현)
            if (other.CompareTag("Player"))
            {
                Debug.Log($"[FieldItemGameObject] 아이템 감지: ID={itemId}, Type={itemType}, Player={other.name}");
            }
        }
    }
}