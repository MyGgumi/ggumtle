using Features.FieldItem.Models;
using UnityEngine;

namespace Features.FieldItem.Views
{
    /// <summary>
    /// 필드 아이템 게임 오브젝트 (힐팩, 스피드팩 통합)
    /// </summary>
    public class FieldItemGameObject : MonoBehaviour
    {
        [SerializeField] private int itemId;
        [SerializeField] private FieldItemType itemType;

        public int ItemId => itemId;
        public FieldItemType ItemType => itemType;

        public void SetId(int id)
        {
            itemId = id;
        }

        public void SetType(FieldItemType type)
        {
            itemType = type;
        }

        private void Awake()
        {
            // 초기화 로직
        }

        private void OnTriggerEnter(Collider other)
        {
            // 플레이어가 아이템을 먹었을 때 처리
            // 추후 구현
        }
    }
}