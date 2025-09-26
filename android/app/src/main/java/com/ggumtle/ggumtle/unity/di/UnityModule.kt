package com.ggumtle.ggumtle.unity.di

import com.ggumtle.domain.unity.UnitySendManager
import com.ggumtle.domain.unity.UnityObserveManager
import com.ggumtle.ggumtle.unity.UnitySendManagerImpl
import com.ggumtle.ggumtle.unity.UnityObserveManagerImpl
import dagger.Binds
import dagger.Module
import dagger.hilt.InstallIn
import dagger.hilt.components.SingletonComponent

@Module
@InstallIn(SingletonComponent::class)
abstract class UnityModule {

    @Binds
    abstract fun bindStartupManager(
        unityStartupObserveManagerImpl: UnityObserveManagerImpl
    ): UnityObserveManager

    @Binds
    abstract fun bindUnitySendManager(
        unitySendManagerImpl: UnitySendManagerImpl
    ): UnitySendManager
}