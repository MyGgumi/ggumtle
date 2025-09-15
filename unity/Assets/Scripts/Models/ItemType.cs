namespace Models
{
    /// <summary>
    /// 아이템 타입 분류
    /// </summary>
    public enum ItemType
    {
        /// <summary>
        /// 플레이어 인벤토리 슬롯에 저장되는 장비 아이템
        /// </summary>
        Equipment,

        /// <summary>
        /// 먹이 시스템에서 관리되는 아이템 (빛젤리)
        /// </summary>
        Feeding,

        /// <summary>
        /// 기타 아이템
        /// </summary>
        Other
    }

    /// <summary>
    /// 아이템 ID를 타입으로 분류하는 유틸리티
    /// </summary>
    public static class ItemTypeHelper
    {
        /// <summary>
        /// 아이템 ID를 기반으로 타입 반환
        /// </summary>
        public static ItemType GetItemType(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return ItemType.Other;

            return itemId.ToLower() switch
            {
                // 장비 아이템 (인벤토리 슬롯)
                "taser" or "테이저건" or "apple" => ItemType.Equipment,
                "flashbang" or "섬광탄" or "stone" => ItemType.Equipment,
                "defibrillator" or "자가제세동기" => ItemType.Equipment,

                // 먹이 아이템
                "light" or "빛젤리" => ItemType.Feeding,

                // 기타
                _ => ItemType.Other
            };
        }

        /// <summary>
        /// 장비 아이템인지 확인
        /// </summary>
        public static bool IsEquipmentItem(string itemId)
        {
            return GetItemType(itemId) == ItemType.Equipment;
        }

        /// <summary>
        /// 먹이 아이템인지 확인
        /// </summary>
        public static bool IsFeedingItem(string itemId)
        {
            return GetItemType(itemId) == ItemType.Feeding;
        }

        /// <summary>
        /// 아이템 ID를 표시용 이름으로 변환
        /// </summary>
        public static string GetDisplayName(string itemId)
        {
            if (string.IsNullOrEmpty(itemId))
                return "";

            return itemId.ToLower() switch
            {
                "taser" or "apple" => "테이저건",
                "flashbang" or "stone" => "섬광탄",
                "defibrillator" => "자가제세동기",
                "light" => "빛젤리",
                _ => itemId
            };
        }

        /// <summary>
        /// 기존 가명을 실제 아이템 ID로 변환
        /// </summary>
        public static string ConvertLegacyId(string legacyId)
        {
            if (string.IsNullOrEmpty(legacyId))
                return legacyId;

            return legacyId.ToLower() switch
            {
                "apple" => "taser",
                "stone" => "flashbang",
                _ => legacyId
            };
        }
    }
}