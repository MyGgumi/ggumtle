using System.Collections.Generic;
using Features.Item.Models;
using UnityEngine;

namespace Features.Item.Services
{
    public static class ItemDefinitionService
    {
        private static readonly Dictionary<int, ItemDefinition> _itemDefinitions = new();

        static ItemDefinitionService()
        {
            InitializeItemDefinitions();
        }

        private static void InitializeItemDefinitions()
        {
            _itemDefinitions.Clear();

            // 테이저건 (ID: 1)
            _itemDefinitions[1] = new ItemDefinition(
                itemId: 1,
                itemName: "테이저건",
                description: "전기 충격으로 적을 기절시킵니다.",
                maxStack: 1,
                cooldownTime: 5f
            );

            // 섬광탄 (ID: 2)
            _itemDefinitions[2] = new ItemDefinition(
                itemId: 2,
                itemName: "섬광탄",
                description: "강한 빛으로 적의 시야를 차단합니다.",
                maxStack: 3,
                cooldownTime: 3f
            );

            // 자가제세동기 (ID: 3)
            _itemDefinitions[3] = new ItemDefinition(
                itemId: 3,
                itemName: "자가제세동기",
                description: "기절한 상태에서 자동으로 소생시킵니다.",
                maxStack: 1,
                cooldownTime: 10f
            );

            // 빛 젤리 (ID: 4) - 꿈틀이 먹이용
            _itemDefinitions[4] = new ItemDefinition(
                itemId: 4,
                itemName: "빛 젤리",
                description: "꿈틀이에게 먹이를 줄 수 있는 특별한 아이템입니다.",
                maxStack: 999,
                cooldownTime: 0f
            );
        }

        public static ItemDefinition GetItemById(int itemId)
        {
            _itemDefinitions.TryGetValue(itemId, out var item);
            return item;
        }

        public static bool IsValidItemId(int itemId)
        {
            return _itemDefinitions.ContainsKey(itemId);
        }

        public static IReadOnlyDictionary<int, ItemDefinition> GetAllItems()
        {
            return _itemDefinitions;
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