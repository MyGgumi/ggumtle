using UnityEngine;

namespace Features.Item.Models
{
    [System.Serializable]
    public class ItemDefinition
    {
        [Header("아이템 기본 정보")]
        public int ItemId;
        public string ItemName;
        public string Description;
        public Sprite ItemIcon;
        public int MaxStack = 1;

        [Header("아이템 효과")]
        public bool IsUsable = true;
        public float CooldownTime = 0f;

        public ItemDefinition(int itemId, string itemName, string description, int maxStack = 1, float cooldownTime = 0f)
        {
            ItemId = itemId;
            ItemName = itemName;
            Description = description;
            MaxStack = maxStack;
            CooldownTime = cooldownTime;
            IsUsable = true;
        }
    }
}