package com.ggumtle.mission.di

import com.ggumtle.mission.manager.CaptureManager
import com.ggumtle.mission.manager.FeedingManager
import com.ggumtle.mission.manager.ShakeDetectionManager
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object MissionModule {

    @Provides
    @Singleton
    fun provideCaptureManager(): CaptureManager {
        return CaptureManager()
    }

    @Provides
    @Singleton
    fun provideFeedingManager(): FeedingManager {
        return FeedingManager()
    }

    @Provides
    @Singleton
    fun provideShakeDetectionManager(): ShakeDetectionManager {
        return ShakeDetectionManager()
    }
}