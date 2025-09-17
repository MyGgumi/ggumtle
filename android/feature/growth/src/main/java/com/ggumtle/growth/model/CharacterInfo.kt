package com.ggumtle.growth.model

data class CharacterInfo(
    val name: String,
    val level: Int,
    val currentStat: Float,
    val nextLevelStat: Float,
    val enhancementCost: Int,
    val successRate: Int,
    val progressRatio: Float,
    val statisticName: String = "치료속도" // 스탯 이름 (치료속도, 공격속도 등)
)