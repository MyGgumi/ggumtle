using System.Collections.Generic;
using Features.Item.Models;
using UnityEngine;

namespace Features.Item.Services
{
    [CreateAssetMenu(fileName = "ItemDefinitionService", menuName = "Game/Item Definition Service")]
    public class ItemDefinitionService : ScriptableObject
    {
        [System.Serializable]
        public class ItemEntry
        {
            [Header("기본 정보")]
            public int ItemId;
            public string ItemName;
            public Sprite ItemIcon;

            [Header("상세 설정")]
            [TextArea(2, 3)]
            public string Description;
            public int MaxStack = 1;
            public float CooldownTime = 0f;

            public ItemDefinition ToItemDefinition()
            {
                return new ItemDefinition(ItemId, ItemName, Description, MaxStack, CooldownTime)
                {
                    ItemIcon = ItemIcon
                };
            }
        }

        [Header("아이템 목록")]
        public ItemEntry[] itemEntries = new ItemEntry[]
        {
            new ItemEntry
            {
                ItemId = 3,
                ItemName = "테이저건",
                Description = "전기 충격으로 적을 기절시킵니다.",
                MaxStack = 1,
                CooldownTime = 5f
            },
            new ItemEntry
            {
                ItemId = 2,
                ItemName = "섬광탄",
                Description = "강한 빛으로 적의 시야를 차단합니다.",
                MaxStack = 3,
                CooldownTime = 3f
            },
            new ItemEntry
            {
                ItemId = 4,
                ItemName = "자가제세동기",
                Description = "기절한 상태에서 자동으로 소생시킵니다.",
                MaxStack = 1,
                CooldownTime = 10f
            },
            new ItemEntry
            {
                ItemId = 1,
                ItemName = "빛 젤리",
                Description = "꿈틀이에게 먹이를 줄 수 있는 특별한 아이템입니다.",
                MaxStack = 999,
                CooldownTime = 0f
            }
        };

        private static ItemDefinitionService _instance;
        private Dictionary<int, ItemDefinition> _itemDefinitions;

        public static ItemDefinitionService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<ItemDefinitionService>("ItemDefinitionService");
                    if (_instance == null)
                    {
                        Debug.LogError("ItemDefinitionService를 Resources 폴더에서 찾을 수 없습니다! Assets/Resources/ItemDefinitionService.asset을 생성하세요.");
                    }
                    else
                    {
                        _instance.BuildDictionary();
                    }
                }
                return _instance;
            }
        }

        private void OnEnable()
        {
            // OnEnable에서는 BuildDictionary를 호출하지 않음 (Instance에서만 호출)
        }

        private void BuildDictionary()
        {
            if (_itemDefinitions != null) return; // 이미 빌드되었으면 스킵

            _itemDefinitions = new Dictionary<int, ItemDefinition>();
            foreach (var entry in itemEntries)
            {
                _itemDefinitions[entry.ItemId] = entry.ToItemDefinition();
            }
        }

        public static ItemDefinition GetItemById(int itemId)
        {
            if (Instance == null || Instance._itemDefinitions == null) return null;
            Instance._itemDefinitions.TryGetValue(itemId, out var item);
            return item;
        }

        public static bool IsValidItemId(int itemId)
        {
            if (Instance == null || Instance._itemDefinitions == null) return false;
            return Instance._itemDefinitions.ContainsKey(itemId);
        }

        public static IReadOnlyDictionary<int, ItemDefinition> GetAllItems()
        {
            if (Instance == null || Instance._itemDefinitions == null) return new Dictionary<int, ItemDefinition>();
            return Instance._itemDefinitions;
        }

        public static string GetItemName(int itemId)
        {
            var item = GetItemById(itemId);
            return item?.ItemName ?? "Unknown Item";
        }

        public static int GetMaxStack(int itemId)
        {
            var item = GetItemById(itemId);
            return item?.MaxStack ?? 1;
        }
    }
}