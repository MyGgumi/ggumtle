package com.ggumtle.loadtest.player

/**
 * Entity position data from INITIALIZE_MAP packet
 */
data class EntityPosition(
    val id: Int,
    val x: Int,
    val y: Int,
    val z: Int
)

/**
 * Map data containing positions of all entities
 */
data class MapData(
    val boxes: List<EntityPosition> = emptyList(),
    val ggumtles: List<EntityPosition> = emptyList(),
    val healPacks: List<EntityPosition> = emptyList(),
    val speedPacks: List<EntityPosition> = emptyList()
) {
    fun findBox(id: Int): EntityPosition? = boxes.find { it.id == id }
    fun findGgumtle(id: Int): EntityPosition? = ggumtles.find { it.id == id }
    fun findHealPack(id: Int): EntityPosition? = healPacks.find { it.id == id }
    fun findSpeedPack(id: Int): EntityPosition? = speedPacks.find { it.id == id }
}

/**
 * Box contents state - 상자 내 아이템 상태 관리
 */
data class BoxState(
    val boxId: Int,
    val items: IntArray = IntArray(BOX_SIZE) { -1 }  // -1 = 빈 슬롯
) {
    companion object {
        const val BOX_SIZE = 9
    }

    fun getItem(index: Int): Int = if (index in 0 until BOX_SIZE) items[index] else -1
    fun isEmpty(index: Int): Boolean = getItem(index) == -1
    fun findFirstItem(): Int = items.indexOfFirst { it != -1 }
    fun findEmptySlot(): Int = items.indexOfFirst { it == -1 }
    fun itemCount(): Int = items.count { it != -1 }
    fun withItems(newItems: IntArray): BoxState = copy(items = newItems.copyOf())

    override fun equals(other: Any?): Boolean {
        if (this === other) return true
        if (other !is BoxState) return false
        return boxId == other.boxId && items.contentEquals(other.items)
    }

    override fun hashCode(): Int = 31 * boxId + items.contentHashCode()
}

/**
 * CLOSE_BOX 응답 결과
 */
enum class CloseBoxResult(val value: Int) {
    SUCCESS(1), FAIL(0), NOT_VIEWER(2);

    companion object {
        private val map = entries.associateBy { it.value }
        fun fromValue(value: Int): CloseBoxResult = map[value] ?: FAIL
    }
}

/**
 * TAKE_ITEM 응답 결과
 */
enum class TakeItemResult(val value: Int) {
    SUCCESS(1), FAIL(0), INDEX_OUT_OF_RANGE(2),
    NOT_FOUND_BOX(3), NOT_FOUND_PLAYER(4), NOT_MONGGING(5),
    NOT_FOUND_ITEM(10), FULL_ABOUT_ITEM(11), NOT_NEAR(12);

    companion object {
        private val map = entries.associateBy { it.value }
        fun fromValue(value: Int): TakeItemResult = map[value] ?: FAIL
    }
}

/**
 * PUT_ITEM 응답 결과
 */
enum class PutItemResult(val value: Int) {
    SUCCESS(1), FAIL(0), ILLEGAL_ITEM_ID(2),
    NOT_FOUND_BOX(3), NOT_FOUND_PLAYER(4), NOT_MONGGING(5),
    NOT_FOUND_ITEM(10), FULL_ABOUT_ITEM(11), NOT_NEAR(12);

    companion object {
        private val map = entries.associateBy { it.value }
        fun fromValue(value: Int): PutItemResult = map[value] ?: FAIL
    }
}
