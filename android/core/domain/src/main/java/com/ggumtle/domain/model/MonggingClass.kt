package com.ggumtle.domain.model

enum class MonggingClass(val type: String, val displayName: String) {
    HP("physical", "탱커 몽깅이"),
    JOB("work", "워커 몽깅이"),
    HEAL("heal", "힐러 몽깅이");

    companion object {
        fun fromType(type: String): MonggingClass {
            return entries.find { it.type == type }
                ?: throw IllegalArgumentException("Unknown monggling type: $type")
        }
    }
}