package com.ggumtle.mission.model

data class EmoticonItem(
    val name: String,
    val emoji: String,
    val animationIndex: Int
)

object EmoticonData {
    val emoticons = listOf(
        EmoticonItem("만세", "🙌", 0),
        EmoticonItem("die", "💀", 4),
        EmoticonItem("돌진", "💨", 6),
        EmoticonItem("맞기", "💥", 8)
    )
}