package com.ggumtle.mission.di

import com.ggumtle.mission.manager.ArManager
import dagger.Module
import dagger.Provides
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent
import javax.inject.Singleton

@Module
@InstallIn(SingletonComponent::class)
object ArModule {

    @Provides
    @Singleton
    fun provideArManager(): ArManager {
        return ArManager()
    }
}