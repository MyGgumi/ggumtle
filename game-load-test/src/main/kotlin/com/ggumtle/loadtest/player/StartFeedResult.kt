package com.ggumtle.loadtest.player

/**
 * Result codes for START_FEED (111) packet response
 * Matches game-server's StartFeedBody.Result enum
 */
enum class StartFeedResult(val value: Byte) {
    FAIL(0),
    START_FEEDING(1),      // 성공 - 먹이주기 시작
    NOT_FOUND_GGUMTLE(2),  // 꿈틀이를 찾을 수 없음
    NOT_FOUND_PLAYER(3),   // 플레이어를 찾을 수 없음
    YET_DIG_UP(10),        // 아직 파지 않음
    ALREADY_DONE(11),      // 이미 먹이주기 완료됨
    LACK_OF_FEED_ITEM(12), // 빛젤리 없음
    NOT_AROUND(13),        // 범위 밖
    UNKNOWN(-1);           // 알 수 없는 응답

    val isSuccess: Boolean get() = this == START_FEEDING

    companion object {
        fun fromValue(value: Byte): StartFeedResult =
            entries.find { it.value == value } ?: UNKNOWN
    }
}
