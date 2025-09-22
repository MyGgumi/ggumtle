package com.ggumtle.growth.model

import com.ggumtle.domain.model.MonggingClass
import com.ggumtle.domain.rest.model.growth.response.MonggingDetailResponse

data class CharacterInfo(
    val id: Long,
    val monggingClass: MonggingClass,
    val nowLevel: Int,
    val nowPercentage: Double,
    val isMaxLevel: Boolean,
    val afterLevel: Int? = null,
    val afterPercentage: Double? = null,
    val needCoin: Int? = null,
    val successPercentage: Int? = null
)

fun MonggingDetailResponse.toCharacterInfo(): CharacterInfo {
    return CharacterInfo(
        id = this.id,
        monggingClass = MonggingClass.fromType(this.monggingClass),
        nowLevel = this.nowLevel,
        nowPercentage = this.nowPercentage,
        isMaxLevel = this.isMaxLevel,
        afterLevel = this.afterLevel,
        afterPercentage = this.afterPercentage,
        needCoin = this.needCoin,
        successPercentage = this.successPercentage
    )
}