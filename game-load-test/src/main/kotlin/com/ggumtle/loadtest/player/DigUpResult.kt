package com.ggumtle.loadtest.player

/**
 * Result codes for DIG_UP_RECEIVE (101) packet response
 * Matches game-server's DigUpReceiveBody.Result enum
 */
enum class DigUpResult(val value: Byte) {
    FAIL(0),
    START_DIGGING(1),      // 성공 - 파기 시작
    NOT_FOUND_GGUMTLE(2),  // 꿈틀이를 찾을 수 없음
    NOT_FOUND_MONGGING(3), // 몽잉이를 찾을 수 없음
    ALREADY_DIG_UP(10),    // 이미 파는 중
    NOT_AROUND(11),        // 근처에 없음
    UNKNOWN(-1);           // 알 수 없는 응답

    val isSuccess: Boolean get() = this == START_DIGGING

    companion object {
        fun fromValue(value: Byte): DigUpResult =
            entries.find { it.value == value } ?: UNKNOWN
    }
}
