package com.ggumtle.growth.model

enum class MissionState(val value: String) {
    BEFORE_SUCCESS("BEFORE_SUCCESS"),  // 미완료
    SUCCESS("SUCCESS"),                // 완료했지만 보상 미수령
    AFTER_REWARD("AFTER_REWARD");      // 보상 수령 완료

    companion object {
        fun fromString(value: String): MissionState {
            return values().find { it.value == value } ?: BEFORE_SUCCESS
        }
    }
}