package com.ggumtle.loadtest.protocol

/**
 * Client -> Server packet types
 */
enum class SendPacketType(val value: Short) {
    // Authentication
    VERIFY_TOKEN(1),

    // Room management
    ROOM_JOIN(10),
    ROOM_CREATE(12),

    // Dream initialization
    SCENE_CHANGE(30),

    // Dream gameplay
    PLAYER_MOVE(40),

    // Player interactions
    HIT_MONGGING(60),
    START_REVIVE(62),
    STOP_REVIVE(65),
    MONGDUNG_SKILL(67),
    ATTACK_WITH_ITEM(69),
    USE_FIELD_ITEM(71),
    USE_DEFIBRILLATOR(73),

    // Box and item interactions
    SHOW_BOX(50),
    CLOSE_BOX(52),
    TAKE_ITEM_FROM_BOX(54),
    PUT_ITEM_TO_BOX(56),

    // Ggumtle interactions
    DIG_UP_GGUMTLE(100),
    STOP_DIGGING(102),
    START_FEED(110),
    STOP_FEED(112),

    // Escape
    ESCAPE(140);

    companion object {
        private val map = entries.associateBy { it.value }
        fun fromValue(value: Short): SendPacketType? = map[value]
    }
}

/**
 * Server -> Client packet types
 */
enum class ReceivePacketType(val value: Short) {
    // Authentication
    VERIFY_TOKEN(2),

    // Room management
    ROOM_JOIN(11),
    ROOM_CREATE(13),

    // Dream initialization
    INITIALIZE_MAP(20),
    INITIALIZE_PLAYER(21),
    SCENE_CHANGE(31),
    GAME_START(35),

    // Dream gameplay
    PLAYER_MOVE_RELAY(41),
    MONGGING_STATUS(42),
    NEW_GGUMTLE(43),

    // Box and item interactions
    SHOW_BOX(51),
    CLOSE_BOX(53),
    TAKE_ITEM(55),
    PUT_ITEM(57),

    // Player interactions
    HIT(61),
    START_REVIVE(63),
    DONE_REVIVE(64),
    STOP_REVIVE(66),
    MONGDUNG_SKILL(68),
    ATTACK_WITH_ITEM(70),
    USE_FIELD_ITEM(72),
    USE_DEFIBRILLATOR(74),

    // Ggumtle interactions
    DIG_UP_RECEIVE(101),
    STOP_DIGGING(103),
    START_FEED(111),
    STOP_FEED(113),
    GGUMTLE_STATUS(120),
    LEFT_JELLY_COUNT(121),
    GGUMTLE_FED_JELLY(122),

    // Exit and escape
    OPEN_EXIT(130),
    ESCAPE_RESULT(141),

    // Game end
    END(200);

    companion object {
        private val map = entries.associateBy { it.value }
        fun fromValue(value: Short): ReceivePacketType? = map[value]
    }
}
