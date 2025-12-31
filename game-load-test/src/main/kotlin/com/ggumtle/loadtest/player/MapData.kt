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
