package com.ggumtle.ggumtle.unity.di

import com.example.domain.unity.UnitySendManager
import com.example.domain.unity.UnityStartupObserveManager
import com.ggumtle.ggumtle.unity.UnitySendManagerImpl
import com.ggumtle.ggumtle.unity.UnityStartupObserveObserveManagerImpl
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent

@Module
@InstallIn(SingletonComponent::class)
abstract class UnityModule {

    @Binds
    abstract fun bindStartupManager(
        unityStartupObserveManagerImpl: UnityStartupObserveObserveManagerImpl
    ): UnityStartupObserveManager

    @Binds
    abstract fun bindUnitySendManager(
        unitySendManagerImpl: UnitySendManagerImpl
    ): UnitySendManager
}