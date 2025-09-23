package com.ggumtle.mission.manager

import android.util.Log
import androidx.compose.ui.geometry.Offset
import javax.inject.Inject
import javax.inject.Singleton
import kotlin.math.sqrt

@Singleton
class FeedingManager @Inject constructor() {

    fun updatePosition(position: Pair<Float, Float>?, offset: Offset): Pair<Float, Float> {
        val currentPosition = position ?: Pair(0f, 0f)
        return Pair(
            currentPosition.first + offset.x,
            currentPosition.second + offset.y
        )
    }

    fun checkCharacterCollision(
        foodPosition: Pair<Float, Float>?,
        modelScreenPosition: Pair<Float, Float>?
    ): Boolean {
        if (foodPosition == null) return false
        if (modelScreenPosition == null) return false

        Log.d("FeedingManager", "foodPosition: $foodPosition")
        Log.d("FeedingManager", "modelScreenPosition: $modelScreenPosition")
        val collisionRadius = 500 // 충돌 감지 반경

        val distance = sqrt(
            (foodPosition.first + modelScreenPosition.first) * (foodPosition.first + modelScreenPosition.first) +
                    (foodPosition.second + modelScreenPosition.second) * (foodPosition.second + modelScreenPosition.second)
        )
        Log.d("FeedingManager", "distance: $distance")
        return distance <= collisionRadius
    }

}